using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Evercoin
{
    public static class Extensions
    {
        /// <summary>
        /// Converts a byte array to or from the endianness used by
        /// <see cref="BitConverter"/>, from or to little-endian.
        /// </summary>
        /// <param name="bytes">
        /// The byte array to convert.
        /// </param>
        /// <returns>
        /// The converted array (either itself, or reversed).
        /// </returns>
        /// <remarks>
        /// Modifies the array in-place, as well as returning it.
        /// </remarks>
        public static byte[] LittleEndianToOrFromBitConverterEndianness(this byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public static byte[] ToLittleEndianUInt256Array(this BigInteger bigInteger)
        {
            byte[] unpaddedResult = bigInteger.ToByteArray();

            if (unpaddedResult.Length > 32)
            {
                throw new InvalidOperationException("Number cannot fit into a 256-bit integer.");
            }

            // Initialize the array with ones if it's negative, zeroes if it's positive.
            byte b = bigInteger.Sign < 0 ?
                     (byte)0xff :
                     (byte)0x00;

            byte[] result =
            {
                b, b, b, b, b, b, b, b,
                b, b, b, b, b, b, b, b,
                b, b, b, b, b, b, b, b,
                b, b, b, b, b, b, b, b
            };

            Buffer.BlockCopy(unpaddedResult, 0, result, 0, unpaddedResult.Length);
            return result;
        }

        /// <summary>
        /// Determines whether this Merkle tree is valid, using a given
        /// <see cref="IHashAlgorithm"/> if needed to calculate child hashes.
        /// </summary>
        /// <param name="node">
        /// This node to validate.
        /// </param>
        /// <param name="hashAlgorithm">
        /// The <see cref="IHashAlgorithm"/> to use to calculate child hashes.
        /// </param>
        /// <returns>
        /// A value indicating whether this Merkle tree is valid.
        /// </returns>
        /// <remarks>
        /// Rules:
        /// 1. Each non-null child must also be valid by the given algorithm.
        /// 2. If left child is null, then right child must be null.
        /// 3. If left child is null, and previous checks all pass, then this
        ///    tree is valid.
        /// 4. Otherwise, let A denote the left child's data.  If right child
        ///    is non-null, then let B denote the right child's data.
        ///    Otherwise, let B denote the same value as A.  Then this tree is
        ///    valid if the given hash algorithm returns a result equal to this
        ///    tree's data when hashing A + B, where "+" denotes concatenation.
        /// 5. Otherwise, this tree is invalid.
        /// </remarks>
        public static bool IsValid(this IMerkleTreeNode node, IHashAlgorithm hashAlgorithm)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException("hashAlgorithm");
            }

            if (node.LeftChild == null)
            {
                return node.RightChild == null;
            }

            if (!node.LeftChild.IsValid(hashAlgorithm))
            {
                return false;
            }

            if (node.RightChild != null &&
                !node.RightChild.IsValid(hashAlgorithm))
            {
                return false;
            }

            IMerkleTreeNode firstNode = node.LeftChild;
            IMerkleTreeNode secondNode = node.RightChild ?? node.LeftChild;
            IEnumerable<byte> dataToHash = firstNode.Data.Concat(secondNode.Data);

            byte[] hashResult = hashAlgorithm.CalculateHash(dataToHash);
            return hashResult.SequenceEqual(node.Data);
        }

        public static bool SequenceEquivalent<T>(this IEnumerable<T> firstEnumerable, IEnumerable<T> secondEnumerable)
        {
            return SequenceEquivalent(firstEnumerable, secondEnumerable, null);
        }

        // http://stackoverflow.com/a/4576854/1083771
        public static bool SequenceEquivalent<T>(this IEnumerable<T> firstEnumerable, IEnumerable<T> secondEnumerable, IEqualityComparer<T> comparer)
        {
            ILookup<T, T> firstLookup = firstEnumerable.ToLookup(x => x, comparer);
            ILookup<T, T> secondLookup = secondEnumerable.ToLookup(x => x, comparer);
            return firstLookup.Count == secondLookup.Count &&
                   firstLookup.All(x => x.Count() == secondLookup[x.Key].Count());
        }

        public static T[] GetArray<T>(this IEnumerable<T> source)
        {
            return source as T[] ?? source.ToArray();
        }

        public static ArraySegment<T> GetRange<T>(this T[] source, int offset, int count)
        {
            return new ArraySegment<T>(source, offset, count);
        }

        public static IEnumerable<T> ExceptWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return source.Where(x => !predicate(x));
        }
    }
}
