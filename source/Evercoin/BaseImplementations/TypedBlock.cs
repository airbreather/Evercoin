using System.Linq;
using System.Numerics;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.BaseImplementations
{
    internal sealed class TypedBlock : IBlock
    {
        /// <summary>
        /// Gets the ordered list of the identifiers of
        /// <see cref="ITransaction"/> objects contained within this block.
        /// </summary>
        public IMerkleTreeNode TransactionIdentifiers { get; set; }

        /// <summary>
        /// Gets the version of this block.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets the <see cref="Instant"/> in time when this block was created.
        /// </summary>
        public Instant Timestamp { get; set; }

        /// <summary>
        /// Gets the nonce for this block.
        /// </summary>
        public uint Nonce { get; set; }

        /// <summary>
        /// Gets the <see cref="ICoinbaseValueSource"/> that represents the
        /// reward for mining this block.
        /// </summary>
        public ICoinbaseValueSource Coinbase { get; set; }

        /// <summary>
        /// Gets the difficulty target being used for this block.
        /// </summary>
        public BigInteger DifficultyTarget { get; set; }

        /// <summary>
        /// Gets the identifier of the previous block in the chain.
        /// </summary>
        public BigInteger PreviousBlockIdentifier { get; set; }

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
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IBlock other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   Equals(this.Coinbase, other.Coinbase) &&
                   this.Timestamp == other.Timestamp &&
                   this.Nonce == other.Nonce &&
                   this.PreviousBlockIdentifier == other.PreviousBlockIdentifier &&
                   this.Version == other.Version &&
                   this.DifficultyTarget == other.DifficultyTarget &&
                   this.TransactionIdentifiers.Data.SequenceEqual(other.TransactionIdentifiers.Data);
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
                .HashWith(this.Coinbase)
                .HashWith(this.Timestamp)
                .HashWith(this.Nonce)
                .HashWith(this.PreviousBlockIdentifier)
                .HashWith(this.Version)
                .HashWith(this.DifficultyTarget);
            builder = this.TransactionIdentifiers.Data.Aggregate(builder, (prevBuilder, nextByte) => prevBuilder.HashWith(nextByte));

            return builder;
        }
    }
}
