using System.Numerics;

namespace Evercoin.Network
{
    internal sealed class NetworkCoinbaseValueSource : NetworkValueSource, ICoinbaseValueSource
    {
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(ICoinbaseValueSource other)
        {
            return base.Equals(other) &&
                   this.OriginatingBlockIdentifier == other.OriginatingBlockIdentifier;
        }

        /// <summary>
        /// Gets the <see cref="IBlock"/> that created this value source.
        /// </summary>
        public BigInteger OriginatingBlockIdentifier { get; set; }
    }
}
