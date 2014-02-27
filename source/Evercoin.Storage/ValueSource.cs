using System.Collections.Immutable;
using System.Linq;

using Evercoin.Util;

namespace Evercoin.Storage
{
    public class ValueSource : ITransactionValueSource
    {
        public ValueSource()
        {
        }

        public ValueSource(IValueSource copyFrom)
            : this()
        {
            ITransactionValueSource txOther = copyFrom as ITransactionValueSource;
            this.AvailableValue = copyFrom.AvailableValue;
            this.ScriptPubKey = copyFrom.ScriptPubKey.ToArray();
            if (txOther != null)
            {
                ValueSource other = txOther as ValueSource;
                this.Transaction = other != null ? other.Transaction : new Transaction(txOther.Transaction);
            }
            else
            {
                this.Transaction = null;
            }
        }

        public int Id { get; set; }

        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        public decimal AvailableValue { get; set; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        public byte[] ScriptPubKey { get; set; }

        IImmutableList<byte> IValueSource.ScriptPubKey { get { return this.ScriptPubKey.ToImmutableList(); } }


        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IValueSource other)
        {
            if (ReferenceEquals(this, other) ||
                this.Equals(other as ITransactionValueSource))
            {
                return true;
            }

            return other != null &&
                   this.Transaction == null &&
                   this.AvailableValue == other.AvailableValue &&
                   this.ScriptPubKey.SequenceEqual(other.ScriptPubKey);
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
            return this.Equals(obj as IValueSource);
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
                .HashWith(this.AvailableValue)
                .HashWith(this.Transaction);
            builder = this.ScriptPubKey.Aggregate(builder, (current, nextByte) => current.HashWith(nextByte));
            return builder;
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
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.AvailableValue == other.AvailableValue &&
                   this.ScriptPubKey.SequenceEqual(other.ScriptPubKey) &&
                   Equals(this.Transaction, other.Transaction);
        }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        public virtual Transaction Transaction { get; set; }

        ITransaction ITransactionValueSource.Transaction { get { return this.Transaction; } }
    }
}