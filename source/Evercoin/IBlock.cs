using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodaTime;

namespace Evercoin
{
    /// <summary>
    /// A block of transactions.
    /// </summary>
    public interface IBlock
    {
        /// <summary>
        /// Gets the ordered list of <see cref="ITransaction"/> objects
        /// contained within this block.
        /// </summary>
        IReadOnlyList<ITransaction> Transactions { get; }

        /// <summary>
        /// Gets the <see cref="BlockVersion"/> of this block.
        /// </summary>
        BlockVersion Version { get; }

        /// <summary>
        /// Gets the <see cref="Instant"/> in time when this block was created.
        /// </summary>
        Instant Timestamp { get; }

        /// <summary>
        /// Gets the nonce for this block.
        /// </summary>
        uint Nonce { get; }

        /// <summary>
        /// Gets the <see cref="IValueSource"/> that represents the reward
        /// for mining this block.
        /// </summary>
        ICoinbaseValueSource Coinbase { get; }
    }
}
