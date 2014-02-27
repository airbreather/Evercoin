using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Evercoin.Util;

namespace Evercoin.Storage
{
    public class Transaction : ITransaction
    {
        public Transaction()
        {
            this.Inputs = new List<ValueSource>();
            this.Outputs = new List<ValueSource>();
        }

        public Transaction(ITransaction copyFrom)
            : this()
        {
            this.Identifier = copyFrom.Identifier;
            this.Version = EfValueConverters.EfInt32FromUInt32(copyFrom.Version);
            foreach (IValueSource valueSource in copyFrom.Inputs)
            {
                this.Inputs.Add(new ValueSource(valueSource));
            }

            foreach (ITransactionValueSource valueSource in copyFrom.Outputs)
            {
                this.Outputs.Add(new ValueSource(valueSource) { Transaction = this });
            }
        }
        
        /// <summary>
        /// Gets a string that identifies this transaction.
        /// </summary>
        [Key]
        public string Identifier { get; set; }

        public virtual Block Block { get; set; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        public virtual List<ValueSource> Inputs { get; set; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        public virtual List<ValueSource> Outputs { get; set; }

        uint ITransaction.Version { get { return EfValueConverters.UInt32FromEfInt32(this.Version); } }

        IImmutableList<IValueSource> ITransaction.Inputs { get { return this.Inputs.ToImmutableList<IValueSource>(); } }

        IImmutableList<ITransactionValueSource> ITransaction.Outputs { get { return this.Outputs.ToImmutableList<ITransactionValueSource>(); } }

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
                   this.Version == EfValueConverters.EfInt32FromUInt32(other.Version) &&
                   this.Inputs.SequenceEqual(other.Inputs) &&
                   this.Outputs.SequenceEqual(other.Outputs);
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
            return this.Equals(obj as ITransaction);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWith(this.Version);
            builder = this.Inputs.Aggregate(builder, (current, nextInput) => current.HashWith(nextInput));
            builder = this.Outputs.Aggregate(builder, (current, nextOutput) => current.HashWith(nextOutput));
            return builder;
        }
    }
}