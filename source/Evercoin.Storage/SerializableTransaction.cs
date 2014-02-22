using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ProtoBuf;

namespace Evercoin.Storage
{
    [ProtoContract(AsReferenceDefault = true)]
    internal sealed class SerializableTransaction : ITransaction
    {
        private readonly List<SerializableValueSource> inputs = new List<SerializableValueSource>();

        private readonly List<SerializableTransactionValueSource> outputs = new List<SerializableTransactionValueSource>();

        public SerializableTransaction()
        {
        }

        public SerializableTransaction(ITransaction transaction)
        {
            this.InitFrom(transaction);
        }

        [ProtoMember(1)]
        public string Identifier { get; set; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        [ProtoMember(2)]
        public uint Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        [ProtoMember(3)]
        public IList<SerializableValueSource> Inputs { get { return this.inputs; } }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        [ProtoMember(4)]
        public IList<SerializableTransactionValueSource> Outputs { get { return this.outputs; } }

        IImmutableList<IValueSource> ITransaction.Inputs { get { return ImmutableList.CreateRange<IValueSource>(this.inputs); } }

        IImmutableList<ITransactionValueSource> ITransaction.Outputs { get { return ImmutableList.CreateRange<ITransactionValueSource>(this.outputs); } }

        private static SerializableValueSource CreateSerializableValueSource(IValueSource valueSource)
        {
            ITransactionValueSource transactionValueSource = valueSource as ITransactionValueSource;
            if (transactionValueSource != null)
            {
                return CreateSerializableTransactionValueSource(transactionValueSource);
            }

            SerializableValueSource serializableValueSource = valueSource as SerializableValueSource;
            return serializableValueSource ?? new SerializableValueSource(valueSource);
        }

        private static SerializableTransactionValueSource CreateSerializableTransactionValueSource(ITransactionValueSource transactionValueSource)
        {
            SerializableTransactionValueSource serializableTransactionValueSource = transactionValueSource as SerializableTransactionValueSource;
            return serializableTransactionValueSource ?? new SerializableTransactionValueSource(transactionValueSource);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.
        /// </param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(ITransaction other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.Version == other.Version &&
                   this.Inputs.SequenceEqual(other.Inputs) &&
                   this.Outputs.SequenceEqual(other.Outputs);
        }

        private void InitFrom(ITransaction transaction)
        {
            this.Identifier = transaction.Identifier;
            this.Version = transaction.Version;

            SerializableTransaction other = transaction as SerializableTransaction;
            if (other != null)
            {
                this.inputs.AddRange(other.inputs);
                this.outputs.AddRange(other.outputs);
            }
            else
            {
                this.inputs.AddRange(transaction.Inputs.Select(CreateSerializableValueSource));
                this.outputs.AddRange(transaction.Outputs.Select(CreateSerializableTransactionValueSource));   
            }
        }
    }
}