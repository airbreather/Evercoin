using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [Serializable]
    internal sealed class ValueSpender : IValueSpender, ISerializable
    {
        private const string SerializationName_SpendingTransactionIdentifier = "SpendingTransactionIdentifier";
        private const string SerializationName_SpendingTransactionInputIndex = "SpendingTransactionInputIndex";
        private const string SerializationName_ScriptSignature = "ScriptSignature";

        public ValueSpender()
        {
        }

        public ValueSpender(IValueSpender copyFrom)
        {
            this.SpendingTransactionIdentifier = copyFrom.SpendingTransactionIdentifier;
            this.SpendingTransactionInputIndex = copyFrom.SpendingTransactionInputIndex;
            this.ScriptSignature = copyFrom.ScriptSignature;
        }

        private ValueSpender(SerializationInfo info, StreamingContext context)
        {
            this.SpendingTransactionIdentifier = info.GetValue<BigInteger>(SerializationName_SpendingTransactionIdentifier);
            this.SpendingTransactionInputIndex = info.GetUInt32(SerializationName_SpendingTransactionInputIndex);
            this.ScriptSignature = info.GetValue<List<byte>>(SerializationName_ScriptSignature).ToImmutableList();
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
                   Equals(this.SpendingValueSource, other.SpendingValueSource) &&
                   this.ScriptSignature.SequenceEqual(other.ScriptSignature);
        }

        /// <summary>
        /// The <see cref="IValueSource"/> being spent by this spender.
        /// </summary>
        public ValueSource TypedSpendingValueSource { get; set; }

        public IValueSource SpendingValueSource { get { return this.TypedSpendingValueSource; } }

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
        public ImmutableList<byte> ScriptSignature { get; set; }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SerializationName_SpendingTransactionIdentifier, this.SpendingTransactionIdentifier);
            info.AddValue(SerializationName_SpendingTransactionInputIndex, this.SpendingTransactionInputIndex);
            info.AddValue(SerializationName_ScriptSignature, this.ScriptSignature.ToList());
        }
    }
}