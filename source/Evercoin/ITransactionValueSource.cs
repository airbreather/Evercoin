using System;

namespace Evercoin
{
    /// <summary>
    /// Represents an <see cref="IValueSource"/> whose value can only be spent
    /// by running a script.
    /// </summary>
    public interface ITransactionValueSource : IValueSource, IEquatable<ITransactionValueSource>
    {
        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        ITransaction Transaction { get; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        byte[] ScriptData { get; }
    }
}
