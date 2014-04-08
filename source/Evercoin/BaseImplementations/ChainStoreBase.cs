using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.BaseImplementations
{
    public abstract class ChainStoreBase : DisposableObject, IChainStore
    {
        public bool TryGetBlock(FancyByteArray blockIdentifier, out IBlock block)
        {
            this.ThrowIfDisposed();
            block = this.FindBlockCore(blockIdentifier);
            return block != null;
        }

        public bool TryGetTransaction(FancyByteArray transactionIdentifier, out ITransaction transaction)
        {
            this.ThrowIfDisposed();
            transaction = this.FindTransactionCore(transactionIdentifier);
            return transaction != null;
        }

        public void PutBlock(FancyByteArray blockIdentifier, IBlock block)
        {
            this.ThrowIfDisposed();
            this.PutBlockCore(blockIdentifier, block);
        }

        public void PutTransaction(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            this.ThrowIfDisposed();
            this.PutTransactionCore(transactionIdentifier, transaction);
        }

        public IBlock GetBlock(FancyByteArray blockIdentifier)
        {
            this.ThrowIfDisposed();
            IBlock result = this.FindBlockCore(blockIdentifier);
            if (result == null)
            {
                throw new KeyNotFoundException("No block was found.");
            }

            return result;
        }

        public bool ContainsBlock(FancyByteArray blockIdentifier)
        {
            this.ThrowIfDisposed();
            return this.ContainsBlockCore(blockIdentifier);
        }

        public ITransaction GetTransaction(FancyByteArray transactionIdentifier)
        {
            this.ThrowIfDisposed();
            ITransaction result = this.FindTransactionCore(transactionIdentifier);
            if (result == null)
            {
                throw new KeyNotFoundException("No transaction was found.");
            }

            return result;
        }

        public bool ContainsTransaction(FancyByteArray transactionIdentifier)
        {
            this.ThrowIfDisposed();
            return this.ContainsTransactionCore(transactionIdentifier);
        }

        public Task<bool> ContainsBlockAsync(FancyByteArray blockIdentifier)
        {
            return this.ContainsBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public Task<IBlock> GetBlockAsync(FancyByteArray blockIdentifier)
        {
            return this.GetBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public Task PutBlockAsync(FancyByteArray blockIdentifier, IBlock block)
        {
            return this.PutBlockAsync(blockIdentifier, block, CancellationToken.None);
        }

        public Task<bool> ContainsTransactionAsync(FancyByteArray transactionIdentifier)
        {
            return this.ContainsTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public Task<ITransaction> GetTransactionAsync(FancyByteArray transactionIdentifier)
        {
            return this.GetTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public Task PutTransactionAsync(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            return this.PutTransactionAsync(transactionIdentifier, transaction, CancellationToken.None);
        }

        public Task<bool> ContainsBlockAsync(FancyByteArray blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.ContainsBlockAsyncCore(blockIdentifier, token);
        }

        public Task<IBlock> GetBlockAsync(FancyByteArray blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.FindBlockAsyncCore(blockIdentifier, token);
        }

        public Task PutBlockAsync(FancyByteArray blockIdentifier, IBlock block, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.PutBlockAsyncCore(blockIdentifier, block, token);
        }

        public Task<bool> ContainsTransactionAsync(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.ContainsTransactionAsyncCore(transactionIdentifier, token);
        }

        public Task<ITransaction> GetTransactionAsync(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.FindTransactionAsyncCore(transactionIdentifier, token);
        }

        public Task PutTransactionAsync(FancyByteArray transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.PutTransactionAsyncCore(transactionIdentifier, transaction, token);
        }

        protected abstract IBlock FindBlockCore(FancyByteArray blockIdentifier);

        protected abstract ITransaction FindTransactionCore(FancyByteArray transactionIdentifier);

        protected abstract void PutBlockCore(FancyByteArray blockIdentifier, IBlock block);

        protected abstract void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction);

        protected virtual bool ContainsBlockCore(FancyByteArray blockIdentifier)
        {
            return this.FindBlockCore(blockIdentifier) != null;
        }

        protected virtual bool ContainsTransactionCore(FancyByteArray transactionIdentifier)
        {
            return this.FindTransactionCore(transactionIdentifier) != null;
        }

        protected virtual Task<bool> ContainsBlockAsyncCore(FancyByteArray blockIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.ContainsBlockCore(blockIdentifier), token);
        }

        protected virtual Task<bool> ContainsTransactionAsyncCore(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.ContainsTransactionCore(transactionIdentifier), token);
        }

        protected virtual Task<IBlock> FindBlockAsyncCore(FancyByteArray blockIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.FindBlockCore(blockIdentifier), token);
        }

        protected virtual Task<ITransaction> FindTransactionAsyncCore(FancyByteArray transactionIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.FindTransactionCore(transactionIdentifier), token);
        }

        protected virtual Task PutBlockAsyncCore(FancyByteArray blockIdentifier, IBlock block, CancellationToken token)
        {
            return Task.Run(() => this.PutBlockCore(blockIdentifier, block), token);
        }

        protected virtual Task PutTransactionAsyncCore(FancyByteArray transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            return Task.Run(() => this.PutTransactionCore(transactionIdentifier, transaction), token);
        }
    }
}
