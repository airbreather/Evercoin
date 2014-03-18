using System.Collections.Concurrent;
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
    ////[Export(typeof(IChainStore))]
    ////[Export(typeof(IReadableChainStore))]
    public sealed class MemoryChainStore : ReadWriteChainStoreBase
    {
        private readonly ConcurrentDictionary<BigInteger, IBlock> blocks = new ConcurrentDictionary<BigInteger, IBlock>();
        private readonly ConcurrentDictionary<BigInteger, ITransaction> transactions = new ConcurrentDictionary<BigInteger, ITransaction>();

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
            Cheating.Add(0, genesisBlockIdentifier);
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            SpinWait waiter = new SpinWait();
            IBlock block;
            while (!this.blocks.TryGetValue(blockIdentifier, out block))
            {
                waiter.SpinOnce();
            }

            return block;
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            SpinWait waiter = new SpinWait();
            ITransaction transaction;
            while (!this.transactions.TryGetValue(transactionIdentifier, out transaction))
            {
                waiter.SpinOnce();
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
            this.blocks[blockIdentifier] = block;
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            // TODO: coinbases can have duplicate transaction IDs before version 2.
            // TODO: Figure that shiz out!
            this.transactions[transactionIdentifier] = transaction;
        }
    }
}
