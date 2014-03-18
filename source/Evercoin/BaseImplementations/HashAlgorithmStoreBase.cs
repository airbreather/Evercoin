using System;
using System.Collections.Generic;

namespace Evercoin.BaseImplementations
{
    /// <summary>
    /// Base class for implementations of <see cref="IHashAlgorithmStore"/>.
    /// </summary>
    public abstract class HashAlgorithmStoreBase : DisposableObject, IHashAlgorithmStore
    {
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
        public IHashAlgorithm GetHashAlgorithm(Guid identifier)
        {
            IHashAlgorithm hashAlgorithm;
            if (!this.TryGetHashAlgorithm(identifier, out hashAlgorithm))
            {
                throw new KeyNotFoundException(identifier + " does not map to an algorithm we know about.");
            }

            return hashAlgorithm;
        }

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
        public abstract bool TryGetHashAlgorithm(Guid identifier, out IHashAlgorithm hashAlgorithm);
    }
}
