using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [Serializable]
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

        private TransactionValueSource(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.OriginatingTransactionIdentifier = info.GetValue<BigInteger>(SerializationName_OriginatingTransactionIdentifier);
            this.OriginatingTransactionOutputIndex = info.GetUInt32(SerializationName_OriginatingTransactionOutputIndex);
            this.ScriptPublicKey = info.GetValue<byte[]>(SerializationName_ScriptPublicKey).ToImmutableList();
        }

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

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        public BigInteger OriginatingTransactionIdentifier { get; set; }

        public uint OriginatingTransactionOutputIndex { get; set; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        public ImmutableList<byte> ScriptPublicKey { get; set; }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(SerializationName_OriginatingTransactionIdentifier, this.OriginatingTransactionIdentifier);
            info.AddValue(SerializationName_OriginatingTransactionOutputIndex, this.OriginatingTransactionOutputIndex);
            info.AddValue(SerializationName_ScriptPublicKey, this.ScriptPublicKey.ToArray());
        }
    }
}
