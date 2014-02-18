using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using NodaTime;

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

        public static IImmutableList<byte> DeleteAllOccurrencesOfSubsequence(this IImmutableList<byte> scriptCode, IReadOnlyList<byte> signature)
        {
            if (scriptCode == null)
            {
                throw new ArgumentNullException("scriptCode");
            }

            if (signature == null)
            {
                throw new ArgumentNullException("signature");
            }

            if (scriptCode.Count == 0 || signature.Count == 0)
            {
                return scriptCode;
            }

            int i;
            int[] kmpLookup = CreateKMPLookup(signature);
            while ((i = FindIndexOfNeedleInHaystack(scriptCode, signature, kmpLookup)) < scriptCode.Count)
            {
                scriptCode = scriptCode.RemoveRange(i, signature.Count);
            }

            return scriptCode;
        }

        // http://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        private static int FindIndexOfNeedleInHaystack(IReadOnlyList<byte> haystack, IReadOnlyList<byte> needle, IReadOnlyList<int> lookup)
        {
            int m = 0;
            int i = 0;
            while (m + i < haystack.Count)
            {
                if (needle[i] == haystack[m + i])
                {
                    if (i++ == needle.Count - 1)
                    {
                        return m;
                    }
                }
                else
                {
                    m = m + i - lookup[i];
                    i = lookup[i] > -1 ? lookup[i] : 0;
                }
            }

            return haystack.Count;
        }

        // http://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        private static int[] CreateKMPLookup(IReadOnlyList<byte> needle)
        {
            int[] lookup = new int[needle.Count];
            lookup[0] = -1;
            int pos = 2;
            int cnd = 0;
            while (pos < needle.Count)
            {
                if (needle[pos - 1] == needle[cnd])
                {
                    lookup[pos++] = ++cnd;
                }
                else if (cnd > 0)
                {
                    cnd = lookup[cnd];
                }
                else
                {
                    lookup[pos++] = 0;
                }
            }

            return lookup;
        }
    }
}
