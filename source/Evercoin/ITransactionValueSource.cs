using System;

namespace Evercoin
{
    /// <summary>
    /// Represents an <see cref="IValueSource"/> connected to a transaction.
    /// </summary>
    public interface ITransactionValueSource : IValueSource, IEquatable<ITransactionValueSource>
    {
        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        ITransaction Transaction { get; }
    }
}
