using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "CoinbaseValueSource", Namespace = "Evercoin.Storage.Model")]
    internal sealed class CoinbaseValueSource : ValueSource, ICoinbaseValueSource
    {
        private const string SerializationName_OriginatingBlockIdentifier = "OriginatingBlockIdentifier";

        public CoinbaseValueSource()
        {
        }

        public CoinbaseValueSource(ICoinbaseValueSource copyFrom)
            : base(copyFrom)
        {
            this.OriginatingBlockIdentifier = copyFrom.OriginatingBlockIdentifier;
        }

        [DataMember(Name = SerializationName_OriginatingBlockIdentifier)]
        public BigInteger OriginatingBlockIdentifier { get; set; }

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
    }
}