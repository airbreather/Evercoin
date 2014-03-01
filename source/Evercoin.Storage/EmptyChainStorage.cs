using System.ComponentModel.Composition;
using System.Numerics;

using Evercoin.BaseImplementations;

namespace Evercoin.Storage
{
    [Export(typeof(IReadOnlyChainStore))]
    [Export(typeof(IChainStore))]
    public sealed class EmptyChainStorage : ReadWriteChainStoreBase
    {
        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            return null;
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            return null;
        }

        protected override void PutBlockCore(IBlock block)
        {
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
        }
    }
}
