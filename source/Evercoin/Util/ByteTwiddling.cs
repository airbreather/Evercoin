using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Evercoin.Util
{
    public static class ByteTwiddling
    {
        /// <summary>
        /// Converts a list of bytes to a hex string.
        /// </summary>
        /// <param name="bytes">
        /// The bytes to convert.
        /// </param>
        /// <returns>
        /// The hex string representation of <paramref name="bytes"/>.
        /// </returns>
        /// <remarks>
        /// http://stackoverflow.com/a/14333437/1083771.
        /// </remarks>
        public static string ByteArrayToHexString(IReadOnlyList<byte> bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            // From the author:
            // Abandon all hope, you who enter here.
            // So I'm not going to touch it.
            char[] c = new char[bytes.Count * 2];
            int b;
            for (int i = 0; i < bytes.Count; i++)
            {
                b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }

            return new string(c);
        }

        /// <summary>
        /// Converts a hex string to an array of bytes.
        /// </summary>
        /// <param name="hexString">
        /// The hex string to convert.
        /// </param>
        /// <returns>
        /// The bytes represented by <paramref name="hexString"/>.
        /// </returns>
        /// <remarks>
        /// http://stackoverflow.com/a/311179/1083771
        /// </remarks>
        public static byte[] HexStringToByteArray(string hexString)
        {
            if (hexString == null)
            {
                throw new ArgumentNullException("hexString");
            }

            int byteCount = hexString.Length / 2;
            byte[] result = new byte[byteCount];
            using (StringReader sr = new StringReader(hexString))
            {
                for (int i = 0; i < byteCount; i++)
                {
                    // Read 2 characters, each representing a nibble.
                    char nibble1 = (char)sr.Read();
                    char nibble2 = (char)sr.Read();

                    string hexByte = String.Concat(nibble1, nibble2);

                    // The new 2-char string is a number in base-16.
                    result[i] = Convert.ToByte(hexByte, 16);
                }
            }

            return result;
        }

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

            ImmutableList<byte> hashResult = hashAlgorithm.CalculateHash(dataToHash);
            return hashResult.SequenceEqual(node.Data);
        }
    }
}
