﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    [Export(typeof(IReadOnlyChainStore))]
    public sealed class MemoryChainStore : ReadWriteChainStoreBase
    {
        private readonly Dictionary<BigInteger, IBlock> blocks = new Dictionary<BigInteger, IBlock>();
        private readonly Dictionary<BigInteger, ITransaction> transactions = new Dictionary<BigInteger, ITransaction>();
        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> blockWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();
        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> txWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();

        public MemoryChainStore()
        {
            BigInteger genesisBlockIdentifier = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").Reverse().GetArray());
            Block genesisBlock = new Block
            {
                Identifier = genesisBlockIdentifier,
                TypedCoinbase = new CoinbaseValueSource
                {
                    AvailableValue = 50,
                    OriginatingBlockIdentifier = genesisBlockIdentifier
                },
                TransactionIdentifiers = new MerkleTreeNode { Data = ByteTwiddling.HexStringToByteArray("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B").Reverse().GetArray() }
            };
            this.PutBlock(genesisBlock);
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            IBlock block;
            if (this.blocks.TryGetValue(blockIdentifier, out block))
            {
                return block;
            }

            ManualResetEventSlim mres = this.blockWaiters.GetOrAdd(blockIdentifier, _ => new ManualResetEventSlim());
            if (mres.Wait(100) &&
                this.blockWaiters.TryRemove(blockIdentifier, out mres))
            {
                mres.Dispose();
            }

            this.blocks.TryGetValue(blockIdentifier, out block);
            return block;
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            ITransaction transaction;
            if (this.transactions.TryGetValue(transactionIdentifier, out transaction))
            {
                return transaction;
            }

            ManualResetEventSlim mres = this.txWaiters.GetOrAdd(transactionIdentifier, _ => new ManualResetEventSlim());
            if (mres.Wait(100) &&
                this.txWaiters.TryRemove(transactionIdentifier, out mres))
            {
                mres.Dispose();
            }

            this.transactions.TryGetValue(transactionIdentifier, out transaction);
            return transaction;
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.blocks.ContainsKey(blockIdentifier);
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.transactions.ContainsKey(transactionIdentifier);
        }

        protected override async Task<bool> ContainsBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return await Task.Run(() => this.ContainsBlockCore(blockIdentifier), token);
        }

        protected override async Task<bool> ContainsTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return await Task.Run(() => this.ContainsTransactionCore(transactionIdentifier), token);
        }

        protected override void PutBlockCore(IBlock block)
        {
            this.blocks.Add(block.Identifier, block);
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
            // TODO: coinbases can have duplicate transaction IDs before version 2.
            // TODO: Figure that shiz out!
            this.transactions[transaction.Identifier] = transaction;
        }
    }
}
