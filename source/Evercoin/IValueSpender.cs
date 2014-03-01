using System;
using System.Collections.Immutable;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// Represents the spending of an <see cref="IValueSource"/>.
    /// </summary>
    public interface IValueSpender : IEquatable<IValueSpender>
    {
        /// <summary>
        /// The <see cref="IValueSource"/> being spent by this spender.
        /// </summary>
        IValueSource SpendingValueSource { get; }

        /// <summary>
        /// The identifier of the <see cref="ITransaction"/> that
        /// contains this spender in its inputs.
        /// </summary>
        BigInteger SpendingTransactionIdentifier { get; }

        /// <summary>
        /// The index where this appears in
        /// the spending transaction's inputs.
        /// </summary>
        uint SpendingTransactionInputIndex { get; }

        /// <summary>
        /// The serialized script that proves that the value from this source
        /// is authorized to be spent.
        /// <c>null</c> if this value source is still spendable.
        /// </summary>
        /// <remarks>
        /// For the coinbase found in blocks, this is usually just a data push.
        /// </remarks>
        ImmutableList<byte> ScriptSignature { get; }
    }
}
