﻿using System.Collections.Immutable;

namespace Evercoin
{
    /// <summary>
    /// Represents a hash tree.
    /// </summary>
    public interface IMerkleTreeNode
    {
        /// <summary>
        /// Gets the data stored in this node.
        /// </summary>
        ImmutableList<byte> Data { get; }

        /// <summary>
        /// Gets the left subtree.
        /// </summary>
        IMerkleTreeNode LeftChild { get; }

        /// <summary>
        /// Gets the right subtree.
        /// </summary>
        IMerkleTreeNode RightChild { get; }
    }
}
