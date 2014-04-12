using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using NodaTime;

namespace Evercoin.App
{
    public sealed class DogecoinChainValidator : IChainValidator
    {
        private readonly IChainParameters chainParameters;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        private readonly IChainSerializer chainSerializer;

        private readonly IReadableChainStore chainStore;

        private readonly ITransactionScriptParser scriptParser;

        private readonly ISignatureCheckerFactory signatureCheckerFactory;

        private readonly ITransactionScriptRunner scriptRunner;

        private readonly IBlockChain blockChain;

        public DogecoinChainValidator(IReadableChainStore chainStore, ITransactionScriptParser scriptParser, ISignatureCheckerFactory signatureCheckerFactory, ITransactionScriptRunner scriptRunner, IChainParameters chainParameters, IHashAlgorithmStore hashAlgorithmStore, IChainSerializer chainSerializer, IBlockChain blockChain)
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

            Guid proofOfWorkHashAlgorithmIdentifier = this.chainParameters.ProofOfWorkHashAlgorithmIdentifier;
            IHashAlgorithm proofOfWorkHashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(proofOfWorkHashAlgorithmIdentifier);
            IChainSerializer chainSerializer = this.chainSerializer;

            FancyByteArray proofOfWork = proofOfWorkHashAlgorithm.CalculateHash(chainSerializer.GetBytesForBlock(block).Value);

            if (proofOfWork >= block.DifficultyTarget)
            {
                // The block doesn't even meet the proof-of-work it says it's supposed to meet.
                return ValidationResult.FailWithReason("Block does not meet difficulty target.");
            }

            Guid blockHashAlgorithmIdentifier = this.chainParameters.BlockHashAlgorithmIdentifier;
            IHashAlgorithm blockHashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(blockHashAlgorithmIdentifier);

            FancyByteArray blockIdentifier = blockHashAlgorithm.CalculateHash(chainSerializer.GetBytesForBlock(block).Value);

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

            if (0 == blockHeight.Value % this.chainParameters.BlocksPerDifficultyRetarget)
            {
                // We're at an adjustment boundary.  Make sure that the target is accurate.
                ulong prevAdjustmentHeight = blockHeight.Value - this.chainParameters.BlocksPerDifficultyRetarget;
                if (prevAdjustmentHeight > 0)
                {
                    prevAdjustmentHeight--;
                }

                FancyByteArray prevAdjustmentBlockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(prevAdjustmentHeight).Value;
                IBlock prevAdjustmentBlock = this.chainStore.GetBlock(prevAdjustmentBlockIdentifier);

                Duration desiredTimeBetweenBlockIntervals = this.chainParameters.DesiredTimeBetweenBlocks * this.chainParameters.BlocksPerDifficultyRetarget;
                Duration actualTimeBetweenBlockIntervals = prevBlock.Timestamp - prevAdjustmentBlock.Timestamp;

                if (blockHeight.Value >= 145000)
                {
                    actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals + (actualTimeBetweenBlockIntervals - desiredTimeBetweenBlockIntervals) / 8;
                    if (actualTimeBetweenBlockIntervals < (desiredTimeBetweenBlockIntervals - (desiredTimeBetweenBlockIntervals / 4)))
                    {
                        actualTimeBetweenBlockIntervals = (desiredTimeBetweenBlockIntervals - (desiredTimeBetweenBlockIntervals / 4));
                    }

                    if (actualTimeBetweenBlockIntervals > (desiredTimeBetweenBlockIntervals + (desiredTimeBetweenBlockIntervals / 2)))
                    {
                        actualTimeBetweenBlockIntervals = (desiredTimeBetweenBlockIntervals + (desiredTimeBetweenBlockIntervals / 2));
                    }
                }
                else if (blockHeight.Value > 10000)
                {
                    if (actualTimeBetweenBlockIntervals < desiredTimeBetweenBlockIntervals / 4)
                    {
                        actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals / 4;
                    }

                    if (actualTimeBetweenBlockIntervals > desiredTimeBetweenBlockIntervals * 4)
                    {
                        actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals * 4;
                    }
                }
                else if (blockHeight.Value > 5000)
                {
                    if (actualTimeBetweenBlockIntervals < desiredTimeBetweenBlockIntervals / 8)
                    {
                        actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals / 8;
                    }

                    if (actualTimeBetweenBlockIntervals > desiredTimeBetweenBlockIntervals * 4)
                    {
                        actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals * 4;
                    }
                }
                else
                {
                    if (actualTimeBetweenBlockIntervals < desiredTimeBetweenBlockIntervals / 16)
                    {
                        actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals / 16;
                    }

                    if (actualTimeBetweenBlockIntervals > desiredTimeBetweenBlockIntervals * 4)
                    {
                        actualTimeBetweenBlockIntervals = desiredTimeBetweenBlockIntervals * 4;
                    }
                }

                BigInteger nextTarget = prevTarget;
                nextTarget *= actualTimeBetweenBlockIntervals.Ticks;
                nextTarget /= desiredTimeBetweenBlockIntervals.Ticks;
                nextTarget = Extensions.TargetFromBits(Extensions.TargetToBits(nextTarget));
                nextTarget = BigInteger.Min(nextTarget, this.chainParameters.MaximumDifficultyTarget);

                if (nextTarget != block.DifficultyTarget)
                {
                    return ValidationResult.FailWithReason(String.Format(CultureInfo.InvariantCulture, "Block has the wrong target.  Expected: {0}, Actual: {1}", Extensions.TargetToBits(nextTarget).ToString("X"), Extensions.TargetToBits(block.DifficultyTarget).ToString("X")));
                }
            }
            else
            {
                // We're not at an adjustment boundary.
                if (block.DifficultyTarget != prevTarget)
                {
                    ValidationResult.FailWithReason("Block has a different difficulty target, but it's not on an adjustment boundary.");
                }
            }

            // TODO: Figure out the randomness... or just hand-wave it by checkpointing.
            ////List<ITransaction> transactions = this.blockChain.GetTransactionsForBlock(blockIdentifier).Select(this.chainStore.GetTransaction).ToList();
            ////
            ////decimal actualCoinbaseValue = transactions[0].Outputs.Sum(x => x.AvailableValue);
            ////
            ////long allowedSubsidyValue;
            ////
            ////string seedString = prevBlockIdentifier.ToString().Substring(7, 7);
            ////int seed = BitConverter.ToInt32(ByteTwiddling.HexStringToByteArray("0" + seedString), 0);
            ////Random random = new Random(seed);
            ////if (blockHeight < 100000)
            ////{
            ////    allowedSubsidyValue = (1 + random.Next(999999)) * 100000000L;
            ////}
            ////else if (blockHeight < 145000)
            ////{
            ////    allowedSubsidyValue = (1 + random.Next(499999)) * 100000000L;
            ////}
            ////else if (blockHeight < 600000)
            ////{
            ////    allowedSubsidyValue = (1 + random.Next(999999)) * 100000000L;
            ////    allowedSubsidyValue /= (int)(blockHeight.Value / 100000);
            ////}
            ////else
            ////{
            ////    allowedSubsidyValue = 1000000000000L;
            ////}
            ////
            ////List<Tuple<FancyByteArray, uint, ITransaction>> prevOutputs = transactions.Select(x => Tuple.Create(x.Inputs, x)).SelectMany(x => x.Item1.Select(y => Tuple.Create(y.SpentTransactionIdentifier, y.SpentTransactionOutputIndex, x.Item2))).ToList();
            ////Dictionary<FancyByteArray, ITransaction> prevTransactions = prevOutputs.Select(x => x.Item1).ExceptWhere(x => x.NumericValue.IsZero).Distinct().ToDictionary(x => x, this.chainStore.GetTransaction);
            ////decimal fees = prevOutputs.ExceptWhere(x => x.Item1.NumericValue.IsZero).GroupBy(x => x.Item3).Sum(x => x.Sum(y => prevTransactions[y.Item1].Outputs[(int)y.Item2].AvailableValue) - x.Key.Outputs.Sum(y => y.AvailableValue));
            ////if (actualCoinbaseValue != allowedSubsidyValue + fees)
            ////{
            ////    return ValidationResult.FailWithReason(String.Format(CultureInfo.InvariantCulture, "Invalid coinbase value.  Expected {0}, got {1}.", (long)(allowedSubsidyValue + fees) / (double)100000000, (long)actualCoinbaseValue / (double)100000000));
            ////}

            return ValidationResult.PassingResult;
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
