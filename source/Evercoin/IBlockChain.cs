using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// A chain of <see cref="IBlock"/> objects.
    /// </summary>
    public interface IBlockChain
    {
        /// <summary>
        /// Gets the block at the head of this chain.
        /// </summary>
        IBlock Head { get; }

        /// <summary>
        /// Gets the number of blocks in this chain.
        /// </summary>
        long Length { get; }
    }
}
