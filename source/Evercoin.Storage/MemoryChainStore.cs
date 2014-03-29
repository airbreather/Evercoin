using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Numerics;

using Evercoin.BaseImplementations;
using Evercoin.Util;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    [Export(typeof(IReadableChainStore))]
    public sealed class MemoryChainStore : ReadWriteChainStoreBase
    {
        private readonly ConcurrentDictionary<BigInteger, byte[]> blocks = new ConcurrentDictionary<BigInteger, byte[]>();
        private readonly ConcurrentDictionary<BigInteger, byte[]> transactions = new ConcurrentDictionary<BigInteger, byte[]>();
        private readonly Waiter<BigInteger> blockWaiter = new Waiter<BigInteger>();
        private readonly Waiter<BigInteger> txWaiter = new Waiter<BigInteger>();
        
        [Import]
        public IChainSerializer ChainSerializer { get; set; }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            this.blockWaiter.WaitFor(blockIdentifier);
            byte[] serializedBlock = this.blocks[blockIdentifier];
            return this.ChainSerializer.GetBlockForBytes(serializedBlock);
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);
            byte[] serializedTransaction = this.transactions[transactionIdentifier];
            return this.ChainSerializer.GetTransactionForBytes(serializedTransaction);
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.blocks.ContainsKey(blockIdentifier);
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.transactions.ContainsKey(transactionIdentifier);
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            byte[] serializedBlock = this.ChainSerializer.GetBytesForBlock(block);
            this.blocks[blockIdentifier] = serializedBlock;
            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
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
