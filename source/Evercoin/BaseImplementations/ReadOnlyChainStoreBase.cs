using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.BaseImplementations
{
    public abstract class ReadOnlyChainStoreBase : ChainStoreBase
    {
        protected sealed override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override Task PutBlockAsyncCore(BigInteger blockIdentifier, IBlock block, CancellationToken token)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }

        protected sealed override Task PutTransactionAsyncCore(BigInteger transactionIdentifier, ITransaction transaction, CancellationToken token)
        {
            throw new NotSupportedException("This chain store is read-only.");
        }
    }
}
