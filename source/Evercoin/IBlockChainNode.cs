using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// A node in the block chain.
    /// </summary>
    public interface IBlockChainNode
    {
        /// <summary>
        /// Gets how deep this block is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many blocks come before this one.
        /// So, the genesis block is at depth zero.
        /// </remarks>
        long Depth { get; }

        /// <summary>
        /// Gets the current <see cref="IBlock"/> in the chain.
        /// </summary>
        IBlock CurrentBlock { get; }

        /// <summary>
        /// Gets the previous <see cref="IBlock"/> in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="Depth"/> equals 0, the return value is undefined.
        /// </remarks>
        IBlock PreviousBlock { get; }

        /// <summary>
        /// Gets the next <see cref="IBlock"/> in the chain,
        /// or <c>null</c> if this is the tail of the chain.
        /// </summary>
        IBlock NextBlock { get; }
    }
}
