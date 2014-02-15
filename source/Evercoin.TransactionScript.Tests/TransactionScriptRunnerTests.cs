using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunnerTests
    {
        // Bit twiddling is always so fun.
        // I wouldn't let myself get away with writing the SUT before this.
        [Theory]
        [InlineData("0AF0", "0A", "F0")]
        [InlineData("0AF0", "F0", "0A")]
        [InlineData("F0", "0A", "F0")]
        [InlineData("0FAFF0", "FAFF", "0FAFF0")]
        [InlineData("FA01000000FA0100FA01", "FA01", "00000000")]
        [InlineData("FA01FA01000000FA0100FA01", "FA01", "00000000")]
        [InlineData("FFFFFFFFFF", "FFFFFFFF", "FF")]
        public void DeleteSubsequenceShouldDeleteSubsequence(string longString, string substring, string expected)
        {
            ImmutableList<byte> bytes = ImmutableList.CreateRange(HexStringToByteArray(longString));
            IList<byte> subsequence = HexStringToByteArray(substring).ToList();

            IImmutableList<byte> trimmedSequence = TransactionScriptRunner.DeleteSubsequence(bytes, subsequence);
            string actual = ByteArrayToHexString(trimmedSequence);
            Assert.Equal(expected, actual);
        }

        // http://stackoverflow.com/a/311179/1083771
        private static IEnumerable<byte> HexStringToByteArray(string hexString)
        {
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

        // http://stackoverflow.com/a/14333437/1083771
        static string ByteArrayToHexString(IReadOnlyList<byte> bytes)
        {
            // From the author:
            // Abandon all hope, you who enter here.
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
    }
}
