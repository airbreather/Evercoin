using System.Linq;
using System.Numerics;

using Evercoin.Util;

namespace Evercoin.BaseImplementations
{
    public sealed class TypedValueSpender : IValueSpender
    {
        public BigInteger SpentTransactionIdentifier { get; set; }

        public uint SpentTransactionOutputIndex { get; set; }

        /// <summary>
        /// The identifier of the <see cref="ITransaction"/> that
        /// contains this spender in its inputs.
        /// </summary>
        public BigInteger SpendingTransactionIdentifier { get; set; }

        /// <summary>
        /// The index where this appears in
        /// the spending transaction's inputs.
        /// </summary>
        public uint SpendingTransactionInputIndex { get; set; }

        /// <summary>
        /// The serialized script that proves that the value from this source
        /// is authorized to be spent.
        /// <c>null</c> if this value source is still spendable.
        /// </summary>
        /// <remarks>
        /// For the coinbase found in blocks, this is usually just a data push.
        /// </remarks>
        public byte[] ScriptSignature { get; set; }

        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as IValueSpender);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IValueSpender other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.SpendingTransactionIdentifier == other.SpendingTransactionIdentifier &&
                   this.SpendingTransactionInputIndex == other.SpendingTransactionInputIndex &&
                   this.SpentTransactionIdentifier == other.SpentTransactionIdentifier &&
                   this.SpentTransactionOutputIndex == other.SpentTransactionOutputIndex &&
                   this.ScriptSignature.SequenceEqual(other.ScriptSignature) &&
                   this.SequenceNumber == other.SequenceNumber;
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
                .HashWith(this.SpentTransactionIdentifier)
                .HashWith(this.SpentTransactionOutputIndex)
                .HashWith(this.SpendingTransactionIdentifier)
                .HashWith(this.SpendingTransactionInputIndex)
                .HashWith(this.SequenceNumber)
                .HashWithEnumerable(this.ScriptSignature);

            return builder;
        }
    }
}
