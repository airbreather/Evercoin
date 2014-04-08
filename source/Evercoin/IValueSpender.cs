using System;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// Represents the spending of an <see cref="IValueSource"/>.
    /// </summary>
    public interface IValueSpender : IEquatable<IValueSpender>
    {
        FancyByteArray SpentTransactionIdentifier { get; }

        uint SpentTransactionOutputIndex { get; }

        /// <summary>
        /// Gets the identifier of the <see cref="ITransaction"/> that
        /// contains this spender in its inputs.
        /// </summary>
        FancyByteArray SpendingTransactionIdentifier { get; }

        /// <summary>
        /// Gets the index where this appears in
        /// the spending transaction's inputs.
        /// </summary>
        uint SpendingTransactionInputIndex { get; }

        /// <summary>
        /// Gets the serialized script that proves that the value from this
        /// source is authorized to be spent.
        /// <c>null</c> if this value source is still spendable.
        /// </summary>
        /// <remarks>
        /// For the coinbase found in blocks, this is usually just a data push.
        /// </remarks>
        FancyByteArray ScriptSignature { get; }

        /// <summary>
        /// Gets the "version" of this spender.
        /// </summary>
        /// <remarks>
        /// When this is <see cref="UInt32.MaxValue"/>, that means that this
        /// source is "final" and may not be replaced by a new spender at a
        /// later point in time.
        /// </remarks>
        uint SequenceNumber { get; }
    }
}
