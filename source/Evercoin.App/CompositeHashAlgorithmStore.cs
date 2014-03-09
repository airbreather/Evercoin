using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

using Evercoin.BaseImplementations;

namespace Evercoin.App
{
    internal sealed class CompositeHashAlgorithmStore : HashAlgorithmStoreBase
    {
        private readonly Collection<IHashAlgorithmStore> hashAlgorithmStores = new Collection<IHashAlgorithmStore>();

        [ImportMany]
        public Collection<IHashAlgorithmStore> HashAlgorithmStores { get { return this.hashAlgorithmStores; } }

        /// <summary>
        /// Gets the <see cref="IHashAlgorithm"/> identified by a given
        /// <see cref="Guid"/>.
        /// </summary>
        /// <param name="identifier">
        /// The <see cref="Guid"/> that identifies the
        /// <see cref="IHashAlgorithm"/> value to get.
        /// </param>
        /// <returns>
        /// The <see cref="IHashAlgorithm"/> identified by
        /// <paramref name="identifier"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="identifier"/> does not map to an algorithm
        /// that we know about.
        /// </exception>
        public override bool TryGetHashAlgorithm(Guid identifier, out IHashAlgorithm hashAlgorithm)
        {
            IHashAlgorithm compilerWorkaround = null;
            bool result = this.hashAlgorithmStores.Any(x => x.TryGetHashAlgorithm(identifier, out compilerWorkaround));
            hashAlgorithm = compilerWorkaround;
            return result;
        }
    }
}
