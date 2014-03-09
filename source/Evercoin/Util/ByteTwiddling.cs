using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                c[i * 2] = (char)(87 + b + (((b - 10) >> 31) & -39));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(87 + b + (((b - 10) >> 31) & -39));
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

        public static byte[] ConcatenateData(params IEnumerable<byte>[] sources)
        {
            return ConcatenateData(sources.AsEnumerable());
        }

        public static byte[] ConcatenateData(IEnumerable<IEnumerable<byte>> sources)
        {
            byte[][] sourceArrays = sources.Select(Extensions.GetArray).GetArray();
            int length = sourceArrays.Sum(x => x.Length);
            byte[] result = new byte[length];

            int index = 0;
            foreach (byte[] sourceArray in sourceArrays)
            {
                Buffer.BlockCopy(sourceArray, 0, result, index, sourceArray.Length);
                index += sourceArray.Length;
            }

            return result;
        }
    }
}
