using System;
using System.Collections.Immutable;
using System.Numerics;

using NodaTime;

namespace Evercoin
{
    /// <summary>
    /// A block of transactions.
    /// </summary>
    public interface IBlock : IEquatable<IBlock>
    {
        /// <summary>
        /// Gets the ordered list of <see cref="ITransaction"/> objects
        /// contained within this block.
        /// </summary>
        IImmutableList<ITransaction> Transactions { get; }

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
        /// Gets the <see cref="IValueSource"/> that represents the reward
        /// for mining this block.
        /// </summary>
        ICoinbaseValueSource Coinbase { get; }

        /// <summary>
        /// Gets the difficulty target being used for this block.
        /// </summary>
        BigInteger DifficultyTarget { get; }

        /// <summary>
        /// Gets how high this block is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many nodes come before this one.
        /// So, the genesis block is at height zero.
        /// </remarks>
        ulong Height { get; }

        /// <summary>
        /// Gets how deep this block is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many blocks come after this one.
        /// </remarks>
        ulong Depth { get; }

        /// <summary>
        /// Gets the previous block in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="Height"/> equals 0, the return value is undefined.
        /// </remarks>
        IBlock PreviousBlock { get; }

        /// <summary>
        /// Gets the next block in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="Depth"/> equals 0, the return value is undefined.
        /// </remarks>
        IBlock NextBlock { get; }
    }
}
