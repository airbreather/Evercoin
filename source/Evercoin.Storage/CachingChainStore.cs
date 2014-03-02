﻿using System;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    [Export(typeof(IReadOnlyChainStore))]
    public sealed class CachingChainStorage : ReadWriteChainStoreBase
    {
        private readonly IChainStore underlyingChainStore;

        private readonly IDisposable underlyingCache;

        private readonly BlockCache blockCache;

        private readonly TransactionCache transactionCache;

        [ImportingConstructor]
        public CachingChainStorage([Import("UncachedChainStore")] IChainStore underlyingChainStore)
        {
            this.underlyingChainStore = underlyingChainStore;
            MemoryCache memoryCache = MemoryCache.Default;
            try
            {
                this.underlyingCache = memoryCache;
                this.blockCache = new BlockCache(memoryCache);
                this.transactionCache = new TransactionCache(memoryCache);
            }
            catch
            {
                memoryCache.Dispose();
                throw;
            }
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            IBlock foundBlock;
            if (this.blockCache.TryGetBlock(blockIdentifier, out foundBlock))
            {
                return foundBlock;
            }

            foundBlock = this.underlyingChainStore.GetBlock(blockIdentifier);
            if (foundBlock != null)
            {
                this.blockCache.PutBlock(foundBlock);
            }

            return foundBlock;
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            ITransaction foundTransaction;
            if (this.transactionCache.TryGetTransaction(transactionIdentifier, out foundTransaction))
            {
                return foundTransaction;
            }

            foundTransaction = this.underlyingChainStore.GetTransaction(transactionIdentifier);
            if (foundTransaction != null)
            {
                this.transactionCache.PutTransaction(foundTransaction);
            }

            return foundTransaction;
        }

        protected override void PutBlockCore(IBlock block)
        {
            this.blockCache.PutBlock(block);
            this.underlyingChainStore.PutBlock(block);
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
            this.transactionCache.PutTransaction(transaction);
            this.underlyingChainStore.PutTransaction(transaction);
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.blockCache.ContainsBlock(blockIdentifier) ||
                   this.underlyingChainStore.ContainsBlock(blockIdentifier);
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.transactionCache.ContainsTransaction(transactionIdentifier) ||
                   this.underlyingChainStore.ContainsTransaction(transactionIdentifier);
        }

        protected override async Task<bool> ContainsBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return this.blockCache.ContainsBlock(blockIdentifier) ||
                   await this.underlyingChainStore.ContainsBlockAsync(blockIdentifier, token);
        }

        protected override async Task<bool> ContainsTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return this.transactionCache.ContainsTransaction(transactionIdentifier) ||
                   await this.underlyingChainStore.ContainsTransactionAsync(transactionIdentifier, token);
        }

        protected override async Task<IBlock> FindBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            IBlock foundBlock;
            if (this.blockCache.TryGetBlock(blockIdentifier, out foundBlock))
            {
                return foundBlock;
            }

            foundBlock = await this.underlyingChainStore.GetBlockAsync(blockIdentifier, token);
            if (foundBlock != null)
            {
                this.blockCache.PutBlock(foundBlock);
            }

            return foundBlock;
        }

        protected override async Task<ITransaction> FindTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            ITransaction foundTransaction;
            if (this.transactionCache.TryGetTransaction(transactionIdentifier, out foundTransaction))
            {
                return foundTransaction;
            }

            foundTransaction = await this.underlyingChainStore.GetTransactionAsync(transactionIdentifier, token);
            if (foundTransaction != null)
            {
                this.transactionCache.PutTransaction(foundTransaction);
            }

            return foundTransaction;
        }

        protected override async Task PutBlockAsyncCore(IBlock block, CancellationToken token)
        {
            this.blockCache.PutBlock(block);
            await this.underlyingChainStore.PutBlockAsync(block, token);
        }

        protected override async Task PutTransactionAsyncCore(ITransaction transaction, CancellationToken token)
        {
            this.transactionCache.PutTransaction(transaction);
            await this.underlyingChainStore.PutTransactionAsync(transaction, token);
        }

        protected override void DisposeManagedResources()
        {
            this.underlyingCache.Dispose();
            this.underlyingChainStore.Dispose();
        }
    }
}