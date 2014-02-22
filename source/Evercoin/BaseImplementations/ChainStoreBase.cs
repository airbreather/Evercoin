using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Evercoin.BaseImplementations
{
    [InheritedExport(typeof(IReadOnlyChainStore))]
    public abstract class ChainStoreBase : IChainStore
    {
        public abstract bool TryGetBlock(string blockIdentifier, out IBlock block);

        public abstract bool TryGetTransaction(string transactionIdentifier, out ITransaction transaction);

        public virtual IBlock GetBlock(string blockIdentifier)
        {
            IBlock result;
            if (!this.TryGetBlock(blockIdentifier, out result))
            {
                throw new KeyNotFoundException("No block was found.");
            }

            return result;
        }

        public virtual bool ContainsBlock(string blockIdentifier)
        {
            IBlock _;
            return this.TryGetBlock(blockIdentifier, out _);
        }

        public virtual ITransaction GetTransaction(string transactionIdentifier)
        {
            ITransaction result;
            if (!this.TryGetTransaction(transactionIdentifier, out result))
            {
                throw new KeyNotFoundException("No transaction was found.");
            }

            return result;
        }

        public virtual bool ContainsTransaction(string transactionIdentifier)
        {
            ITransaction _;
            return this.TryGetTransaction(transactionIdentifier, out _);
        }

        public virtual async Task<IBlock> GetBlockAsync(string blockIdentifier)
        {
            return await Task.Run(() => this.GetBlock(blockIdentifier));
        }

        public virtual async Task PutBlockAsync(IBlock block)
        {
            await Task.Run(() => this.PutBlock(block));
        }

        public virtual async Task<ITransaction> GetTransactionAsync(string transactionIdentifier)
        {
            return await Task.Run(() => this.GetTransaction(transactionIdentifier));
        }

        public virtual async Task PutTransactionAsync(ITransaction transaction)
        {
            await Task.Run(() => this.PutTransaction(transaction));
        }

        public virtual void PutBlock(IBlock block)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        public virtual void PutTransaction(ITransaction transaction)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }
    }
}
