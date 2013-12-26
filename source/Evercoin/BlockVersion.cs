using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// The version of a block.
    /// </summary>
    public enum BlockVersion : uint
    {
        /// <summary>
        /// The initial version used in blocks.
        /// </summary>
        Version1 = 1,

        /// <summary>
        /// The second version number used in blocks, per BIP 0034.
        /// </summary>
        /// <remarks>
        /// The difference from <see cref="Version1"/>: coinbase transactions
        /// on these blocks include the block height.
        /// </remarks>
        Version2 = 2
    }
}
