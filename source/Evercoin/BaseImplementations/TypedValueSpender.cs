using System.Linq;
using System.Numerics;

namespace Evercoin.BaseImplementations
{
    internal sealed class TypedValueSpender : IValueSpender
    {
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
                   Equals(this.SpendingValueSource, other.SpendingValueSource) &&
                   this.ScriptSignature.SequenceEqual(other.ScriptSignature) &&
                   this.SequenceNumber == other.SequenceNumber;
        }

        /// <summary>
        /// The <see cref="IValueSource"/> being spent by this spender.
        /// </summary>
        public IValueSource SpendingValueSource { get; set; }

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
    }
}
