using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Evercoin.Storage
{
    public sealed class MemoryBlockChain : IBlockChain
    {
        private readonly ConcurrentDictionary<FancyByteArray, List<FancyByteArray>> blockToTransactionMapping = new ConcurrentDictionary<FancyByteArray, List<FancyByteArray>>();

        private readonly ConcurrentDictionary<FancyByteArray, FancyByteArray> transactionToBlockMapping = new ConcurrentDictionary<FancyByteArray, FancyByteArray>();

        private readonly ConcurrentDictionary<FancyByteArray, ulong> blockToHeightMapping = new ConcurrentDictionary<FancyByteArray, ulong>();

        private readonly ConcurrentDictionary<ulong, FancyByteArray> heightToBlockMapping = new ConcurrentDictionary<ulong, FancyByteArray>();

        private long blockCount = Int64.MinValue + 1;

        private long transactionCount = Int64.MinValue + 1;

        public ulong BlockCount { get { return (ulong)this.blockCount + Int64.MaxValue; } }

        public ulong TransactionCount { get { return (ulong)this.transactionCount + Int64.MaxValue; } }

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
            Interlocked.Increment(ref this.blockCount);
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

            Interlocked.Increment(ref this.transactionCount);
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

        public void RemoveBlocksAboveHeight(ulong height)
        {
            if (height++ > this.BlockCount)
            {
                return;
            }

            for (; height < this.BlockCount; height++)
            {
                FancyByteArray blockIdentifier;
                if (this.heightToBlockMapping.TryRemove(height, out blockIdentifier))
                {
                    ulong _;
                    this.blockToHeightMapping.TryRemove(blockIdentifier, out _);

                    List<FancyByteArray> transactions;
                    this.blockToTransactionMapping.TryRemove(blockIdentifier, out transactions);
                    if (transactions != null)
                    {
                        this.transactionCount -= transactions.Count;
                    }
                }
            }

            this.blockCount = (long)(height - Int64.MaxValue);
        }
    }
}