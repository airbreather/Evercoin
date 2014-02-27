using System.ComponentModel.Composition;

namespace Evercoin.BaseImplementations
{
    [InheritedExport(typeof(IReadOnlyChainStore))]
    [InheritedExport(typeof(IChainStore))]
    public abstract class ReadWriteChainStoreBase : ChainStoreBase
    {
    }
}
