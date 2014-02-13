using System;
using System.Collections.Immutable;

namespace Evercoin
{
    /// <summary>
    /// Represents a transfer of value.
    /// </summary>
    public interface ITransaction : IEquatable<ITransaction>
    {
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
