using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;

namespace Evercoin.App
{
    internal sealed class CachingChainStorage : ReadWriteChainStoreBase
    {
        private readonly IChainStore underlyingChainStore;

        private readonly IDisposable underlyingBlockCache;

        private readonly IDisposable underlyingTxCache;

        private readonly Cache<IBlock> blockCache;

        private readonly Cache<ITransaction> transactionCache;

        public CachingChainStorage(IChainStore underlyingChainStore, IChainSerializer chainSerializer)
        {
            this.underlyingChainStore = underlyingChainStore;
            MemoryCache blockMemoryCache = new MemoryCache("blocks");
            MemoryCache txMemoryCache = new MemoryCache("transactions");
            try
            {
                this.underlyingBlockCache = blockMemoryCache;
                this.underlyingTxCache = txMemoryCache;
                this.blockCache = new Cache<IBlock>(blockMemoryCache, 300000, chainSerializer.GetBytesForBlock, x => chainSerializer.GetBlockForBytes(x.Value));
                this.transactionCache = new Cache<ITransaction>(txMemoryCache, 40000, chainSerializer.GetBytesForTransaction, x => chainSerializer.GetTransactionForBytes(x.Value));
            }
            catch
            {
                blockMemoryCache.Dispose();
                txMemoryCache.Dispose();
                throw;
            }
        }

        protected override IBlock FindBlockCore(FancyByteArray blockIdentifier)
        {
            IBlock foundBlock;
            if (this.blockCache.TryGetValue(blockIdentifier, out foundBlock))
            {
                return foundBlock;
            }

            foundBlock = this.underlyingChainStore.GetBlock(blockIdentifier);
            if (foundBlock != null)
            {
                this.blockCache.Put(blockIdentifier, foundBlock);
            }

            return foundBlock;
        }

        protected override ITransaction FindTransactionCore(FancyByteArray transactionIdentifier)
        {
            ITransaction foundTransaction;
            if (this.transactionCache.TryGetValue(transactionIdentifier, out foundTransaction))
            {
                return foundTransaction;
            }

            foundTransaction = this.underlyingChainStore.GetTransaction(transactionIdentifier);
            if (foundTransaction != null)
            {
                this.transactionCache.Put(transactionIdentifier, foundTransaction);
            }

            return foundTransaction;
        }

        protected override void PutBlockCore(FancyByteArray blockIdentifier, IBlock block)
        {
            this.blockCache.Put(blockIdentifier, block);
            this.underlyingChainStore.PutBlock(blockIdentifier, block);
        }

        protected override void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            this.transactionCache.Put(transactionIdentifier, transaction);
            this.underlyingChainStore.PutTransaction(transactionIdentifier, transaction);
        }

        protected override bool ContainsBlockCore(FancyByteArray blockIdentifier)
        {
            return this.blockCache.Contains(blockIdentifier) ||
                   this.underlyingChainStore.ContainsBlock(blockIdentifier);
        }

        protected override bool ContainsTransactionCore(FancyByteArray transactionIdentifier)
        {
            return this.transactionCache.Contains(transactionIdentifier) ||
                   this.underlyingChainStore.ContainsTransaction(transactionIdentifier);
        }

        protected override async Task<bool> ContainsBlockAsyncCore(FancyByteArray blockIdentifier, CancellationToken token)
        {
            return this.blockCache.Contains(blockIdentifier) ||
                   await this.underlyingChainStore.ContainsBlockAsync(blockIdentifier, token);
        }

        protected override async Task<bool> ContainsTransactionAsyncCore(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            return this.transactionCache.Contains(transactionIdentifier) ||
                   await this.underlyingChainStore.ContainsTransactionAsync(transactionIdentifier, token);
        }

        protected override async Task<IBlock> FindBlockAsyncCore(FancyByteArray blockIdentifier, CancellationToken token)
        {
            IBlock foundBlock;
            if (this.blockCache.TryGetValue(blockIdentifier, out foundBlock))
            {
                return foundBlock;
            }

            foundBlock = await this.underlyingChainStore.GetBlockAsync(blockIdentifier, token);
            if (foundBlock != null)
            {
                this.blockCache.Put(blockIdentifier, foundBlock);
            }

            return foundBlock;
        }

        protected override async Task<ITransaction> FindTransactionAsyncCore(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            ITransaction foundTransaction;
            if (this.transactionCache.TryGetValue(transactionIdentifier, out foundTransaction))
            {
                return foundTransaction;
            }

            foundTransaction = await this.underlyingChainStore.GetTransactionAsync(transactionIdentifier, token);
            if (foundTransaction != null)
            {
                this.transactionCache.Put(transactionIdentifier, foundTransaction);
            }

            return foundTransaction;
        }

        protected override async Task PutBlockAsyncCore(FancyByteArray blockIdentifier, IBlock block, CancellationToken token)
        {
            this.blockCache.Put(blockIdentifier, block);
            await this.underlyingChainStore.PutBlockAsync(blockIdentifier, block, token);
        }

        protected override async Task PutTransactionAsyncCore(FancyByteArray transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            this.transactionCache.Put(transactionIdentifier, transaction);
            await this.underlyingChainStore.PutTransactionAsync(transactionIdentifier, transaction, token);
        }

        protected override void DisposeManagedResources()
        {
            this.underlyingBlockCache.Dispose();
            this.underlyingTxCache.Dispose();
            this.underlyingChainStore.Dispose();
        }
    }
}