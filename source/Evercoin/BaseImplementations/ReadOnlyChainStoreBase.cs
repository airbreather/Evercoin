using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.BaseImplementations
{
    [InheritedExport(typeof(IReadOnlyChainStore))]
    public abstract class ReadOnlyChainStoreBase : ChainStoreBase
    {
        protected sealed override void PutBlockCore(IBlock block)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override void PutTransactionCore(ITransaction transaction)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override Task PutBlockAsyncCore(IBlock block, CancellationToken token)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override Task PutTransactionAsyncCore(ITransaction transaction, CancellationToken token)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }
    }
}
