using System.Linq;
using System.Numerics;

using NodaTime;

namespace Evercoin.BaseImplementations
{
    internal sealed class TypedBlock : IBlock
    {
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
    }
}
