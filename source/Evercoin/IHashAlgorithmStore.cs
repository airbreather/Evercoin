using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Evercoin
{
    /// <summary>
    /// Represents a store that contains all the
    /// <see cref="IHashAlgorithm"/> objects that we know about.
    /// </summary>
    [InheritedExport(typeof(IHashAlgorithmStore))]
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
    }
}
