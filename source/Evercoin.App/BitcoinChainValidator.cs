using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.App
{
    public sealed class BitcoinChainValidator : IChainValidator
    {
        private readonly IChainParameters chainParameters;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        private readonly IChainSerializer chainSerializer;

        private readonly IReadableChainStore chainStore;

        private readonly ITransactionScriptParser scriptParser;

        private readonly ISignatureCheckerFactory signatureCheckerFactory;

        private readonly ITransactionScriptRunner scriptRunner;

        public BitcoinChainValidator(IReadableChainStore chainStore, ITransactionScriptParser scriptParser, ISignatureCheckerFactory signatureCheckerFactory, ITransactionScriptRunner scriptRunner, IChainParameters chainParameters, IHashAlgorithmStore hashAlgorithmStore, IChainSerializer chainSerializer, IBlockChain blockChain)
        {
            this.chainStore = chainStore;
            this.scriptParser = scriptParser;
            this.signatureCheckerFactory = signatureCheckerFactory;
            this.scriptRunner = scriptRunner;
            this.chainParameters = chainParameters;
            this.hashAlgorithmStore = hashAlgorithmStore;
            this.chainSerializer = chainSerializer;
        }

        public ValidationResult ValidateBlock(IBlock block)
        {
            Guid blockHashAlgorithmIdentifier = this.chainParameters.BlockHashAlgorithmIdentifier;
            IHashAlgorithm blockHashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(blockHashAlgorithmIdentifier);
            IChainSerializer chainSerializer = this.chainSerializer;

            FancyByteArray blockIdentifier = blockHashAlgorithm.CalculateHash(chainSerializer.GetBytesForBlock(block));

            if (blockIdentifier >= block.DifficultyTarget)
            {
                return ValidationResult.FailWithReason("Block does not meet difficulty target.");
            }

            return ValidationResult.PassingResult;
        }

        public ValidationResult ValidateTransaction(ITransaction transaction)
        {
            Guid txHashAlgorithmIdentifier = this.chainParameters.TransactionHashAlgorithmIdentifier;
            IHashAlgorithm txHashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(txHashAlgorithmIdentifier);
            IChainSerializer chainSerializer = this.chainSerializer;

            FancyByteArray transactionIdentifier = txHashAlgorithm.CalculateHash(chainSerializer.GetBytesForTransaction(transaction));

            if (this.chainStore.ContainsTransaction(transactionIdentifier))
            {
                return ValidationResult.PassingResult;
            }

            Dictionary<BigInteger, ITransaction> prevTransactions = new Dictionary<BigInteger, ITransaction>();
            foreach (BigInteger prevTransactionIdentifier in transaction.Inputs.Select(x => x.SpentTransactionIdentifier).Where(x => !x.IsZero).Distinct())
            {
                if (!this.chainStore.ContainsTransaction(prevTransactionIdentifier))
                {
                    return ValidationResult.FailWithReason("Previous transaction doesn't exist yet...");
                }

                prevTransactions[prevTransactionIdentifier] = this.chainStore.GetTransaction(prevTransactionIdentifier);
            }

            int valid = 1;
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.For(0, transaction.Inputs.Length, options, i =>
            {
                var input = transaction.Inputs[i];
                if (input.SpentTransactionIdentifier.IsZero)
                {
                    return;
                }

                ITransaction prevTransaction = prevTransactions[input.SpentTransactionIdentifier];
                IValueSource spentValueSource = prevTransaction.Outputs[input.SpentTransactionOutputIndex];

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
