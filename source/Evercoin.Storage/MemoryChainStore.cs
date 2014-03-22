using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    [Export(typeof(IReadableChainStore))]
    public sealed class MemoryChainStore : ReadWriteChainStoreBase
    {
        private readonly ConcurrentDictionary<BigInteger, IBlock> blocks = new ConcurrentDictionary<BigInteger, IBlock>();
        private readonly ConcurrentDictionary<BigInteger, ITransaction> transactions = new ConcurrentDictionary<BigInteger, ITransaction>();
        private readonly Waiter<BigInteger> blockWaiter = new Waiter<BigInteger>();
        private readonly Waiter<BigInteger> txWaiter = new Waiter<BigInteger>();

        public MemoryChainStore()
        {
            BigInteger genesisBlockIdentifier = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").Reverse().GetArray());
            Block genesisBlock = new Block
            {
                Identifier = genesisBlockIdentifier,
                TransactionIdentifiers = new MerkleTreeNode { Data = ByteTwiddling.HexStringToByteArray("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B").Reverse().GetArray() }
            };
            this.PutBlock(genesisBlockIdentifier, genesisBlock);
            Cheating.AddBlock(0, genesisBlockIdentifier);
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            this.blockWaiter.WaitFor(blockIdentifier);
            return this.blocks[blockIdentifier];
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);
            return this.transactions[transactionIdentifier];
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
            this.blocks[blockIdentifier] = block;
            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            // TODO: coinbases can have duplicate transaction IDs before version 2.
            // TODO: Figure that shiz out!
            this.transactions[transactionIdentifier] = transaction;
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
