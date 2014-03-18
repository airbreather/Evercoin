using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

using Evercoin.BaseImplementations;

namespace Evercoin.App
{
    /// <summary>
    /// An implementation of <see cref="IHashAlgorithmStore"/> that aggregates
    /// multiple other stores together.
    /// </summary>
    internal sealed class CompositeHashAlgorithmStore : HashAlgorithmStoreBase
    {
        /// <summary>
        /// Backing field for <see cref="HashAlgorithmStores"/>.
        /// </summary>
        private readonly Collection<IHashAlgorithmStore> hashAlgorithmStores = new Collection<IHashAlgorithmStore>();

        /// <summary>
        /// Gets a collection of child hash algorithm stores.
        /// </summary>
        [ImportMany]
        public Collection<IHashAlgorithmStore> HashAlgorithmStores { get { return this.hashAlgorithmStores; } }

        /// <summary>
        /// Indicates whether this store contains
        /// an algorithm with the given identifier, storing a found algorithm
        /// to an out parameter on success.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the hash algorithm to search for.
        /// </param>
        /// <param name="hashAlgorithm">
        /// A location to store the algorithm, if it is found.
        /// </param>
        /// <returns>
        /// A value indicating whether this store contains
        /// an algorithm with the given identifier.
        /// </returns>
        public override bool TryGetHashAlgorithm(Guid identifier, out IHashAlgorithm hashAlgorithm)
        {
            IHashAlgorithm outParameterWorkaround = null;
            bool result = this.hashAlgorithmStores.Any(x => x.TryGetHashAlgorithm(identifier, out outParameterWorkaround));
            hashAlgorithm = outParameterWorkaround;
            return result;
        }
    }
}
