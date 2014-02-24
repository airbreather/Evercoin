﻿using System;
using System.Linq;

using Evercoin.Util;

using ProtoBuf;

namespace Evercoin.Storage
{
    [ProtoContract]
    internal class SerializableTransactionValueSource : SerializableValueSource, ITransactionValueSource
    {
        private Lazy<SerializableTransaction> lazyTransaction;

        public SerializableTransactionValueSource()
        {
            this.lazyTransaction = new Lazy<SerializableTransaction>(() =>
            {
                SerializableTransaction hydratedTransaction = (SerializableTransaction)this.ChainStore.GetTransaction(this.TransactionIdentifier);
                foreach (SerializableTransactionValueSource vs in hydratedTransaction.Inputs.OfType<SerializableTransactionValueSource>())
                {
                    vs.ChainStore = this.ChainStore;
                }

                return hydratedTransaction;
            });
        }

        public SerializableTransactionValueSource(ITransactionValueSource valueSource)
            : base(valueSource)
        {
            this.InitFrom(valueSource);
        }

        public IReadOnlyChainStore ChainStore { get; set; }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        [ProtoMember(1)]
        public string TransactionIdentifier { get; set; }

        public SerializableTransaction Transaction
        {
            get { return this.lazyTransaction.Value; }
            set
            {
                this.lazyTransaction = new Lazy<SerializableTransaction>(() => value);
                this.TransactionIdentifier = value.Identifier;
            }
        }

        ITransaction ITransactionValueSource.Transaction { get { return this.lazyTransaction.Value; } }

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
                   Equals(((ITransactionValueSource)this).Transaction, other.Transaction);
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
                .HashWith(((ITransactionValueSource)this).Transaction);
        }

        private void InitFrom(ITransactionValueSource transactionValueSource)
        {
            SerializableTransaction serializableTransaction = transactionValueSource.Transaction as SerializableTransaction;
            this.Transaction = serializableTransaction ?? new SerializableTransaction(transactionValueSource.Transaction);
        }
    }
}