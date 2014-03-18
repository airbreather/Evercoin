using System;
using System.Collections.Generic;

namespace Evercoin
{
    /// <summary>
    /// Represents a store that contains all the
    /// <see cref="IHashAlgorithm"/> objects that we know about.
    /// </summary>
    public interface IHashAlgorithmStore
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
        IHashAlgorithm GetHashAlgorithm(Guid identifier);

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
        bool TryGetHashAlgorithm(Guid identifier, out IHashAlgorithm hashAlgorithm);
    }
}
