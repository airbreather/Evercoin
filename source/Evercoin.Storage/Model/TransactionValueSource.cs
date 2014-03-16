using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "TransactionValueSource", Namespace = "Evercoin.Storage.Model")]
    internal sealed class TransactionValueSource : ValueSource, ITransactionValueSource
    {
        private const string SerializationName_OriginatingTransactionIdentifier = "OriginatingTransactionIdentifier";
        private const string SerializationName_OriginatingTransactionOutputIndex = "OriginatingTransactionOutputIndex";
        private const string SerializationName_ScriptPublicKey = "ScriptPublicKey";

        public TransactionValueSource()
        {
        }

        public TransactionValueSource(ITransactionValueSource copyFrom)
            : base(copyFrom)
        {
            this.ScriptPublicKey = copyFrom.ScriptPublicKey;
            this.OriginatingTransactionIdentifier = copyFrom.OriginatingTransactionIdentifier;
            this.OriginatingTransactionOutputIndex = copyFrom.OriginatingTransactionOutputIndex;
        }

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
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(ITransactionValueSource other)
        {
            return base.Equals(other) &&
                   this.OriginatingTransactionIdentifier == other.OriginatingTransactionIdentifier &&
                   this.ScriptPublicKey.SequenceEqual(other.ScriptPublicKey);
        }
    }
}
