using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Util
{
    public static class Cheating
    {
        private static int blockCount = 0;
        private static int txCount = 0;
        private static int highestBlock = 0;

        private static readonly object syncLock = new object();
        private static readonly Dictionary<BigInteger, int> IdToHeight = new Dictionary<BigInteger, int>();
        private static readonly Waiter<BigInteger> BlockWaiter = new Waiter<BigInteger>();

        private static BigInteger[] BlockIdentifiers = new BigInteger[0];
        private static BigInteger[] TransactionIdentifiers = new BigInteger[0];

        public static void AddBlock(int height, BigInteger blockIdentifier)
        {
            lock (syncLock)
            {
                if (BlockIdentifiers.Length < height + 1)
                {
                    Array.Resize(ref BlockIdentifiers, height + 600000);
                }

                highestBlock = Math.Max(highestBlock, height);

                BlockIdentifiers[height] = blockIdentifier;
                IdToHeight[blockIdentifier] = height;

                BlockWaiter.SetEventFor(blockIdentifier);

                blockCount++;
            }
        }

        public static void AddTransaction(BigInteger transactionIdentifier)
        {
            lock (syncLock)
            {
                txCount++;

                if (TransactionIdentifiers.Length < txCount + 1)
                {
                    Array.Resize(ref TransactionIdentifiers, txCount + 600000);
                }

                TransactionIdentifiers[txCount] = transactionIdentifier;
            }
        }

        public static IReadOnlyList<BigInteger> GetBlockIdentifiers()
        {
            lock (syncLock)
            {
                return new ArraySegment<BigInteger>(BlockIdentifiers, 0, blockCount);
            }
        }

        public static int GetBlockIdentifierCount()
        {
            return blockCount;
        }

        public static int GetHighestBlock()
        {
            return highestBlock;
        }

        public static int GetTransactionIdentifierCount()
        {
            return txCount;
        }

        public static async Task<int> GetBlockHeightAsync(BigInteger blockIdentifier, CancellationToken token)
        {
            await Task.Run(() => BlockWaiter.WaitFor(blockIdentifier, token), token);

            lock (syncLock)
            {
                return IdToHeight[blockIdentifier];
            }
        }

        public static void DisposeThings()
        {
            BlockWaiter.Dispose();
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

                IEnumerable<byte> dataToHash = firstNode.Data.Concat(secondNode.Data);
                this.Data = hashAlgorithm.CalculateHash(dataToHash);
            }
        }
    }
}
