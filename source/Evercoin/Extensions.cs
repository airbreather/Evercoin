﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

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
        /// Gets a single-element enumerable that contains just a single value.
        /// </summary>
        /// <param name="value">
        /// The value to stuff into a single-element enumerable.
        /// </param>
        /// <returns>
        /// A single-element enumerable that contains just <paramref name="value"/>.
        /// </returns>
        public static IEnumerable<T> AsSingleElementEnumerable<T>(this T value)
        {
            T[] result = { value };
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
            IEnumerable<byte> dataToHash = ByteTwiddling.ConcatenateData(firstNode.Data, secondNode.Data);

            FancyByteArray hashResult = hashAlgorithm.CalculateHash(dataToHash);
            return hashResult == node.Data;
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
        /// Gets a subset of the input list.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the list.
        /// </typeparam>
        /// <param name="source">
        /// The list to get a subset of.
        /// </param>
        /// <param name="offset">
        /// The zero-based offset into <paramref name="source"/> to start.
        /// </param>
        /// <param name="count">
        /// The number of elements in <paramref name="source"/> to take.
        /// </param>
        /// <returns>
        /// A subset of <paramref name="source"/>.
        /// </returns>
        public static IReadOnlyList<T> GetRange<T>(this IReadOnlyList<T> source, int offset, int count)
        {
            return new ReadOnlySubList<T>(source, offset, count);
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

        public static BigInteger TargetFromBits(uint bits)
        {
            uint mantissa = bits & 0x007fffff;
            bool negative = (bits & 0x00800000) != 0;
            byte exponent = (byte)(bits >> 24);
            BigInteger result;

            if (exponent <= 3)
            {
                mantissa >>= 8 * (3 - exponent);
                result = mantissa;
            }
            else
            {
                result = mantissa;
                result <<= 8 * (exponent - 3);
            }

            if ((result.Sign < 0) != negative)
            {
                result = -result;
            }

            return result;
        }

        public static uint TargetToBits(BigInteger target)
        {
            int size = target.ToByteArray().Length;

            uint result;
            if (size <= 3)
            {
                result = ((uint)target) << (8 * (3 - size));
            }
            else
            {
                result = (uint)(target >> (8 * (size - 3)));
            }

            if (0 != (result & 0x00800000))
            {
                result >>= 8;
                size++;
            }

            result |= (uint)(size << 24);
            result |= (uint)(target.Sign < 0 ? 0x00800000 : 0);
            return result;
        }
    }
}
