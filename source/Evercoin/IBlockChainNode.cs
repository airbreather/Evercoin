using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// A node in the block chain.
    /// </summary>
    public interface IBlockChainNode : IEquatable<IBlockChainNode>
    {
        /// <summary>
        /// Gets the difficulty target being used for this node.
        /// </summary>
        BigInteger DifficultyTarget { get; }

        /// <summary>
        /// Gets how high this node is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many nodes come before this one.
        /// So, the genesis block is at height zero.
        /// </remarks>
        ulong Height { get; }

        /// <summary>
        /// Gets how deep this node is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many nodes come after this one.
        /// </remarks>
        ulong Depth { get; }

        /// <summary>
        /// Gets the <see cref="IBlock"/> for this node.
        /// </summary>
        IBlock Block { get; }

        /// <summary>
        /// Gets the previous node in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="Height"/> equals 0, the return value is undefined.
        /// </remarks>
        IBlockChainNode PreviousNode { get; }

        /// <summary>
        /// Gets the next node in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="Depth"/> equals 0, the return value is undefined.
        /// </remarks>
        IBlockChainNode NextNode { get; }
    }
}
