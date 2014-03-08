using System.Collections.Immutable;

using Evercoin.Util;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.Tests
{
    public sealed class ByteTwiddlingTests
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
        public void DeleteAllOccurrencesOfSubsequenceShouldDeleteAllOccurrencesOfSubsequence(string longString, string substring, string expected)
        {
            ImmutableList<byte> bytes = ImmutableList.CreateRange(ByteTwiddling.HexStringToByteArray(longString));
            byte[] subsequence = ByteTwiddling.HexStringToByteArray(substring);
            byte[] expectedBytes = ByteTwiddling.HexStringToByteArray(expected);

            IImmutableList<byte> actual = bytes;
            Assert.Equal(expectedBytes, actual);
        }
    }
}
