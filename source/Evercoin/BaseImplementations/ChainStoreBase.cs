using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.BaseImplementations
{
    public abstract class ChainStoreBase : DisposableObject, IChainStore
    {
        public bool TryGetBlock(string blockIdentifier, out IBlock block)
        {
            this.ThrowIfDisposed();
            block = this.FindBlockCore(blockIdentifier);
            return block != null;
        }

        public bool TryGetTransaction(string transactionIdentifier, out ITransaction transaction)
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

        public IBlock GetBlock(string blockIdentifier)
        {
            this.ThrowIfDisposed();
            IBlock result = this.FindBlockCore(blockIdentifier);
            if (result == null)
            {
                throw new KeyNotFoundException("No block was found.");
            }

            return result;
        }

        public bool ContainsBlock(string blockIdentifier)
        {
            this.ThrowIfDisposed();
            return this.ContainsBlockCore(blockIdentifier);
        }

        public ITransaction GetTransaction(string transactionIdentifier)
        {
            this.ThrowIfDisposed();
            ITransaction result = this.FindTransactionCore(transactionIdentifier);
            if (result == null)
            {
                throw new KeyNotFoundException("No transaction was found.");
            }

            return result;
        }

        public bool ContainsTransaction(string transactionIdentifier)
        {
            this.ThrowIfDisposed();
            return this.ContainsTransactionCore(transactionIdentifier);
        }

        public async Task<bool> ContainsBlockAsync(string blockIdentifier)
        {
            return await this.ContainsBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public async Task<IBlock> GetBlockAsync(string blockIdentifier)
        {
            return await this.GetBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public async Task PutBlockAsync(IBlock block)
        {
            await this.PutBlockAsync(block, CancellationToken.None);
        }

        public async Task<bool> ContainsTransactionAsync(string transactionIdentifier)
        {
            return await this.ContainsTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public async Task<ITransaction> GetTransactionAsync(string transactionIdentifier)
        {
            return await this.GetTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public async Task PutTransactionAsync(ITransaction transaction)
        {
            await this.PutTransactionAsync(transaction, CancellationToken.None);
        }

        public async Task<bool> ContainsBlockAsync(string blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.ContainsBlockAsyncCore(blockIdentifier, token) != null;
        }

        public async Task<IBlock> GetBlockAsync(string blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.FindBlockAsyncCore(blockIdentifier, token);
        }

        public async Task PutBlockAsync(IBlock block, CancellationToken token)
        {
            this.ThrowIfDisposed();
            await this.PutBlockAsyncCore(block, token);
        }

        public async Task<bool> ContainsTransactionAsync(string transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.ContainsTransactionAsyncCore(transactionIdentifier, token);
        }

        public async Task<ITransaction> GetTransactionAsync(string transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return await this.FindTransactionAsyncCore(transactionIdentifier, token);
        }

        public async Task PutTransactionAsync(ITransaction transaction, CancellationToken token)
        {
            this.ThrowIfDisposed();
            await this.PutTransactionAsyncCore(transaction, token);
        }

        protected abstract IBlock FindBlockCore(string blockIdentifier);

        protected abstract ITransaction FindTransactionCore(string transactionIdentifier);

        protected abstract void PutBlockCore(IBlock block);

        protected abstract void PutTransactionCore(ITransaction transaction);

        protected virtual bool ContainsBlockCore(string blockIdentifier)
        {
            return this.FindBlockCore(blockIdentifier) != null;
        }

        protected virtual bool ContainsTransactionCore(string transactionIdentifier)
        {
            return this.FindTransactionCore(transactionIdentifier) != null;
        }

        protected virtual async Task<bool> ContainsBlockAsyncCore(string blockIdentifier, CancellationToken token)
        {
            return await this.FindBlockAsyncCore(blockIdentifier, token) != null;
        }

        protected virtual async Task<bool> ContainsTransactionAsyncCore(string transactionIdentifier, CancellationToken token)
        {
            return await this.FindTransactionAsyncCore(transactionIdentifier, token) != null;
        }

        protected virtual async Task<IBlock> FindBlockAsyncCore(string blockIdentifier, CancellationToken token)
        {
            return await Task.Run(() => this.FindBlockCore(blockIdentifier), token);
        }

        protected virtual async Task<ITransaction> FindTransactionAsyncCore(string transactionIdentifier, CancellationToken token)
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
