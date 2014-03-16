﻿using System;
using System.Numerics;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;

namespace Evercoin.App
{
    internal sealed class CachingChainStorage : ReadWriteChainStoreBase
    {
        private readonly IChainStore underlyingChainStore;

        private readonly IDisposable underlyingCache;

        private readonly BlockCache blockCache;

        private readonly TransactionCache transactionCache;

        public CachingChainStorage(IChainStore underlyingChainStore)
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
                this.blockCache.PutBlock(blockIdentifier, foundBlock);
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
                this.transactionCache.PutTransaction(transactionIdentifier, foundTransaction);
            }

            return foundTransaction;
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            this.blockCache.PutBlock(blockIdentifier, block);
            this.underlyingChainStore.PutBlock(blockIdentifier, block);
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            this.transactionCache.PutTransaction(transactionIdentifier, transaction);
            this.underlyingChainStore.PutTransaction(transactionIdentifier, transaction);
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
                this.blockCache.PutBlock(blockIdentifier, foundBlock);
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
                this.transactionCache.PutTransaction(transactionIdentifier, foundTransaction);
            }

            return foundTransaction;
        }

        protected override async Task PutBlockAsyncCore(BigInteger blockIdentifier, IBlock block, CancellationToken token)
        {
            this.blockCache.PutBlock(blockIdentifier, block);
            await this.underlyingChainStore.PutBlockAsync(blockIdentifier, block, token);
        }

        protected override async Task PutTransactionAsyncCore(BigInteger transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            this.transactionCache.PutTransaction(transactionIdentifier, transaction);
            await this.underlyingChainStore.PutTransactionAsync(transactionIdentifier, transaction, token);
        }

        protected override void DisposeManagedResources()
        {
            this.underlyingCache.Dispose();
            this.underlyingChainStore.Dispose();
        }
    }
}