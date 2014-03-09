using System;
using System.Collections.Generic;

namespace Evercoin.BaseImplementations
{
    /// <summary>
    /// Base class for implementations of <see cref="IHashAlgorithmStore"/>.
    /// </summary>
    public abstract class HashAlgorithmStoreBase : IHashAlgorithmStore
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

        public abstract bool TryGetHashAlgorithm(Guid identifier, out IHashAlgorithm hashAlgorithm);
    }
}
