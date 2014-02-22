﻿using System;
using System.Collections.Immutable;
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
        /// Gets a string that identifies this block.
        /// </summary>
        string Identifier { get; }

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
        IValueSource Coinbase { get; }

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
        /// Gets the identifier of the previous block in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="Height"/> equals 0, the return value is undefined.
        /// </remarks>
        string PreviousBlockIdentifier { get; }
    }
}
