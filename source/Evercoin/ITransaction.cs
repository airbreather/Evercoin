using System;
using System.Collections.Immutable;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// Represents a transfer of value.
    /// </summary>
    /// <remarks>
    /// Implementations should be immutable; instances are likely to be shared.
    /// </remarks>
    public interface ITransaction : IEquatable<ITransaction>
    {
        /// <summary>
        /// Gets a string that identifies this transaction.
        /// </summary>
        BigInteger Identifier { get; }

        /// <summary>
        /// Gets the identifier of the <see cref="IBlock"/> that contains
        /// this transaction, if any.
        /// <see cref="String.Empty"/> if it is not yet included in a block.
        /// </summary>
        BigInteger ContainingBlockIdentifier { get; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        ImmutableList<IValueSpender> Inputs { get; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        ImmutableList<ITransactionValueSource> Outputs { get; }
    }
}
