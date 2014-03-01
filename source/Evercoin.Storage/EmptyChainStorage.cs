using System.ComponentModel.Composition;

using Evercoin.BaseImplementations;

namespace Evercoin.Storage
{
    [Export(typeof(IReadOnlyChainStore))]
    [Export(typeof(IChainStore))]
    public sealed class EmptyChainStorage : ReadWriteChainStoreBase
    {
        protected override IBlock FindBlockCore(string blockIdentifier)
        {
            return null;
        }

        protected override ITransaction FindTransactionCore(string transactionIdentifier)
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
