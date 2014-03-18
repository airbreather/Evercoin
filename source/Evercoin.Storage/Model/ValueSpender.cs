using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "ValueSpender", Namespace = "Evercoin.Storage.Model")]
    internal sealed class ValueSpender : IValueSpender
    {
        private const string SerializationName_SpendingTransactionIdentifier = "SpendingTransactionIdentifier";
        private const string SerializationName_SpendingTransactionInputIndex = "SpendingTransactionInputIndex";
        private const string SerializationName_ScriptSignature = "ScriptSignature";
        private const string SerializationName_SequenceNumber = "SequenceNumber";
        private const string SerializationName_SpendingValueSource = "SpendingValueSource";

        private Lazy<Collection<byte>> scriptSignature = new Lazy<Collection<byte>>();

        public ValueSpender()
        {
        }

        public ValueSpender(IValueSpender copyFrom)
        {
            this.SpendingTransactionIdentifier = copyFrom.SpendingTransactionIdentifier;
            this.SpendingTransactionInputIndex = copyFrom.SpendingTransactionInputIndex;
            this.SequenceNumber = copyFrom.SequenceNumber;
            this.ScriptSignatureCollection.AddRange(copyFrom.ScriptSignature);
            this.TypedSpendingValueSource = new ValueSource(copyFrom.SpendingValueSource);
        }

        /// <summary>
        /// The <see cref="IValueSource"/> being spent by this spender.
        /// </summary>
        [DataMember(Name = SerializationName_SpendingValueSource)]
        public ValueSource TypedSpendingValueSource { get; set; }

        public IValueSource SpendingValueSource { get { return this.TypedSpendingValueSource; } }

        /// <summary>
        /// The identifier of the <see cref="ITransaction"/> that
        /// contains this spender in its inputs.
        /// </summary>
        [DataMember(Name = SerializationName_SpendingTransactionIdentifier)]
        public BigInteger SpendingTransactionIdentifier { get; set; }

        /// <summary>
        /// The index where this appears in
        /// the spending transaction's inputs.
        /// </summary>
        [DataMember(Name = SerializationName_SpendingTransactionInputIndex)]
        public uint SpendingTransactionInputIndex { get; set; }

        /// <summary>
        /// The serialized script that proves that the value from this source
        /// is authorized to be spent.
        /// <c>null</c> if this value source is still spendable.
        /// </summary>
        /// <remarks>
        /// For the coinbase found in blocks, this is usually just a data push.
        /// </remarks>
        public byte[] ScriptSignature { get { return this.ScriptSignatureCollection.GetArray(); } }

        [DataMember(Name = SerializationName_ScriptSignature)]
        public Collection<byte> ScriptSignatureCollection { get { return this.scriptSignature.Value; } }

        [DataMember(Name = SerializationName_SequenceNumber)]
        public uint SequenceNumber { get; set; }

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
                   this.ScriptSignatureCollection.SequenceEqual(other.ScriptSignature) &&
                   this.SequenceNumber == other.SequenceNumber;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            this.scriptSignature = new Lazy<Collection<byte>>();
        }
    }
}