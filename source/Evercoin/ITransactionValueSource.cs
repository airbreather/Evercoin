using System;
using System.Collections.Immutable;

namespace Evercoin
{
    /// <summary>
    /// Represents an <see cref="IValueSource"/> from a transaction.
    /// </summary>
    public interface ITransactionValueSource : IValueSource, IEquatable<ITransactionValueSource>
    {
        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        ITransaction OriginatingTransaction { get; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        ImmutableList<byte> ScriptPublicKey { get; }
    }
}
