#if !X64
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Evercoin.Util;

namespace Evercoin.Storage
{
    public sealed class MemoryBlockChain : IBlockChain
    {
        private readonly ConcurrentDictionary<FancyByteArray, List<FancyByteArray>> blockToTransactionMapping = new ConcurrentDictionary<FancyByteArray, List<FancyByteArray>>();

        private readonly ConcurrentDictionary<FancyByteArray, FancyByteArray> transactionToBlockMapping = new ConcurrentDictionary<FancyByteArray, FancyByteArray>();

        private readonly ConcurrentDictionary<FancyByteArray, ulong> blockToHeightMapping = new ConcurrentDictionary<FancyByteArray, ulong>();

        private readonly ConcurrentDictionary<ulong, FancyByteArray> heightToBlockMapping = new ConcurrentDictionary<ulong, FancyByteArray>();

        private readonly Waiter<ulong> blockHeightWaiter = new Waiter<ulong>();

        private readonly Waiter<FancyByteArray> blockIdWaiter = new Waiter<FancyByteArray>();

        private readonly Waiter<FancyByteArray> txIdWaiter = new Waiter<FancyByteArray>();

        private long blockCount = Int64.MinValue + 1;

        private long transactionCount = Int64.MinValue + 1;

        public ulong BlockCount { get { return (ulong)this.blockCount + Int64.MaxValue; } }

        public ulong TransactionCount { get { return (ulong)this.transactionCount + Int64.MaxValue; } }

        public FancyByteArray GetIdentifierOfBlockAtHeight(ulong height)
        {
            FancyByteArray result;
            while (!this.TryGetIdentifierOfBlockAtHeight(height, out result))
            {
                this.blockHeightWaiter.WaitFor(height);
            }

            return result;
        }

        public FancyByteArray GetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier)
        {
            FancyByteArray result;
            while (!this.TryGetIdentifierOfBlockWithTransaction(transactionIdentifier, out result))
            {
                this.txIdWaiter.WaitFor(transactionIdentifier);
            }

            return result;
        }

        public ulong GetHeightOfBlock(FancyByteArray blockIdentifier)
        {
            ulong result;
            while (!this.TryGetHeightOfBlock(blockIdentifier, out result))
            {
                this.blockIdWaiter.WaitFor(blockIdentifier);
            }

            return result;
        }

        public IEnumerable<FancyByteArray> GetTransactionsForBlock(FancyByteArray blockIdentifier)
        {
            IEnumerable<FancyByteArray> result;
            while (!this.TryGetTransactionsForBlock(blockIdentifier, out result))
            {
                this.blockIdWaiter.WaitFor(blockIdentifier);
            }

            return result;
        }

        public bool TryGetIdentifierOfBlockAtHeight(ulong height, out FancyByteArray blockIdentifier)
        {
            return this.heightToBlockMapping.TryGetValue(height, out blockIdentifier);
        }

        public bool TryGetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier, out FancyByteArray blockIdentifier)
        {
            return this.transactionToBlockMapping.TryGetValue(transactionIdentifier, out blockIdentifier);
        }

        public bool TryGetHeightOfBlock(FancyByteArray blockIdentifier, out ulong height)
        {
            return this.blockToHeightMapping.TryGetValue(blockIdentifier, out height);
        }

        public bool TryGetTransactionsForBlock(FancyByteArray blockIdentifier, out IEnumerable<FancyByteArray> transactionIdentifiers)
        {
            List<FancyByteArray> blockTransactions;
            if (!this.blockToTransactionMapping.TryGetValue(blockIdentifier, out blockTransactions))
            {
                transactionIdentifiers = default(IEnumerable<FancyByteArray>);
                return false;
            }

            lock (blockTransactions)
            {
                transactionIdentifiers = blockTransactions.ToList();
                return true;
            }
        }

        public void AddBlockAtHeight(FancyByteArray blockIdentifier, ulong height)
        {
            this.blockToHeightMapping[blockIdentifier] = height;
            this.heightToBlockMapping[height] = blockIdentifier;
            this.blockToTransactionMapping[blockIdentifier] = new List<FancyByteArray>();
            Interlocked.Increment(ref this.blockCount);
            this.blockHeightWaiter.SetEventFor(height);
            this.blockIdWaiter.SetEventFor(blockIdentifier);
        }

        public void AddTransactionToBlock(FancyByteArray transactionIdentifier, FancyByteArray blockIdentifier, ulong index)
        {
            this.blockIdWaiter.WaitFor(blockIdentifier);
            List<FancyByteArray> blockTransactions = this.blockToTransactionMapping[blockIdentifier];

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
            this.txIdWaiter.SetEventFor(transactionIdentifier);
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
#endif