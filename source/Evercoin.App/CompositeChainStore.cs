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
        private readonly Collection<IReadableChainStore> underlyingReadOnlyChainStores = new Collection<IReadableChainStore>();

        [ImportMany]
        public Collection<IChainStore> UnderlyingChainStores { get { return this.underlyingChainStores; } }

        [ImportMany]
        public Collection<IReadableChainStore> UnderlyingReadOnlyChainStores { get { return this.underlyingReadOnlyChainStores; } }

        protected override IBlock FindBlockCore(FancyByteArray blockIdentifier)
        {
            IBlock foundBlock = null;
            return this.underlyingReadOnlyChainStores.Any(x => x.TryGetBlock(blockIdentifier, out foundBlock)) ?
                   foundBlock :
                   null;
        }

        protected override ITransaction FindTransactionCore(FancyByteArray transactionIdentifier)
        {
            ITransaction foundTransaction = null;
            return this.underlyingReadOnlyChainStores.Any(x => x.TryGetTransaction(transactionIdentifier, out foundTransaction)) ?
                   foundTransaction :
                   null;
        }

        protected override void PutBlockCore(FancyByteArray blockIdentifier, IBlock block)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                chainStore.PutBlock(blockIdentifier, block);
            }
        }

        protected override void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                chainStore.PutTransaction(transactionIdentifier, transaction);
            }
        }

        protected override bool ContainsBlockCore(FancyByteArray blockIdentifier)
        {
            return this.underlyingReadOnlyChainStores.Any(x => x.ContainsBlock(blockIdentifier));
        }

        protected override bool ContainsTransactionCore(FancyByteArray transactionIdentifier)
        {
            return this.underlyingReadOnlyChainStores.Any(x => x.ContainsTransaction(transactionIdentifier));
        }

        protected override async Task<bool> ContainsBlockAsyncCore(FancyByteArray blockIdentifier, CancellationToken token)
        {
            IEnumerable<Task<bool>> searchTasks = this.underlyingReadOnlyChainStores.Select(x => x.ContainsBlockAsync(blockIdentifier, token));
            bool[] searchResults = await Task.WhenAll(searchTasks);
            return searchResults.Contains(true);
        }

        protected override async Task<bool> ContainsTransactionAsyncCore(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            IEnumerable<Task<bool>> searchTasks = this.underlyingReadOnlyChainStores.Select(x => x.ContainsTransactionAsync(transactionIdentifier, token));
            bool[] searchResults = await Task.WhenAll(searchTasks);
            return searchResults.Contains(true);
        }

        protected override async Task<IBlock> FindBlockAsyncCore(FancyByteArray blockIdentifier, CancellationToken token)
        {
            foreach (IReadableChainStore chainStore in this.underlyingReadOnlyChainStores)
            {
                IBlock foundBlock = await chainStore.GetBlockAsync(blockIdentifier, token);
                if (foundBlock != null)
                {
                    return foundBlock;
                }
            }

            return null;
        }

        protected override async Task<ITransaction> FindTransactionAsyncCore(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            foreach (IReadableChainStore chainStore in this.underlyingReadOnlyChainStores)
            {
                ITransaction foundTransaction = await chainStore.GetTransactionAsync(transactionIdentifier, token);
                if (foundTransaction != null)
                {
                    return foundTransaction;
                }
            }

            return null;
        }

        protected override async Task PutBlockAsyncCore(FancyByteArray blockIdentifier, IBlock block, CancellationToken token)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                await chainStore.PutBlockAsync(blockIdentifier, block, token);
            }
        }

        protected override async Task PutTransactionAsyncCore(FancyByteArray transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            foreach (IChainStore chainStore in this.underlyingChainStores)
            {
                await chainStore.PutTransactionAsync(transactionIdentifier, transaction, token);
            }
        }
    }
}