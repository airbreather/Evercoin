using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "Transaction", Namespace = "Evercoin.Storage.Model")]
    internal sealed class Transaction : ITransaction
    {
        private const string SerializationName_Identifier = "Identifier";
        private const string SerializationName_Version = "Version";
        private const string SerializationName_Inputs = "Inputs";
        private const string SerializationName_Outputs = "Outputs";
        private const string SerializationName_LockTime = "LockTime";

        private Lazy<Collection<ValueSpender>> typedInputs = new Lazy<Collection<ValueSpender>>();
        private Lazy<Collection<TransactionValueSource>> typedOutputs = new Lazy<Collection<TransactionValueSource>>();

        public Transaction()
        {
        }

        public Transaction(BigInteger identifier, ITransaction copyFrom)
            : this()
        {
            this.Identifier = identifier;
            this.Version = copyFrom.Version;
            this.LockTime = copyFrom.LockTime;
            this.TypedInputs.AddRange(copyFrom.Inputs.Select(x => new ValueSpender(x)));
            this.TypedOutputs.AddRange(copyFrom.Outputs.Select(x => new TransactionValueSource(x)));
        }

        /// <summary>
        /// Gets a string that identifies this transaction.
        /// </summary>
        [DataMember(Name = SerializationName_Identifier)]
        public BigInteger Identifier { get; set; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        [DataMember(Name = SerializationName_Version)]
        public uint Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        [DataMember(Name = SerializationName_Inputs)]
        public Collection<ValueSpender> TypedInputs { get { return this.typedInputs.Value; } }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        [DataMember(Name = SerializationName_Outputs)]
        public Collection<TransactionValueSource> TypedOutputs { get { return this.typedOutputs.Value; } }

        public IValueSpender[] Inputs { get { return this.typedInputs.Value.GetArray<IValueSpender>(); } }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public ITransactionValueSource[] Outputs { get { return this.typedOutputs.Value.GetArray<ITransactionValueSource>(); } }

        [DataMember(Name = SerializationName_LockTime)]
        public uint LockTime { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(ITransaction other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.typedInputs.Value.SequenceEqual(other.Inputs) &&
                   this.typedOutputs.Value.SequenceEqual(other.Outputs) &&
                   this.Version == other.Version &&
                   this.LockTime == other.LockTime;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            this.typedInputs = new Lazy<Collection<ValueSpender>>();
            this.typedOutputs = new Lazy<Collection<TransactionValueSource>>();
        }
    }
}
