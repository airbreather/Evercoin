﻿using System;
using System.Collections.Generic;
using System.Linq;

using Moq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunnerBroadTests
    {
        public static IEnumerable<object[]> MissingOpcodes
        {
            get
            {
                return Enumerable.Range((int)ScriptOpcode.BEGIN_UNUSED, ScriptOpcode.END_UNUSED - ScriptOpcode.BEGIN_UNUSED + 1)
                                 .Select(opcode => new object[] { (ScriptOpcode)opcode });
            }
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

        [Theory]
        [InlineData(ScriptOpcode.OP_CAT)]
        [InlineData(ScriptOpcode.OP_SUBSTR)]
        [InlineData(ScriptOpcode.OP_LEFT)]
        [InlineData(ScriptOpcode.OP_RIGHT)]
        [InlineData(ScriptOpcode.OP_INVERT)]
        [InlineData(ScriptOpcode.OP_AND)]
        [InlineData(ScriptOpcode.OP_OR)]
        [InlineData(ScriptOpcode.OP_XOR)]
        [InlineData(ScriptOpcode.OP_2MUL)]
        [InlineData(ScriptOpcode.OP_2DIV)]
        [InlineData(ScriptOpcode.OP_MUL)]
        [InlineData(ScriptOpcode.OP_DIV)]
        [InlineData(ScriptOpcode.OP_MOD)]
        [InlineData(ScriptOpcode.OP_LSHIFT)]
        [InlineData(ScriptOpcode.OP_RSHIFT)]
        [InlineData(ScriptOpcode.OP_VERIF)]
        [InlineData(ScriptOpcode.OP_VERNOTIF)]
        [PropertyData("MissingOpcodes")]
        public void DisabledOpcodesShouldCauseScriptFailureEvenIfNotExecuted(ScriptOpcode disabledOpcode)
        {
            byte[] scriptBytes = {
                                     (byte)ScriptOpcode.OP_1,
                                     (byte)ScriptOpcode.OP_IF,
                                     (byte)ScriptOpcode.OP_1,
                                     (byte)ScriptOpcode.OP_ELSE,
                                     (byte)disabledOpcode,
                                     (byte)ScriptOpcode.OP_ENDIF
                                 };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOpcode.OP_VER)]
        [InlineData(ScriptOpcode.OP_RESERVED)]
        [InlineData(ScriptOpcode.OP_RESERVED1)]
        [InlineData(ScriptOpcode.OP_RESERVED2)]
        public void ReservedOpcodesShouldCauseScriptFailureIfExecuted(ScriptOpcode reservedOpcode)
        {
            byte[] scriptBytes = {
                                     (byte)ScriptOpcode.OP_1,
                                     (byte)ScriptOpcode.OP_IF,
                                     (byte)reservedOpcode,
                                     (byte)ScriptOpcode.OP_ELSE,
                                     (byte)ScriptOpcode.OP_1,
                                     (byte)ScriptOpcode.OP_ENDIF
                                 };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOpcode.OP_VER)]
        [InlineData(ScriptOpcode.OP_RESERVED)]
        [InlineData(ScriptOpcode.OP_RESERVED1)]
        [InlineData(ScriptOpcode.OP_RESERVED2)]
        public void ReservedOpcodesShouldNotCauseScriptFailureIfUnexecuted(ScriptOpcode reservedOpcode)
        {
            byte[] scriptBytes = {
                                     (byte)ScriptOpcode.OP_1,
                                     (byte)ScriptOpcode.OP_IF,
                                     (byte)ScriptOpcode.OP_1,
                                     (byte)ScriptOpcode.OP_ELSE,
                                     (byte)reservedOpcode,
                                     (byte)ScriptOpcode.OP_ENDIF
                                 };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.True(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOpcode.OP_IF, 1)]
        [InlineData(ScriptOpcode.OP_NOTIF, 1)]
        [InlineData(ScriptOpcode.OP_VERIFY, 1)]
        [InlineData(ScriptOpcode.OP_DROP, 1)]
        [InlineData(ScriptOpcode.OP_DUP, 1)]
        [InlineData(ScriptOpcode.OP_IFDUP, 1)]
        [InlineData(ScriptOpcode.OP_2DROP, 2)]
        [InlineData(ScriptOpcode.OP_2DUP, 2)]
        [InlineData(ScriptOpcode.OP_3DUP, 3)]
        [InlineData(ScriptOpcode.OP_2OVER, 4)]
        [InlineData(ScriptOpcode.OP_2ROT, 6)]
        [InlineData(ScriptOpcode.OP_2SWAP, 4)]
        [InlineData(ScriptOpcode.OP_NIP, 2)]
        [InlineData(ScriptOpcode.OP_OVER, 2)]
        [InlineData(ScriptOpcode.OP_PICK, 1)]
        [InlineData(ScriptOpcode.OP_ROLL, 1)]
        [InlineData(ScriptOpcode.OP_ROT, 3)]
        [InlineData(ScriptOpcode.OP_SWAP, 2)]
        [InlineData(ScriptOpcode.OP_TUCK, 2)]
        [InlineData(ScriptOpcode.OP_SIZE, 1)]
        [InlineData(ScriptOpcode.OP_EQUAL, 2)]
        [InlineData(ScriptOpcode.OP_EQUALVERIFY, 2)]
        [InlineData(ScriptOpcode.OP_1ADD, 1)]
        [InlineData(ScriptOpcode.OP_1SUB, 1)]
        [InlineData(ScriptOpcode.OP_NEGATE, 1)]
        [InlineData(ScriptOpcode.OP_ABS, 1)]
        [InlineData(ScriptOpcode.OP_NOT, 1)]
        [InlineData(ScriptOpcode.OP_0NOTEQUAL, 1)]
        [InlineData(ScriptOpcode.OP_ADD, 2)]
        [InlineData(ScriptOpcode.OP_SUB, 2)]
        [InlineData(ScriptOpcode.OP_BOOLAND, 2)]
        [InlineData(ScriptOpcode.OP_BOOLOR, 2)]
        [InlineData(ScriptOpcode.OP_NUMEQUAL, 2)]
        [InlineData(ScriptOpcode.OP_NUMEQUALVERIFY, 2)]
        [InlineData(ScriptOpcode.OP_NUMNOTEQUAL, 2)]
        [InlineData(ScriptOpcode.OP_LESSTHAN, 2)]
        [InlineData(ScriptOpcode.OP_GREATERTHAN, 2)]
        [InlineData(ScriptOpcode.OP_LESSTHANOREQUAL, 2)]
        [InlineData(ScriptOpcode.OP_GREATERTHANOREQUAL, 2)]
        [InlineData(ScriptOpcode.OP_MIN, 2)]
        [InlineData(ScriptOpcode.OP_MAX, 2)]
        [InlineData(ScriptOpcode.OP_WITHIN, 3)]
        [InlineData(ScriptOpcode.OP_RIPEMD160, 1)]
        [InlineData(ScriptOpcode.OP_SHA1, 1)]
        [InlineData(ScriptOpcode.OP_SHA256, 1)]
        [InlineData(ScriptOpcode.OP_HASH160, 1)]
        [InlineData(ScriptOpcode.OP_HASH256, 1)]
        [InlineData(ScriptOpcode.OP_CHECKSIG, 2)]
        [InlineData(ScriptOpcode.OP_CHECKSIGVERIFY, 2)]
        [InlineData(ScriptOpcode.OP_CHECKMULTISIG, 1)]
        [InlineData(ScriptOpcode.OP_CHECKMULTISIGVERIFY, 1)]
        public void StackManipulationOpcodesShouldCauseScriptFailureWithoutEnoughItemsOnStack(ScriptOpcode opcode, int requiredStackDepth)
        {
            List<byte> scriptBytes = new List<byte>();
            for (int i = 0; i < requiredStackDepth - 1; i++)
            {
                scriptBytes.Add((byte)ScriptOpcode.OP_1);
            }

            scriptBytes.Add((byte)opcode);

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }
    }
}
