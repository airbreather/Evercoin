using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace Evercoin.Storage.Model
{
    [Serializable]
    internal sealed class Transaction : ITransaction, ISerializable
    {
        private const string SerializationName_Identifier = "Identifier";
        private const string SerializationName_ContainingBlockIdentifier = "ContainingBlockIdentifier";
        private const string SerializationName_Version = "Version";
        private const string SerializationName_Inputs = "Inputs";
        private const string SerializationName_Outputs = "Outputs";
        private const string SerializationName_LockTime = "LockTime";

        public Transaction()
        {
            this.TypedInputs = new Collection<ValueSpender>();
            this.TypedOutputs = new Collection<TransactionValueSource>();
        }

        public Transaction(ITransaction copyFrom)
            : this()
        {
            this.Identifier = copyFrom.Identifier;
            this.ContainingBlockIdentifier = copyFrom.ContainingBlockIdentifier;
            this.Version = copyFrom.Version;
            this.LockTime = copyFrom.LockTime;
            foreach (IValueSpender input in copyFrom.Inputs)
            {
                this.TypedInputs.Add(new ValueSpender(input));
            }

            foreach (ITransactionValueSource output in copyFrom.Outputs)
            {
                this.TypedOutputs.Add(new TransactionValueSource(output));
            }
        }

        private Transaction(SerializationInfo info, StreamingContext context)
        {
            this.Identifier = info.GetValue<BigInteger>(SerializationName_Identifier);
            this.ContainingBlockIdentifier = info.GetValue<BigInteger>(SerializationName_ContainingBlockIdentifier);
            this.Version = info.GetUInt32(SerializationName_Version);
            this.LockTime = info.GetUInt32(SerializationName_LockTime);
            this.TypedInputs = info.GetValue<Collection<ValueSpender>>(SerializationName_Inputs);
            this.TypedOutputs = info.GetValue<Collection<TransactionValueSource>>(SerializationName_Outputs);
        }

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
                   this.Identifier == other.Identifier &&
                   this.TypedInputs.SequenceEqual(other.Inputs) &&
                   this.TypedOutputs.SequenceEqual(other.Outputs) &&
                   this.Version == other.Version &&
                   this.LockTime == other.LockTime;
        }

        /// <summary>
        /// Gets a string that identifies this transaction.
        /// </summary>
        public BigInteger Identifier { get; set; }

        /// <summary>
        /// Gets the identifier of the <see cref="IBlock"/> that contains
        /// this transaction, if any.
        /// <see cref="string.Empty"/> if it is not yet included in a block.
        /// </summary>
        public BigInteger ContainingBlockIdentifier { get; set; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        public Collection<ValueSpender> TypedInputs { get; private set; }

        public IValueSpender[] Inputs { get { return this.TypedInputs.GetArray<IValueSpender>(); } }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public Collection<TransactionValueSource> TypedOutputs { get; private set; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public ITransactionValueSource[] Outputs { get { return this.TypedOutputs.GetArray<ITransactionValueSource>(); } }

        public uint LockTime { get; set; }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SerializationName_Identifier, this.Identifier);
            info.AddValue(SerializationName_ContainingBlockIdentifier, this.ContainingBlockIdentifier);
            info.AddValue(SerializationName_Version, this.Version);
            info.AddValue(SerializationName_Inputs, this.TypedInputs);
            info.AddValue(SerializationName_Outputs, this.TypedOutputs);
            info.AddValue(SerializationName_LockTime, this.LockTime);
        }
    }
}
