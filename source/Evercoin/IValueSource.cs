using System;

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
    }
}
