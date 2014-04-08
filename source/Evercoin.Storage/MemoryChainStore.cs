using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Numerics;

using Evercoin.BaseImplementations;
using Evercoin.Util;

namespace Evercoin.Storage
{
    ////[Export(typeof(IChainStore))]
    ////[Export(typeof(IReadableChainStore))]
    public sealed class MemoryChainStore : ReadWriteChainStoreBase
    {
        private readonly ConcurrentDictionary<FancyByteArray, byte[]> blocks = new ConcurrentDictionary<FancyByteArray, byte[]>();
        private readonly ConcurrentDictionary<FancyByteArray, byte[]> transactions = new ConcurrentDictionary<FancyByteArray, byte[]>();
        private readonly Waiter<FancyByteArray> blockWaiter = new Waiter<FancyByteArray>();
        private readonly Waiter<FancyByteArray> txWaiter = new Waiter<FancyByteArray>();
        
        [Import]
        public IChainSerializer ChainSerializer { get; set; }

        protected override IBlock FindBlockCore(FancyByteArray blockIdentifier)
        {
            this.blockWaiter.WaitFor(blockIdentifier);
            byte[] serializedBlock = this.blocks[blockIdentifier];
            return this.ChainSerializer.GetBlockForBytes(serializedBlock);
        }

        protected override ITransaction FindTransactionCore(FancyByteArray transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);
            byte[] serializedTransaction = this.transactions[transactionIdentifier];
            return this.ChainSerializer.GetTransactionForBytes(serializedTransaction);
        }

        protected override bool ContainsBlockCore(FancyByteArray blockIdentifier)
        {
            return this.blocks.ContainsKey(blockIdentifier);
        }

        protected override bool ContainsTransactionCore(FancyByteArray transactionIdentifier)
        {
            return this.transactions.ContainsKey(transactionIdentifier);
        }

        protected override void PutBlockCore(FancyByteArray blockIdentifier, IBlock block)
        {
            byte[] serializedBlock = this.ChainSerializer.GetBytesForBlock(block);
            this.blocks[blockIdentifier] = serializedBlock;
            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            byte[] serializedTransaction = this.ChainSerializer.GetBytesForTransaction(transaction);

            // TODO: coinbases can have duplicate transaction IDs before version 2.
            // TODO: Figure that shiz out!
            this.transactions[transactionIdentifier] = serializedTransaction;
            this.txWaiter.SetEventFor(transactionIdentifier);
        }

        /// <summary>
        /// When overridden in a derived class, releases managed resources that
        /// implement the <see cref="IDisposable"/> interface.
        /// </summary>
        protected override void DisposeManagedResources()
        {
            this.blockWaiter.Dispose();
            this.txWaiter.Dispose();
            base.DisposeManagedResources();
        }
    }
}
