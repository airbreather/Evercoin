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
        /// Gets the <see cref="ITransaction"/> objects contained within this block.
        /// </summary>
        ICollection<ITransaction> Transactions { get; }

        /// <summary>
        /// The <see cref="IBlock"/> that this block builds upon.
        /// </summary>
        /// <remarks>
        /// <c>null</c> if this is the genesis block.
        /// </remarks>
        IBlock PreviousBlock { get; }

        /// <summary>
        /// The <see cref="Instant"/> in time when this block was created.
        /// </summary>
        Instant Timestamp { get; }
    }
}
