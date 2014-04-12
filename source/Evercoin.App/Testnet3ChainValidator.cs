using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using NodaTime;

namespace Evercoin.App
{
    public sealed class Testnet3ChainValidator : IChainValidator
    {
        private readonly IChainParameters chainParameters;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        private readonly IChainSerializer chainSerializer;

        private readonly IReadableChainStore chainStore;

        private readonly ITransactionScriptParser scriptParser;

        private readonly ISignatureCheckerFactory signatureCheckerFactory;

        private readonly ITransactionScriptRunner scriptRunner;

        private readonly IBlockChain blockChain;

        public Testnet3ChainValidator(IReadableChainStore chainStore, ITransactionScriptParser scriptParser, ISignatureCheckerFactory signatureCheckerFactory, ITransactionScriptRunner scriptRunner, IChainParameters chainParameters, IHashAlgorithmStore hashAlgorithmStore, IChainSerializer chainSerializer, IBlockChain blockChain)
        {
            this.chainStore = chainStore;
            this.scriptParser = scriptParser;
            this.signatureCheckerFactory = signatureCheckerFactory;
            this.scriptRunner = scriptRunner;
            this.chainParameters = chainParameters;
            this.hashAlgorithmStore = hashAlgorithmStore;
            this.chainSerializer = chainSerializer;
            this.blockChain = blockChain;
        }

        public ValidationResult ValidateBlock(IBlock block)
        {
            if (Equals(block, this.chainParameters.GenesisBlock))
            {
                return ValidationResult.PassingResult;
            }

            Guid blockHashAlgorithmIdentifier = this.chainParameters.BlockHashAlgorithmIdentifier;
            IHashAlgorithm blockHashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(blockHashAlgorithmIdentifier);
            IChainSerializer chainSerializer = this.chainSerializer;

            FancyByteArray blockIdentifier = blockHashAlgorithm.CalculateHash(chainSerializer.GetBytesForBlock(block).Value);

            if (blockIdentifier >= block.DifficultyTarget)
            {
                // The block doesn't even meet the proof-of-work it says it's supposed to meet.
                return ValidationResult.FailWithReason("Block does not meet difficulty target.");
            }

            ulong? blockHeight = this.blockChain.GetHeightOfBlock(blockIdentifier);
            if (!blockHeight.HasValue)
            {
                // The block doesn't actually appear in the chain.
                // TODO: I think this should actually throw instead?
                return ValidationResult.FailWithReason("Block is unconnected");
            }

            FancyByteArray prevBlockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(blockHeight.Value - 1).Value;
            IBlock prevBlock = this.chainStore.GetBlock(prevBlockIdentifier);
            BigInteger prevTarget = prevBlock.DifficultyTarget;

            if (0 != blockHeight.Value % this.chainParameters.BlocksPerDifficultyRetarget)
            {
                // We're not at an adjustment boundary.
                // testnet3 lets you mine a min-difficulty block if nobody has found a block in two intervals.
                if (block.Timestamp - prevBlock.Timestamp > this.chainParameters.DesiredTimeBetweenBlocks * 2)
                {
                    return block.DifficultyTarget > this.chainParameters.MaximumDifficultyTarget ?
                        ValidationResult.FailWithReason("Block doesn't meet the maximum target for testnet") :
                        ValidationResult.PassingResult;
                }

                // If the previous block was a min-difficulty block, but we're not in the above-mentioned special case
                // then we need to scan, often back to the previous retarget, for the "actual" target.
                while (blockHeight.Value > this.chainParameters.BlocksPerDifficultyRetarget &&
                       0 != (blockHeight.Value - 1) % this.chainParameters.BlocksPerDifficultyRetarget &&
                       prevTarget == this.chainParameters.MaximumDifficultyTarget)
                {
                    prevBlockIdentifier = prevBlock.PreviousBlockIdentifier;
                    prevBlock = this.chainStore.GetBlock(prevBlockIdentifier);
                    prevTarget = prevBlock.DifficultyTarget;
                    blockHeight = blockHeight - 1;
                }

                return block.DifficultyTarget != prevTarget ?
                       ValidationResult.FailWithReason("Block has a different difficulty target, but it's not on an adjustment boundary.") :
                       ValidationResult.PassingResult;
            }

            // We're at an adjustment boundary.  Make sure that the target is accurate.
            // For some reason, Bitcoin uses the time since the previous block for the adjustment, rather than the current one.
            ulong prevAdjustmentHeight = blockHeight.Value - this.chainParameters.BlocksPerDifficultyRetarget;
            FancyByteArray prevAdjustmentBlockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(prevAdjustmentHeight).Value;
            IBlock prevAdjustmentBlock = this.chainStore.GetBlock(prevAdjustmentBlockIdentifier);

            Duration desiredTimeBetweenBlockIntervals = this.chainParameters.DesiredTimeBetweenBlocks * this.chainParameters.BlocksPerDifficultyRetarget;
            Duration actualTimeBetweenBlockIntervals = prevBlock.Timestamp - prevAdjustmentBlock.Timestamp;

            // Don't let the adjustment shift by more than a factor of 4 in either direction.
            if (actualTimeBetweenBlockIntervals < desiredTimeBetweenBlockIntervals / 4)
            {
                actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals / 4;
            }

            if (actualTimeBetweenBlockIntervals > desiredTimeBetweenBlockIntervals * 4)
            {
                actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals * 4;
            }

            BigInteger nextTarget = prevTarget;
            nextTarget *= actualTimeBetweenBlockIntervals.Ticks;
            nextTarget /= desiredTimeBetweenBlockIntervals.Ticks;
            nextTarget = Extensions.TargetFromBits(Extensions.TargetToBits(nextTarget));
            nextTarget = BigInteger.Min(nextTarget, this.chainParameters.MaximumDifficultyTarget);

            return nextTarget != block.DifficultyTarget ?
                   ValidationResult.FailWithReason("Block has the wrong target.") :
                   ValidationResult.PassingResult;
        }

        public ValidationResult ValidateTransaction(ITransaction transaction)
        {
            Guid txHashAlgorithmIdentifier = this.chainParameters.TransactionHashAlgorithmIdentifier;
            IHashAlgorithm txHashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(txHashAlgorithmIdentifier);
            IChainSerializer chainSerializer = this.chainSerializer;

            FancyByteArray transactionIdentifier = txHashAlgorithm.CalculateHash(chainSerializer.GetBytesForTransaction(transaction).Value);

            if (this.chainStore.ContainsTransaction(transactionIdentifier))
            {
                return ValidationResult.PassingResult;
            }

            Dictionary<BigInteger, ITransaction> prevTransactions = new Dictionary<BigInteger, ITransaction>();
            foreach (FancyByteArray prevTransactionIdentifier in transaction.Inputs.Select(x => x.SpentTransactionIdentifier).Where(x => !x.NumericValue.IsZero).Distinct())
            {
                if (!this.chainStore.ContainsTransaction(prevTransactionIdentifier.Value))
                {
                    return ValidationResult.FailWithReason("Previous transaction doesn't exist yet...");
                }

                prevTransactions[prevTransactionIdentifier] = this.chainStore.GetTransaction(prevTransactionIdentifier);
            }

            int valid = 1;
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.For(0, transaction.Inputs.Count, options, i =>
            {
                var input = transaction.Inputs[i];
                if (input.SpentTransactionIdentifier.NumericValue.IsZero)
                {
                    return;
                }

                ITransaction prevTransaction = prevTransactions[input.SpentTransactionIdentifier];
                IValueSource spentValueSource = prevTransaction.Outputs[(int)input.SpentTransactionOutputIndex];

                byte[] scriptSig = input.ScriptSignature;
                IEnumerable<TransactionScriptOperation> parsedScriptSig = this.scriptParser.Parse(scriptSig);
                ISignatureChecker signatureChecker = this.signatureCheckerFactory.CreateSignatureChecker(transaction, i);
                var result = this.scriptRunner.EvaluateScript(parsedScriptSig, signatureChecker);
                if (!result)
                {
                    Interlocked.CompareExchange(ref valid, 0, 1);
                    return;
                }

                byte[] scriptPubKey = spentValueSource.ScriptPublicKey;
                IEnumerable<TransactionScriptOperation> parsedScriptPubKey = this.scriptParser.Parse(scriptPubKey);

                if (!this.scriptRunner.EvaluateScript(parsedScriptPubKey, signatureChecker, result.MainStack, result.AlternateStack))
                {
                    Interlocked.CompareExchange(ref valid, 0, 1);
                }
            });

            if (valid == 0)
            {
                return ValidationResult.FailWithReason("Invalid script!");
            }

            return ValidationResult.PassingResult;
        }
    }
}
