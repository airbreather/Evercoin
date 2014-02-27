using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.Storage
{
    public class Block : IBlock
    {
        public Block()
        {
            this.Transactions = new List<Transaction>();
        }

        public Block(IBlock copyFrom)
            : this()
        {
            this.Identifier = copyFrom.Identifier;
            this.Version = EfValueConverters.EfInt32FromUInt32(copyFrom.Version);
            this.TimestampTicks = copyFrom.Timestamp.Ticks;
            this.Nonce = EfValueConverters.EfInt32FromUInt32(copyFrom.Nonce);
            this.DifficultyTargetBits = EfValueConverters.EfInt64FromUInt64((ulong)copyFrom.DifficultyTarget);
            this.Height = EfValueConverters.EfInt64FromUInt64(copyFrom.Height);
            this.PreviousBlockIdentifier = copyFrom.PreviousBlockIdentifier;
            foreach (ITransaction transaction in copyFrom.Transactions)
            {
                this.Transactions.Add(new Transaction(transaction));
            }

            this.Coinbase = new ValueSource(copyFrom.Coinbase);
        }

        [Key]
        public string Identifier { get; set; }

        public virtual List<Transaction> Transactions { get; set; }

        public int Version { get; set; }

        public long TimestampTicks { get; set; }

        public int Nonce { get; set; }

        public virtual ValueSource Coinbase { get; set; }

        public long DifficultyTargetBits { get; set; }

        public long Height { get; set; }

        public virtual Block PreviousBlock { get; set; }

        public string PreviousBlockIdentifier { get; set; }

        public bool Equals(IBlock other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.Identifier == other.Identifier &&
                   this.Height == EfValueConverters.EfInt64FromUInt64(other.Height) &&
                   this.PreviousBlockIdentifier == other.PreviousBlockIdentifier &&
                   this.Nonce == EfValueConverters.EfInt32FromUInt32(other.Nonce) &&
                   this.Version == EfValueConverters.EfInt32FromUInt32(other.Version) &&
                   this.TimestampTicks == other.Timestamp.Ticks &&
                   this.DifficultyTargetBits == EfValueConverters.EfInt64FromUInt64((ulong)other.DifficultyTarget) &&
                   this.Transactions.SequenceEqual(other.Transactions) &&
                   Equals(this.Coinbase, other.Coinbase);
        }

        ulong IBlock.Height { get { return EfValueConverters.UInt64FromEfInt64(this.Height); } }

        uint IBlock.Nonce { get { return EfValueConverters.UInt32FromEfInt32(this.Nonce); } }

        uint IBlock.Version { get { return EfValueConverters.UInt32FromEfInt32(this.Version); } }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as IBlock);
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
                .HashWith(this.Identifier)
                .HashWith(this.Nonce)
                .HashWith(this.PreviousBlockIdentifier)
                .HashWith(this.Version)
                .HashWith(this.Height)
                .HashWith(this.TimestampTicks)
                .HashWith(this.Coinbase);
            
            builder = this.Transactions.Aggregate(builder, (current, nextTransaction) => current.HashWith(nextTransaction));
            return builder;
        }

        IImmutableList<ITransaction> IBlock.Transactions { get { return this.Transactions.ToImmutableList<ITransaction>(); } }

        IValueSource IBlock.Coinbase { get { return this.Coinbase; } }

        Instant IBlock.Timestamp { get { return Instant.FromTicksSinceUnixEpoch(this.TimestampTicks); } }

        BigInteger IBlock.DifficultyTarget { get { return this.DifficultyTargetBits; } }
    }
}
