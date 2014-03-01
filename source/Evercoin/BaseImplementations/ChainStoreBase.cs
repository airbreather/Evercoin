﻿using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.BaseImplementations
{
    public abstract class ChainStoreBase : DisposableObject, IChainStore
    {
        public bool TryGetBlock(BigInteger blockIdentifier, out IBlock block)
        {
            this.ThrowIfDisposed();
            block = this.FindBlockCore(blockIdentifier);
            return block != null;
        }

        public bool TryGetTransaction(BigInteger transactionIdentifier, out ITransaction transaction)
        {
            this.ThrowIfDisposed();
            transaction = this.FindTransactionCore(transactionIdentifier);
            return transaction != null;
        }

        public void PutBlock(IBlock block)
        {
            this.ThrowIfDisposed();
            this.PutBlockCore(block);
        }

        public void PutTransaction(ITransaction transaction)
        {
            this.ThrowIfDisposed();
            this.PutTransactionCore(transaction);
        }

        public IBlock GetBlock(BigInteger blockIdentifier)
        {
            this.ThrowIfDisposed();
            IBlock result = this.FindBlockCore(blockIdentifier);
            if (result == null)
            {
                throw new KeyNotFoundException("No block was found.");
            }

            return result;
        }

        public bool ContainsBlock(BigInteger blockIdentifier)
        {
            this.ThrowIfDisposed();
            return this.ContainsBlockCore(blockIdentifier);
        }

        public ITransaction GetTransaction(BigInteger transactionIdentifier)
        {
            this.ThrowIfDisposed();
            ITransaction result = this.FindTransactionCore(transactionIdentifier);
            if (result == null)
            {
                throw new KeyNotFoundException("No transaction was found.");
            }

            return result;
        }

        public bool ContainsTransaction(BigInteger transactionIdentifier)
        {
            this.ThrowIfDisposed();
            return this.ContainsTransactionCore(transactionIdentifier);
        }

        public async Task<bool> ContainsBlockAsync(BigInteger blockIdentifier)
        {
            return await this.ContainsBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public async Task<IBlock> GetBlockAsync(BigInteger blockIdentifier)
        {
            return await this.GetBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public async Task PutBlockAsync(IBlock block)
        {
            await this.PutBlockAsync(block, CancellationToken.None);
        }

        public async Task<bool> ContainsTransactionAsync(BigInteger transactionIdentifier)
        {
            return await this.ContainsTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public async Task<ITransaction> GetTransactionAsync(BigInteger transactionIdentifier)
        {
            return await this.GetTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public async Task PutTransactionAsync(ITransaction transaction)
        {
            await this.PutTransactionAsync(transaction, CancellationToken.None);
        }

        public async Task<bool> ContainsBlockAsync(BigInteger blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.ContainsBlockAsyncCore(blockIdentifier, token);
        }

        public async Task<IBlock> GetBlockAsync(BigInteger blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.FindBlockAsyncCore(blockIdentifier, token);
        }

        public async Task PutBlockAsync(IBlock block, CancellationToken token)
        {
            this.ThrowIfDisposed();
            await this.PutBlockAsyncCore(block, token);
        }

        public async Task<bool> ContainsTransactionAsync(BigInteger transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.ContainsTransactionAsyncCore(transactionIdentifier, token);
        }

        public async Task<ITransaction> GetTransactionAsync(BigInteger transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.FindTransactionAsyncCore(transactionIdentifier, token);
        }

        public async Task PutTransactionAsync(ITransaction transaction, CancellationToken token)
        {
            this.ThrowIfDisposed();
            await this.PutTransactionAsyncCore(transaction, token);
        }

        protected abstract IBlock FindBlockCore(BigInteger blockIdentifier);

        protected abstract ITransaction FindTransactionCore(BigInteger transactionIdentifier);

        protected abstract void PutBlockCore(IBlock block);

        protected abstract void PutTransactionCore(ITransaction transaction);

        protected virtual bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.FindBlockCore(blockIdentifier) != null;
        }

        protected virtual bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.FindTransactionCore(transactionIdentifier) != null;
        }

        protected virtual async Task<bool> ContainsBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return await this.FindBlockAsyncCore(blockIdentifier, token) != null;
        }

        protected virtual async Task<bool> ContainsTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return await this.FindTransactionAsyncCore(transactionIdentifier, token) != null;
        }

        protected virtual async Task<IBlock> FindBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return await Task.Run(() => this.FindBlockCore(blockIdentifier), token);
        }

        protected virtual async Task<ITransaction> FindTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return await Task.Run(() => this.FindTransactionCore(transactionIdentifier), token);
        }

        protected virtual async Task PutBlockAsyncCore(IBlock block, CancellationToken token)
        {
            await Task.Run(() => this.PutBlockCore(block), token);
        }

        protected virtual async Task PutTransactionAsyncCore(ITransaction transaction, CancellationToken token)
        {
            await Task.Run(() => this.PutTransactionCore(transaction), token);
        }
    }
}
