using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Evercoin.Network
{
    internal sealed class NetworkTransaction : ITransaction
    {
        private readonly List<NetworkValueSource> inputs = new List<NetworkValueSource>();

        private readonly List<NetworkTransactionValueSource> outputs = new List<NetworkTransactionValueSource>();

        public NetworkTransaction()
        {
        }

        public NetworkTransaction(ITransaction transaction)
        {
            this.InitFrom(transaction);
        }

        public string Identifier { get; set; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        public IList<NetworkValueSource> Inputs { get { return this.inputs; } }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public IList<NetworkTransactionValueSource> Outputs { get { return this.outputs; } }

        IImmutableList<IValueSource> ITransaction.Inputs { get { return ImmutableList.CreateRange<IValueSource>(this.inputs); } }

        IImmutableList<ITransactionValueSource> ITransaction.Outputs { get { return ImmutableList.CreateRange<ITransactionValueSource>(this.outputs); } }

        private static NetworkValueSource CreateSerializableValueSource(IValueSource valueSource)
        {
            ITransactionValueSource transactionValueSource = valueSource as ITransactionValueSource;
            if (transactionValueSource != null)
            {
                return CreateSerializableTransactionValueSource(transactionValueSource);
            }

            NetworkValueSource networkValueSource = valueSource as NetworkValueSource;
            return networkValueSource ?? new NetworkValueSource(valueSource);
        }

        private static NetworkTransactionValueSource CreateSerializableTransactionValueSource(ITransactionValueSource transactionValueSource)
        {
            NetworkTransactionValueSource networkTransactionValueSource = transactionValueSource as NetworkTransactionValueSource;
            return networkTransactionValueSource ?? new NetworkTransactionValueSource(transactionValueSource);
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

            NetworkTransaction other = transaction as NetworkTransaction;
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