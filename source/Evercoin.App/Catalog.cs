using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Evercoin.App
{
    internal sealed class Catalog
    {
        [ImportMany(typeof(IChainStore))]
        private readonly List<IChainStore> chainStores = new List<IChainStore>();

        [ImportMany(typeof(IReadOnlyChainStore))]
        private readonly List<IReadOnlyChainStore> readOnlyChainStores = new List<IReadOnlyChainStore>();

        [ImportMany(typeof(IHashAlgorithmStore))]
        private readonly List<IHashAlgorithmStore> hashAlgorithmStores = new List<IHashAlgorithmStore>();

        public IReadOnlyList<IChainStore> ChainStores { get { return this.chainStores; } }
        public IReadOnlyList<IReadOnlyChainStore> ReadOnlyChainStores { get { return this.readOnlyChainStores; } }
        public IReadOnlyList<IHashAlgorithmStore> HashAlgorithmStores { get { return this.hashAlgorithmStores; } } 
    }
}
