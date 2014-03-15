using System.Collections.Concurrent;
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
            this.PutBlock(genesisBlockIdentifier, genesisBlock);
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            IBlock block;
            bool weOwnMres = false;
            ManualResetEventSlim mres = this.blockWaiters.GetOrAdd(blockIdentifier, delegate
            {
                weOwnMres = true;
                return new ManualResetEventSlim();
            });
            while (!this.blocks.TryGetValue(blockIdentifier, out block))
            {
                mres.Wait(10000);
            }

            if (weOwnMres)
            {
                mres.Dispose();
            }

            return block;
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            ITransaction transaction;
            bool weOwnMres = false;
            ManualResetEventSlim mres = this.txWaiters.GetOrAdd(transactionIdentifier, delegate
            {
                weOwnMres = true;
                return new ManualResetEventSlim();
            });
            while (!this.transactions.TryGetValue(transactionIdentifier, out transaction))
            {
                mres.Wait(10000);
            }

            if (weOwnMres)
            {
                mres.Dispose();
            }

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

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            this.blocks.Add(blockIdentifier, block);

            ManualResetEventSlim waiter;
            if (this.blockWaiters.TryRemove(blockIdentifier, out waiter))
            {
                using (waiter)
                {
                    waiter.Set();
                }
            }
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            // TODO: coinbases can have duplicate transaction IDs before version 2.
            // TODO: Figure that shiz out!
            this.transactions[transactionIdentifier] = transaction;

            ManualResetEventSlim waiter;
            if (this.txWaiters.TryRemove(transactionIdentifier, out waiter))
            {
                using (waiter)
                {
                    waiter.Set();
                }
            }
        }
    }
}
