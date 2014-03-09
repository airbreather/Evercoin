using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;

namespace Evercoin.App
{
    internal sealed class CompositeChainStorage : ReadWriteChainStoreBase
    {
        private readonly Collection<IChainStore> underlyingChainStores = new Collection<IChainStore>();
        private readonly Collection<IReadOnlyChainStore> underlyingReadOnlyChainStores = new Collection<IReadOnlyChainStore>();

        [ImportMany]
        public Collection<IChainStore> UnderlyingChainStores { get { return this.underlyingChainStores; } }

        [ImportMany]
        public Collection<IReadOnlyChainStore> UnderlyingReadOnlyChainStores { get { return this.underlyingReadOnlyChainStores; } }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            IBlock foundBlock = null;
            return this.underlyingReadOnlyChainStores.Any(x => x.TryGetBlock(blockIdentifier, out foundBlock)) ?
                   foundBlock :
                   null;
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            ITransaction foundTransaction = null;
            return this.underlyingReadOnlyChainStores.Any(x => x.TryGetTransaction(transactionIdentifier, out foundTransaction)) ?
                   foundTransaction :
                   null;
        }

        protected override void PutBlockCore(IBlock block)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                chainStore.PutBlock(block);
            }
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                chainStore.PutTransaction(transaction);
            }
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.underlyingReadOnlyChainStores.Any(x => x.ContainsBlock(blockIdentifier));
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.underlyingReadOnlyChainStores.Any(x => x.ContainsTransaction(transactionIdentifier));
        }

        protected override async Task<bool> ContainsBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            IEnumerable<Task<bool>> searchTasks = this.underlyingReadOnlyChainStores.Select(x => x.ContainsBlockAsync(blockIdentifier, token));
            bool[] searchResults = await Task.WhenAll(searchTasks);
            return searchResults.Contains(true);
        }

        protected override async Task<bool> ContainsTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            IEnumerable<Task<bool>> searchTasks = this.underlyingReadOnlyChainStores.Select(x => x.ContainsTransactionAsync(transactionIdentifier, token));
            bool[] searchResults = await Task.WhenAll(searchTasks);
            return searchResults.Contains(true);
        }

        protected override async Task<IBlock> FindBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            foreach (IReadOnlyChainStore chainStore in this.underlyingReadOnlyChainStores)
            {
                IBlock foundBlock = await chainStore.GetBlockAsync(blockIdentifier, token);
                if (foundBlock != null)
                {
                    return foundBlock;
                }
            }

            return null;
        }

        protected override async Task<ITransaction> FindTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            foreach (IReadOnlyChainStore chainStore in this.underlyingReadOnlyChainStores)
            {
                ITransaction foundTransaction = await chainStore.GetTransactionAsync(transactionIdentifier, token);
                if (foundTransaction != null)
                {
                    return foundTransaction;
                }
            }

            return null;
        }

        protected override async Task PutBlockAsyncCore(IBlock block, CancellationToken token)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                await chainStore.PutBlockAsync(block, token);
            }
        }

        protected override async Task PutTransactionAsyncCore(ITransaction transaction, CancellationToken token)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                await chainStore.PutTransactionAsync(transaction, token);
            }
        }
    }
}