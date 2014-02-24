using Evercoin.Util;

namespace Evercoin.Network
{
    internal class NetworkTransactionValueSource : NetworkValueSource, ITransactionValueSource
    {
        public NetworkTransactionValueSource()
        {
        }

        public NetworkTransactionValueSource(ITransactionValueSource valueSource)
            : base(valueSource)
        {
            this.InitFrom(valueSource);
        }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        public NetworkTransaction Transaction { get; set; }

        ITransaction ITransactionValueSource.Transaction { get { return this.Transaction; } }

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
                   Equals(this.Transaction, other.Transaction);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ITransactionValueSource);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public override bool Equals(IValueSource other)
        {
            return this.Equals(other as ITransactionValueSource);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return new HashCodeBuilder(base.GetHashCode())
                .HashWith(this.Transaction);
        }

        private void InitFrom(ITransactionValueSource transactionValueSource)
        {
            NetworkTransaction serializableTransaction = transactionValueSource.Transaction as NetworkTransaction;
            this.Transaction = serializableTransaction ?? new NetworkTransaction(transactionValueSource.Transaction);
        }
    }
}