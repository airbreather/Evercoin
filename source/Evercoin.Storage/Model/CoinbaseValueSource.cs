using System;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [Serializable]
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

        private CoinbaseValueSource(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.OriginatingBlockIdentifier = info.GetValue<BigInteger>(SerializationName_OriginatingBlockIdentifier);
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

        public BigInteger OriginatingBlockIdentifier { get; set; }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(SerializationName_OriginatingBlockIdentifier, this.OriginatingBlockIdentifier);
        }
    }
}