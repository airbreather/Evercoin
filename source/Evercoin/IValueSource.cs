using System;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// Represents a source of value that is available for spending.
    /// </summary>
    public interface IValueSource : IEquatable<IValueSource>
    {
        /// <summary>
        /// Gets a value indicating whether this is the coinbase value source
        /// that gets created as a subsidy for miners.
        /// </summary>
        bool IsCoinbase { get; }

        /// <summary>
        /// Gets the identifier of the <see cref="ITransaction"/> that contains
        /// this as one of its outputs.
        /// 0 if this is a coinbase.
        /// </summary>
        BigInteger OriginatingTransactionIdentifier { get; }

        /// <summary>
        /// Gets the <see cref="ITransaction"/> that contains this
        /// as one of its outputs.
        /// 0 if this is a coinbase.
        /// </summary>
        uint OriginatingTransactionOutputIndex { get; }

        /// <summary>
        /// Gets the serialized script that dictates how the value
        /// from this source can be spent.
        /// Undefined if this is a coinbase.
        /// </summary>
        byte[] ScriptPublicKey { get; }

        /// <summary>
        /// Gets how much value can be spent by this source.
        /// </summary>
        decimal AvailableValue { get; }
    }
}
