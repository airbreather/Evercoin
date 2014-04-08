using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.BaseImplementations
{
    public abstract class ReadOnlyChainStoreBase : ChainStoreBase
    {
        protected sealed override void PutBlockCore(FancyByteArray blockIdentifier, IBlock block)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override Task PutBlockAsyncCore(FancyByteArray blockIdentifier, IBlock block, CancellationToken token)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override Task PutTransactionAsyncCore(FancyByteArray transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }
    }
}
