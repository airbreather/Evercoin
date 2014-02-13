using System;

namespace Evercoin
{
    /// <summary>
    /// An <see cref="IValueSource"/> that comes from the block reward and all
    /// transaction fees for transactions included in the block.
    /// </summary>
    public interface ICoinbaseValueSource : IValueSource, IEquatable<ICoinbaseValueSource>
    {
        /// <summary>
        /// Gets the coinbase data.
        /// </summary>
        byte[] CoinbaseData { get; }
    }
}
