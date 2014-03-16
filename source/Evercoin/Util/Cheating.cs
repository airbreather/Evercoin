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
        private static readonly object syncLock = new object();
        private static readonly Dictionary<BigInteger, int> IdToHeight = new Dictionary<BigInteger, int>(300000);
        private static readonly Dictionary<BigInteger, ManualResetEventSlim> IdWaiters = new Dictionary<BigInteger, ManualResetEventSlim>(300000);

        private static BigInteger[] BlockIdentifiers = new BigInteger[0];

        public static void Add(int height, BigInteger blockIdentifier)
        {
            lock (syncLock)
            {
                if (BlockIdentifiers.Length < height + 1)
                {
                    Array.Resize(ref BlockIdentifiers, height + 300000);
                }

                BlockIdentifiers[height] = blockIdentifier;
                IdToHeight[blockIdentifier] = height;

                ManualResetEventSlim mres;
                if (!IdWaiters.TryGetValue(blockIdentifier, out mres))
                {
                    IdWaiters[blockIdentifier] = mres = new ManualResetEventSlim();
                }

                mres.Set();
                blockCount++;
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

        public static async Task<int> GetBlockHeightAsync(BigInteger blockIdentifier, CancellationToken token)
        {
            int height;
            if (IdToHeight.TryGetValue(blockIdentifier, out height))
            {
                return height;
            }

            ManualResetEventSlim mres;
            lock (syncLock)
            {
                if (!IdWaiters.TryGetValue(blockIdentifier, out mres))
                {
                    IdWaiters[blockIdentifier] = mres = new ManualResetEventSlim();
                }
            }

            await Task.Run(() => mres.Wait(token), token);
            lock (syncLock)
            {
                return IdToHeight[blockIdentifier];
            }
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
