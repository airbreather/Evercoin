using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace Evercoin.Util
{
    public static class Cheating
    {
        private static int txCount = 0;

        public static void AddTransaction(BigInteger transactionIdentifier)
        {
            Interlocked.Increment(ref txCount);
        }

        public static int GetTransactionIdentifierCount()
        {
            return txCount;
        }

        public static IMerkleTreeNode ToMerkleTree(this IEnumerable<IEnumerable<byte>> inputs, IHashAlgorithm hashAlgorithm)
        {
            List<MerkleTreeNode> leaves = inputs.Select(x => new MerkleTreeNode { Data = x.GetArray() }).ToList();
            if (leaves.Count == 0)
            {
                throw new ArgumentException("Must at least one!", "inputs");
            }

            List<MerkleTreeNode> nodesAtThisDepth = leaves.ToList();
            while (nodesAtThisDepth.Count > 1)
            {
                List<MerkleTreeNode> nodesAtPrevDepth = nodesAtThisDepth;
                nodesAtThisDepth = new List<MerkleTreeNode>();

                MerkleTreeNode nextNode = new MerkleTreeNode();
                for (int i = 0; i < nodesAtPrevDepth.Count; i++)
                {
                    MerkleTreeNode prevNode = nodesAtPrevDepth[i];
                    if (i % 2 == 0)
                    {
                        nextNode.LeftChild = prevNode;
                    }
                    else
                    {
                        nextNode.RightChild = prevNode;
                        nodesAtThisDepth.Add(nextNode);
                        nextNode = new MerkleTreeNode();
                    }
                }

                if (nodesAtPrevDepth.Count % 2 == 1)
                {
                    nodesAtThisDepth.Add(nextNode);
                }
            }

            nodesAtThisDepth[0].FixHashes(hashAlgorithm);
            return nodesAtThisDepth[0];
        }

        private sealed class MerkleTreeNode : IMerkleTreeNode
        {
            /// <summary>
            /// Gets the data stored in this node.
            /// </summary>
            public byte[] Data { get; set; }

            /// <summary>
            /// Gets the left subtree.
            /// </summary>
            public MerkleTreeNode LeftChild { get; set; }

            /// <summary>
            /// Gets the right subtree.
            /// </summary>
            public MerkleTreeNode RightChild { get; set; }

            IMerkleTreeNode IMerkleTreeNode.LeftChild { get { return this.LeftChild; } }

            IMerkleTreeNode IMerkleTreeNode.RightChild { get { return this.RightChild; } }

            public void FixHashes(IHashAlgorithm hashAlgorithm)
            {
                if (this.LeftChild != null)
                {
                    this.LeftChild.FixHashes(hashAlgorithm);
                }

                if (this.RightChild != null)
                {
                    this.RightChild.FixHashes(hashAlgorithm);
                }

                // We're cheating, so I don't care about whether to bail out early if this is invalid.
                IMerkleTreeNode firstNode = this.LeftChild ?? this.RightChild;
                IMerkleTreeNode secondNode = this.RightChild ?? this.LeftChild;

                if (firstNode == null || secondNode == null)
                {
                    return;
                }

                byte[] dataToHash = ByteTwiddling.ConcatenateData(firstNode.Data, secondNode.Data);
                this.Data = hashAlgorithm.CalculateHash(dataToHash);
            }
        }
    }
}
