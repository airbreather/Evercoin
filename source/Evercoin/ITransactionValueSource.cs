using System;
using System.Numerics;

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
        BigInteger OriginatingTransactionIdentifier { get; }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// </summary>
        uint OriginatingTransactionOutputIndex { get; }

        /// <summary>
        /// The serialized script that dictates how the value
        /// from this source can be spent.
        /// </summary>
        byte[] ScriptPublicKey { get; }
    }
}
