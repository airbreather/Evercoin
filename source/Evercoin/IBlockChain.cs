using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Evercoin
{
    public interface IBlockChain
    {
        ulong Length { get; }

        FancyByteArray? GetIdentifierOfBlockAtHeight(ulong height);

        FancyByteArray? GetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier);

        ulong? GetHeightOfBlock(FancyByteArray blockIdentifier);

        void AddBlockAtHeight(FancyByteArray block, ulong height);

        void AddTransactionToBlock(FancyByteArray transactionIdentifier, FancyByteArray blockIdentifier, ulong index);

        IEnumerable<FancyByteArray> GetTransactionsForBlock(FancyByteArray blockIdentifier);
    }

    public sealed class BlockChain : IBlockChain
    {
        private readonly ConcurrentDictionary<FancyByteArray, List<FancyByteArray>> blockToTransactionMapping = new ConcurrentDictionary<FancyByteArray, List<FancyByteArray>>();

        private readonly ConcurrentDictionary<FancyByteArray, FancyByteArray> transactionToBlockMapping = new ConcurrentDictionary<FancyByteArray, FancyByteArray>();

        private readonly ConcurrentDictionary<FancyByteArray, ulong> blockToHeightMapping = new ConcurrentDictionary<FancyByteArray, ulong>();

        private readonly ConcurrentDictionary<ulong, FancyByteArray> heightToBlockMapping = new ConcurrentDictionary<ulong, FancyByteArray>();

        private long length = Int64.MinValue + 1;

        public ulong Length { get { return (ulong)this.length + Int64.MaxValue; } }

        public FancyByteArray? GetIdentifierOfBlockAtHeight(ulong height)
        {
            FancyByteArray result;
            return this.heightToBlockMapping.TryGetValue(height, out result) ?
                   result :
                   default(FancyByteArray?);
        }

        public FancyByteArray? GetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier)
        {
            FancyByteArray result;
            return this.transactionToBlockMapping.TryGetValue(transactionIdentifier, out result) ?
                result :
                default(FancyByteArray?);
        }

        public ulong? GetHeightOfBlock(FancyByteArray blockIdentifier)
        {
            ulong result;
            return this.blockToHeightMapping.TryGetValue(blockIdentifier, out result) ?
                   result :
                   default(ulong?);
        }

        public void AddBlockAtHeight(FancyByteArray blockIdentifier, ulong height)
        {
            this.blockToHeightMapping[blockIdentifier] = height;
            this.heightToBlockMapping[height] = blockIdentifier;
            this.blockToTransactionMapping[blockIdentifier] = new List<FancyByteArray>();
            Interlocked.Increment(ref length);
        }

        public void AddTransactionToBlock(FancyByteArray transactionIdentifier, FancyByteArray blockIdentifier, ulong index)
        {
            List<FancyByteArray> blockTransactions = this.blockToTransactionMapping.GetOrAdd(blockIdentifier, _ => new List<FancyByteArray>());

            if (index > Int32.MaxValue)
            {
                throw new NotSupportedException("Seriously?");
            }

            // TODO: BIP30
            this.transactionToBlockMapping[transactionIdentifier] = blockIdentifier;

            int intIndex = (int)index;

            lock (blockTransactions)
            {
                while (blockTransactions.Count < intIndex + 1)
                {
                    blockTransactions.Add(new FancyByteArray());
                }

                blockTransactions[intIndex] = transactionIdentifier;
            }
        }

        public IEnumerable<FancyByteArray> GetTransactionsForBlock(FancyByteArray blockIdentifier)
        {
            List<FancyByteArray> blockTransactions;
            if (!this.blockToTransactionMapping.TryGetValue(blockIdentifier, out blockTransactions))
            {
                return Enumerable.Empty<FancyByteArray>();
            }

            lock (blockTransactions)
            {
                return blockTransactions.ToList();
            }
        }
    }
}
