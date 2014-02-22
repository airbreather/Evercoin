using System;
using System.Collections.Immutable;

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
        string Identifier { get; }

        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        IImmutableList<IValueSource> Inputs { get; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        IImmutableList<ITransactionValueSource> Outputs { get; }
    }
}
