using System;
using System.Collections.Generic;

using Moq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunnerMiscellaneousTests
    {
        [Theory]
        [InlineData(ScriptOpcode.OP_NOP)]
        [InlineData(ScriptOpcode.OP_NOP1)]
        [InlineData(ScriptOpcode.OP_NOP2)]
        [InlineData(ScriptOpcode.OP_NOP3)]
        [InlineData(ScriptOpcode.OP_NOP4)]
        [InlineData(ScriptOpcode.OP_NOP5)]
        [InlineData(ScriptOpcode.OP_NOP6)]
        [InlineData(ScriptOpcode.OP_NOP7)]
        [InlineData(ScriptOpcode.OP_NOP8)]
        [InlineData(ScriptOpcode.OP_NOP9)]
        [InlineData(ScriptOpcode.OP_NOP10)]
        public void NoopShouldNotAffectStacks(ScriptOpcode opcode)
        {
            Stack<FancyByteArray> stack = new Stack<FancyByteArray>();
            byte[] data1 = Guid.NewGuid().ToByteArray();
            byte[] data2 = Guid.NewGuid().ToByteArray();
            byte[] data3 = Guid.NewGuid().ToByteArray();
            stack.Push(data3);
            stack.Push(data2);
            stack.Push(data1);

            TransactionScriptOperation[] script =
            {
                (byte)opcode
            };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();

            ScriptEvaluationResult result = sut.EvaluateScript(script, Mock.Of<ISignatureChecker>(), stack);

            Assert.Equal(3, stack.Count);
            Assert.Equal<byte>(data1, (byte[])stack.Pop());
            Assert.Equal<byte>(data2, (byte[])stack.Pop());
            Assert.Equal<byte>(data3, (byte[])stack.Pop());

            Assert.Equal(0, result.AlternateStack.Count);
        }
    }
}
