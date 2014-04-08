using System;
using System.Collections.ObjectModel;

namespace Evercoin
{
    /// <summary>
    /// Represents a transfer of value.
    /// </summary>
    public interface ITransaction : IEquatable<ITransaction>
    {
        /// <summary>
        /// Gets the version of this transaction.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Gets the inputs spent by this transaction.
        /// </summary>
        ReadOnlyCollection<IValueSpender> Inputs { get; }

        /// <summary>
        /// Gets the outputs of this transaction.
        /// </summary>
        ReadOnlyCollection<IValueSource> Outputs { get; }

        /// <summary>
        /// Gets a value that represents the time (see remarks)
        /// after which this transaction may be added to a block.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is admittedly a bit confusing, but it's a fundamental part of
        /// the protocol, since it's required for calculating the identifier,
        /// so it's unfortunately something I have to support here.
        /// 0:    always locked (may never be added to a block).
        /// &lt;  500000000: Block number at which this transaction is locked.
        /// %gt;= 500000000: UNIX timestamp at which this transaction is locked.
        /// </para>
        /// <para>
        /// TODO: this could be encapsulated by a slightly intelligent struct,
        /// i.e., something that can say "does this block come before this
        /// locktime?", and check the timestamp or height accordingly.
        /// </para>
        /// </remarks>
        uint LockTime { get; }
    }
}
