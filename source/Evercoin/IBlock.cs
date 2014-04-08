using System;
using System.Numerics;

using NodaTime;

namespace Evercoin
{
    /// <summary>
    /// A block of transactions.
    /// </summary>
    /// <remarks>
    /// Implementations should be immutable; instances are likely to be shared.
    /// </remarks>
    public interface IBlock : IEquatable<IBlock>
    {
        /// <summary>
        /// Gets the ordered list of the identifiers of
        /// <see cref="ITransaction"/> objects contained within this block.
        /// </summary>
        IMerkleTreeNode TransactionIdentifiers { get; }

        /// <summary>
        /// Gets the version of this block.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Gets the <see cref="Instant"/> in time when this block was created.
        /// </summary>
        Instant Timestamp { get; }

        /// <summary>
        /// Gets the nonce for this block.
        /// </summary>
        uint Nonce { get; }

        /// <summary>
        /// Gets the difficulty target being used for this block.
        /// </summary>
        BigInteger DifficultyTarget { get; }

        /// <summary>
        /// Gets the identifier of the previous block in the chain.
        /// </summary>
        FancyByteArray PreviousBlockIdentifier { get; }
    }
}
