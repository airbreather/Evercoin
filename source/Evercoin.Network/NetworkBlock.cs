using System.Linq;
using System.Numerics;

using NodaTime;

namespace Evercoin.Network
{
    internal sealed class NetworkBlock : IBlock
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

            NetworkBlock otherNetworkBlock = other as NetworkBlock;
            if (otherNetworkBlock != null)
            {
                // Short-circuit because we're the only ones who create these!
                return this.Identifier == other.Identifier;
            }

            return other != null &&
                   this.Height == other.Height &&
                   this.Identifier == other.Identifier &&
                   Equals(this.Coinbase, other.Coinbase) &&
                   this.Timestamp == other.Timestamp &&
                   this.Nonce == other.Nonce &&
                   this.PreviousBlockIdentifier == other.PreviousBlockIdentifier &&
                   this.Version == other.Version &&
                   this.DifficultyTarget == other.DifficultyTarget &&
                   this.TransactionIdentifiers.Data.SequenceEqual(other.TransactionIdentifiers.Data);
        }

        /// <summary>
        /// Gets an integer that identifies this block.
        /// </summary>
        public BigInteger Identifier { get; set; }

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
        /// Gets how high this block is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many nodes come before this one.
        /// So, the genesis block is at height zero.
        /// </remarks>
        public ulong Height { get; set; }

        /// <summary>
        /// Gets the identifier of the previous block in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="IBlock.Height"/> equals 0, the return value is undefined.
        /// </remarks>
        public BigInteger PreviousBlockIdentifier { get; set; }
    }
}
