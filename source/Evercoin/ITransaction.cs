using System;

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
        /// Gets the version of this transaction.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        IValueSpender[] Inputs { get; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        ITransactionValueSource[] Outputs { get; }

        uint LockTime { get; }
    }
}
