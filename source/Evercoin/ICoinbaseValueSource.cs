using System;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// Represents an <see cref="IValueSource"/> from a block.
    /// </summary>
    public interface ICoinbaseValueSource : IValueSource, IEquatable<ICoinbaseValueSource>
    {
        /// <summary>
        /// Gets the <see cref="IBlock"/> that created this value source.
        /// </summary>
        BigInteger OriginatingBlockIdentifier { get; }
    }
}
