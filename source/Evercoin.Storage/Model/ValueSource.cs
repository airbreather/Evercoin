using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "ValueSource", Namespace = "Evercoin.Storage.Model")]
    internal sealed class ValueSource : IValueSource
    {
        private const string SerializationName_AvailableValue = "AvailableValue";
        private const string SerializationName_OriginatingTransactionIdentifier = "OriginatingTransactionIdentifier";
        private const string SerializationName_OriginatingTransactionOutputIndex = "OriginatingTransactionOutputIndex";
        private const string SerializationName_ScriptPublicKey = "ScriptPublicKey";

        public ValueSource()
        {
        }

        public ValueSource(IValueSource copyFrom)
            : this()
        {
            this.AvailableValue = copyFrom.AvailableValue;
            this.ScriptPublicKey = copyFrom.ScriptPublicKey;
            this.OriginatingTransactionIdentifier = copyFrom.OriginatingTransactionIdentifier;
            this.OriginatingTransactionOutputIndex = copyFrom.OriginatingTransactionOutputIndex;
        }

        /// <summary>
        /// Gets a value indicating whether this is the coinbase value source
        /// that gets created as a subsidy for miners.
        /// </summary>
        public bool IsCoinbase { get { return this.OriginatingTransactionIdentifier.IsZero && this.OriginatingTransactionOutputIndex == 0; } }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        [DataMember(Name = SerializationName_OriginatingTransactionIdentifier)]
        public BigInteger OriginatingTransactionIdentifier { get; set; }

        [DataMember(Name = SerializationName_OriginatingTransactionOutputIndex)]
        public uint OriginatingTransactionOutputIndex { get; set; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        [DataMember(Name = SerializationName_ScriptPublicKey)]
        public byte[] ScriptPublicKey { get; set; }

        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        [DataMember(Name = SerializationName_AvailableValue)]
        public decimal AvailableValue { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IValueSource other)
        {
            return base.Equals(other) &&
                   this.AvailableValue == other.AvailableValue &&
                   this.OriginatingTransactionIdentifier == other.OriginatingTransactionIdentifier &&
                   this.ScriptPublicKey.SequenceEqual(other.ScriptPublicKey);
        }
    }
}
