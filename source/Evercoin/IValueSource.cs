using System;
using System.Collections.Immutable;

namespace Evercoin
{
    /// <summary>
    /// Represents a source of value that is available for spending.
    /// </summary>
    public interface IValueSource : IEquatable<IValueSource>
    {
        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        decimal AvailableValue { get; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        IImmutableList<byte> ScriptPubKey { get; }
    }
}
