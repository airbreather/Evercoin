using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Evercoin
{
    /// <summary>
    /// Contains useful extension methods.
    /// </summary>
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
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Gets a 256-bit byte array for this <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="bigInteger">
        /// The value to get data for.
        /// </param>
        /// <returns>
        /// A little-endian 256-bit (32-byte) array that contains the data
        /// for this value.  Differs from <see cref="BigInteger.ToByteArray"/>
        /// in that this is guaranteed to be 32 bytes long.
        /// </returns>
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

        /// <summary>
        /// Determines whether two sequences are equivalent.
        /// Equivalent sequences contain the same elements, but not necessarily
        /// in the same order.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements of the input sequences.
        /// </typeparam>
        /// <param name="firstEnumerable">
        /// An <see cref="IEnumerable{T}"/> to compare to the second sequence.
        /// </param>
        /// <param name="secondEnumerable">
        /// An <see cref="IEnumerable{T}"/> to compare to the first sequence.
        /// </param>
        /// <returns>
        /// A value indicating whether the two sequences contain the same
        /// elements in the same quantities, ignoring the ordering.
        /// </returns>
        /// <remarks>
        /// Inspired by http://stackoverflow.com/a/4576854/1083771.
        /// </remarks>
        public static bool SequenceEquivalent<T>(this IEnumerable<T> firstEnumerable, IEnumerable<T> secondEnumerable)
        {
            return SequenceEquivalent(firstEnumerable, secondEnumerable, null);
        }

        /// <summary>
        /// Determines whether two sequences are equivalent.
        /// Equivalent sequences contain the same elements, but not necessarily
        /// in the same order.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements of the input sequences.
        /// </typeparam>
        /// <param name="firstEnumerable">
        /// An <see cref="IEnumerable{T}"/> to compare to the second sequence.
        /// </param>
        /// <param name="secondEnumerable">
        /// An <see cref="IEnumerable{T}"/> to compare to the first sequence.
        /// </param>
        /// <param name="comparer">
        /// An <see cref="IEqualityComparer{T}"/> to use to compare elements.
        /// </param>
        /// <returns>
        /// A value indicating whether the two sequences contain the same
        /// elements in the same quantities, ignoring the ordering.
        /// </returns>
        /// <remarks>
        /// Inspired by http://stackoverflow.com/a/4576854/1083771.
        /// </remarks>
        public static bool SequenceEquivalent<T>(this IEnumerable<T> firstEnumerable, IEnumerable<T> secondEnumerable, IEqualityComparer<T> comparer)
        {
            if (firstEnumerable == null)
            {
                throw new ArgumentNullException("firstEnumerable");
            }

            if (secondEnumerable == null)
            {
                throw new ArgumentNullException("secondEnumerable");
            }

            ILookup<T, T> firstLookup = firstEnumerable.ToLookup(x => x, comparer);
            ILookup<T, T> secondLookup = secondEnumerable.ToLookup(x => x, comparer);
            return firstLookup.Count == secondLookup.Count &&
                   firstLookup.All(x => x.Count() == secondLookup[x.Key].Count());
        }

        /// <summary>
        /// Gets the values of the given sequence as an array,
        /// optimized for cases where the input is already an array.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements.
        /// </typeparam>
        /// <param name="source">
        /// The sequence to capture as an array.
        /// </param>
        /// <returns>
        /// The elements of <paramref name="source"/> in an array.
        /// </returns>
        public static T[] GetArray<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return source as T[] ?? source.ToArray();
        }

        /// <summary>
        /// Gets a subset of the input array.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the array.
        /// </typeparam>
        /// <param name="source">
        /// The array to get a subset of.
        /// </param>
        /// <param name="offset">
        /// The zero-based offset into <paramref name="source"/> to start.
        /// </param>
        /// <param name="count">
        /// The number of elements in <paramref name="source"/> to take.
        /// </param>
        /// <returns>
        /// An <see cref="ArraySegment{T}"/> that acts as a subset
        /// of <paramref name="source"/>.
        /// </returns>
        public static ArraySegment<T> GetRange<T>(this T[] source, int offset, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", count, "count can't be negative");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "offset can't be negative");
            }

            if (offset + count > source.Length)
            {
                throw new ArgumentException("the subset of <source> beginning at <offset> contains fewer than <count> elements.", "count");
            }

            return new ArraySegment<T>(source, offset, count);
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements.
        /// </typeparam>
        /// <param name="source">
        /// An <see cref="IEnumerable{T}"/> to filter.
        /// </param>
        /// <param name="predicate">
        /// A function to test each element for a condition.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains elements from
        /// <paramref name="source"/> that do not satisfy
        /// <paramref name="predicate"/>.
        /// </returns>
        public static IEnumerable<T> ExceptWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            return source.Where(x => !predicate(x));
        }

        /// <summary>
        /// Adds a sequence of elements to the end of the collection.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the collection.
        /// </typeparam>
        /// <param name="collection">
        /// An <see cref="ICollection{T}"/> to add elements to.
        /// </param>
        /// <param name="values">
        /// The elements to add to the collection.
        /// </param>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> values)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            List<T> list = collection as List<T>;
            if (list != null)
            {
                list.AddRange(values);
                return;
            }

            foreach (T value in values)
            {
                collection.Add(value);
            }
        }
    }
}
