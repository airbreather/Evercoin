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
        public abstract IHashAlgorithm GetHashAlgorithm(Guid identifier);
        
        /// <summary>
        /// Registers a new <see cref="IHashAlgorithm"/>.
        /// </summary>
        /// <param name="identifier">
        /// A <see cref="Guid"/> value that can be used to retrieve the
        /// registered <see cref="IHashAlgorithm"/> in the future.
        /// </param>
        /// <param name="algorithm">
        /// The <see cref="IHashAlgorithm"/> to register.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// This store does not support registering new algorithms.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="algorithm"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="identifier"/> already identifies another
        /// <see cref="IHashAlgorithm"/> in this store that is not
        /// equal to <paramref name="algorithm"/>.
        /// </exception>
        /// <remarks>
        /// This is expected to be used rarely, for registering the algorithms
        /// for cryptocurrencies that Evercoin does not know about.
        /// </remarks>
        public virtual void RegisterHashAlgorithm(Guid identifier, IHashAlgorithm algorithm)
        {
            throw new NotSupportedException("This hash algorithm store is read-only.");
        }
    }
}
