using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Moq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunnerTests
    {
        public static IEnumerable<object[]> MissingOpcodes
        {
            get
            {
                return Enumerable.Range((int)ScriptOperation.BEGIN_UNUSED, ScriptOperation.END_UNUSED - ScriptOperation.BEGIN_UNUSED + 1)
                                 .Select(opcode => new object[] { (ScriptOperation)opcode });
            }
        }

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

        [Theory]
        [InlineData(ScriptOperation.OP_CAT)]
        [InlineData(ScriptOperation.OP_SUBSTR)]
        [InlineData(ScriptOperation.OP_LEFT)]
        [InlineData(ScriptOperation.OP_RIGHT)]
        [InlineData(ScriptOperation.OP_INVERT)]
        [InlineData(ScriptOperation.OP_AND)]
        [InlineData(ScriptOperation.OP_OR)]
        [InlineData(ScriptOperation.OP_XOR)]
        [InlineData(ScriptOperation.OP_2MUL)]
        [InlineData(ScriptOperation.OP_2DIV)]
        [InlineData(ScriptOperation.OP_MUL)]
        [InlineData(ScriptOperation.OP_DIV)]
        [InlineData(ScriptOperation.OP_MOD)]
        [InlineData(ScriptOperation.OP_LSHIFT)]
        [InlineData(ScriptOperation.OP_RSHIFT)]
        [InlineData(ScriptOperation.OP_VERIF)]
        [InlineData(ScriptOperation.OP_VERNOTIF)]
        [PropertyData("MissingOpcodes")]
        public void DisabledOpcodesShouldCauseScriptFailureEvenIfNotExecuted(ScriptOperation disabledOpcode)
        {
            byte[] scriptBytes = {
                                     (byte)ScriptOperation.OP_1,
                                     (byte)ScriptOperation.OP_IF,
                                     (byte)ScriptOperation.OP_1,
                                     (byte)ScriptOperation.OP_ELSE,
                                     (byte)disabledOpcode,
                                     (byte)ScriptOperation.OP_ENDIF
                                 };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOperation.OP_VER)]
        [InlineData(ScriptOperation.OP_RESERVED)]
        [InlineData(ScriptOperation.OP_RESERVED1)]
        [InlineData(ScriptOperation.OP_RESERVED2)]
        public void ReservedOpcodesShouldCauseScriptFailureIfExecuted(ScriptOperation reservedOpcode)
        {
            byte[] scriptBytes = {
                                     (byte)ScriptOperation.OP_1,
                                     (byte)ScriptOperation.OP_IF,
                                     (byte)reservedOpcode,
                                     (byte)ScriptOperation.OP_ELSE,
                                     (byte)ScriptOperation.OP_1,
                                     (byte)ScriptOperation.OP_ENDIF
                                 };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOperation.OP_VER)]
        [InlineData(ScriptOperation.OP_RESERVED)]
        [InlineData(ScriptOperation.OP_RESERVED1)]
        [InlineData(ScriptOperation.OP_RESERVED2)]
        public void ReservedOpcodesShouldNotCauseScriptFailureIfUnexecuted(ScriptOperation reservedOpcode)
        {
            byte[] scriptBytes = {
                                     (byte)ScriptOperation.OP_1,
                                     (byte)ScriptOperation.OP_IF,
                                     (byte)ScriptOperation.OP_1,
                                     (byte)ScriptOperation.OP_ELSE,
                                     (byte)reservedOpcode,
                                     (byte)ScriptOperation.OP_ENDIF
                                 };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.True(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOperation.OP_IF, 1)]
        [InlineData(ScriptOperation.OP_NOTIF, 1)]
        [InlineData(ScriptOperation.OP_VERIFY, 1)]
        [InlineData(ScriptOperation.OP_DROP, 1)]
        [InlineData(ScriptOperation.OP_DUP, 1)]
        [InlineData(ScriptOperation.OP_IFDUP, 1)]
        [InlineData(ScriptOperation.OP_2DROP, 2)]
        [InlineData(ScriptOperation.OP_2DUP, 2)]
        [InlineData(ScriptOperation.OP_3DUP, 3)]
        [InlineData(ScriptOperation.OP_2OVER, 4)]
        [InlineData(ScriptOperation.OP_2ROT, 6)]
        [InlineData(ScriptOperation.OP_2SWAP, 4)]
        [InlineData(ScriptOperation.OP_NIP, 2)]
        [InlineData(ScriptOperation.OP_OVER, 2)]
        [InlineData(ScriptOperation.OP_PICK, 1)]
        [InlineData(ScriptOperation.OP_ROLL, 1)]
        [InlineData(ScriptOperation.OP_ROT, 3)]
        [InlineData(ScriptOperation.OP_SWAP, 2)]
        [InlineData(ScriptOperation.OP_TUCK, 2)]
        [InlineData(ScriptOperation.OP_SIZE, 1)]
        [InlineData(ScriptOperation.OP_EQUAL, 2)]
        [InlineData(ScriptOperation.OP_EQUALVERIFY, 2)]
        [InlineData(ScriptOperation.OP_1ADD, 1)]
        [InlineData(ScriptOperation.OP_1SUB, 1)]
        [InlineData(ScriptOperation.OP_NEGATE, 1)]
        [InlineData(ScriptOperation.OP_ABS, 1)]
        [InlineData(ScriptOperation.OP_NOT, 1)]
        [InlineData(ScriptOperation.OP_0NOTEQUAL, 1)]
        [InlineData(ScriptOperation.OP_ADD, 2)]
        [InlineData(ScriptOperation.OP_SUB, 2)]
        [InlineData(ScriptOperation.OP_BOOLAND, 2)]
        [InlineData(ScriptOperation.OP_BOOLOR, 2)]
        [InlineData(ScriptOperation.OP_NUMEQUAL, 2)]
        [InlineData(ScriptOperation.OP_NUMEQUALVERIFY, 2)]
        [InlineData(ScriptOperation.OP_NUMNOTEQUAL, 2)]
        [InlineData(ScriptOperation.OP_LESSTHAN, 2)]
        [InlineData(ScriptOperation.OP_GREATERTHAN, 2)]
        [InlineData(ScriptOperation.OP_LESSTHANOREQUAL, 2)]
        [InlineData(ScriptOperation.OP_GREATERTHANOREQUAL, 2)]
        [InlineData(ScriptOperation.OP_MIN, 2)]
        [InlineData(ScriptOperation.OP_MAX, 2)]
        [InlineData(ScriptOperation.OP_WITHIN, 3)]
        [InlineData(ScriptOperation.OP_RIPEMD160, 1)]
        [InlineData(ScriptOperation.OP_SHA1, 1)]
        [InlineData(ScriptOperation.OP_SHA256, 1)]
        [InlineData(ScriptOperation.OP_HASH160, 1)]
        [InlineData(ScriptOperation.OP_HASH256, 1)]
        [InlineData(ScriptOperation.OP_CHECKSIG, 2)]
        [InlineData(ScriptOperation.OP_CHECKSIGVERIFY, 2)]
        [InlineData(ScriptOperation.OP_CHECKMULTISIG, 1)]
        [InlineData(ScriptOperation.OP_CHECKMULTISIGVERIFY, 1)]
        public void StackManipulationOpcodesShouldCauseScriptFailureWithoutEnoughItemsOnStack(ScriptOperation opcode, int requiredStackDepth)
        {
            List<byte> scriptBytes = new List<byte>();
            for (int i = 0; i < requiredStackDepth - 1; i++)
            {
                scriptBytes.Add((byte)ScriptOperation.OP_1);
            }

            scriptBytes.Add((byte)opcode);

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Fact]
        public void ConstructorShouldThrowOnNullHashAlgorithmStore()
        {
            ArgumentNullException thrownException = Assert.Throws<ArgumentNullException>(() => new TransactionScriptRunner(null));
            Assert.Equal("hashAlgorithmStore", thrownException.ParamName);
        }

        [Fact]
        public void EvaluateScriptShouldThrowOnNullScript()
        {
            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            ArgumentNullException thrownException = Assert.Throws<ArgumentNullException>(() => sut.EvaluateScript(null, Mock.Of<ISignatureChecker>()));
            Assert.Equal("serializedScript", thrownException.ParamName);
        }

        [Fact]
        public void EvaluateScriptShouldThrowOnNullSignatureChecker()
        {
            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            ArgumentNullException thrownException = Assert.Throws<ArgumentNullException>(() => sut.EvaluateScript(Enumerable.Empty<byte>(), null));
            Assert.Equal("signatureChecker", thrownException.ParamName);
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
