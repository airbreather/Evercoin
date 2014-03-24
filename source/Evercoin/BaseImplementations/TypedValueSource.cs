using System.Linq;
using System.Numerics;

using Evercoin.Util;

namespace Evercoin.BaseImplementations
{
    internal sealed class TypedValueSource : IValueSource
    {
        /// <summary>
        /// Gets a value indicating whether this is the coinbase value source
        /// that gets created as a subsidy for miners.
        /// </summary>
        public bool IsCoinbase { get { return this.OriginatingTransactionIdentifier.IsZero && this.OriginatingTransactionOutputIndex == 0; } }

        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        public decimal AvailableValue { get; set; }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        public BigInteger OriginatingTransactionIdentifier { get; set; }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        public uint OriginatingTransactionOutputIndex { get; set; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        public byte[] ScriptPublicKey { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as IValueSource);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWith(this.AvailableValue)
                .HashWith(this.OriginatingTransactionIdentifier)
                .HashWith(this.OriginatingTransactionOutputIndex)
                .HashWithEnumerable(this.ScriptPublicKey);

            return builder;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IValueSource other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.AvailableValue == other.AvailableValue &&
                   this.OriginatingTransactionIdentifier == other.OriginatingTransactionIdentifier &&
                   this.OriginatingTransactionOutputIndex == other.OriginatingTransactionOutputIndex &&
                   this.ScriptPublicKey.SequenceEqual(other.ScriptPublicKey);
        }
    }
}
