using System.Collections.Generic;
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

        public void PutBlock(BigInteger blockIdentifier, IBlock block)
        {
            this.ThrowIfDisposed();
            this.PutBlockCore(blockIdentifier, block);
        }

        public void PutTransaction(BigInteger transactionIdentifier, ITransaction transaction)
        {
            this.ThrowIfDisposed();
            this.PutTransactionCore(transactionIdentifier, transaction);
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

        public Task<bool> ContainsBlockAsync(BigInteger blockIdentifier)
        {
            return this.ContainsBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public Task<IBlock> GetBlockAsync(BigInteger blockIdentifier)
        {
            return this.GetBlockAsync(blockIdentifier, CancellationToken.None);
        }

        public Task PutBlockAsync(BigInteger blockIdentifier, IBlock block)
        {
            return this.PutBlockAsync(blockIdentifier, block, CancellationToken.None);
        }

        public Task<bool> ContainsTransactionAsync(BigInteger transactionIdentifier)
        {
            return this.ContainsTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public Task<ITransaction> GetTransactionAsync(BigInteger transactionIdentifier)
        {
            return this.GetTransactionAsync(transactionIdentifier, CancellationToken.None);
        }

        public Task PutTransactionAsync(BigInteger transactionIdentifier, ITransaction transaction)
        {
            return this.PutTransactionAsync(transactionIdentifier, transaction, CancellationToken.None);
        }

        public Task<bool> ContainsBlockAsync(BigInteger blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.ContainsBlockAsyncCore(blockIdentifier, token);
        }

        public Task<IBlock> GetBlockAsync(BigInteger blockIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.FindBlockAsyncCore(blockIdentifier, token);
        }

        public Task PutBlockAsync(BigInteger blockIdentifier, IBlock block, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.PutBlockAsyncCore(blockIdentifier, block, token);
        }

        public Task<bool> ContainsTransactionAsync(BigInteger transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.ContainsTransactionAsyncCore(transactionIdentifier, token);
        }

        public Task<ITransaction> GetTransactionAsync(BigInteger transactionIdentifier, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.FindTransactionAsyncCore(transactionIdentifier, token);
        }

        public Task PutTransactionAsync(BigInteger transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            this.ThrowIfDisposed();
            return this.PutTransactionAsyncCore(transactionIdentifier, transaction, token);
        }

        protected abstract IBlock FindBlockCore(BigInteger blockIdentifier);

        protected abstract ITransaction FindTransactionCore(BigInteger transactionIdentifier);

        protected abstract void PutBlockCore(BigInteger blockIdentifier, IBlock block);

        protected abstract void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction);

        protected virtual bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.FindBlockCore(blockIdentifier) != null;
        }

        protected virtual bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.FindTransactionCore(transactionIdentifier) != null;
        }

        protected virtual Task<bool> ContainsBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.ContainsBlockCore(blockIdentifier), token);
        }

        protected virtual Task<bool> ContainsTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.ContainsTransactionCore(transactionIdentifier), token);
        }

        protected virtual Task<IBlock> FindBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.FindBlockCore(blockIdentifier), token);
        }

        protected virtual Task<ITransaction> FindTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return Task.Run(() => this.FindTransactionCore(transactionIdentifier), token);
        }

        protected virtual Task PutBlockAsyncCore(BigInteger blockIdentifier, IBlock block, CancellationToken token)
        {
            return Task.Run(() => this.PutBlockCore(blockIdentifier, block), token);
        }

        protected virtual Task PutTransactionAsyncCore(BigInteger transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            return Task.Run(() => this.PutTransactionCore(transactionIdentifier, transaction), token);
        }
    }
}
