using System.Numerics;

using Evercoin.Util;

namespace Evercoin.BaseImplementations
{
    internal sealed class TypedCoinbaseValueSource : TypedValueSource, ICoinbaseValueSource
    {
        /// <summary>
        /// Gets the <see cref="IBlock"/> that created this value source.
        /// </summary>
        public BigInteger OriginatingBlockIdentifier { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ICoinbaseValueSource);
        }

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
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder(base.GetHashCode())
                .HashWith(this.OriginatingBlockIdentifier);

            return builder;
        }
    }
}
