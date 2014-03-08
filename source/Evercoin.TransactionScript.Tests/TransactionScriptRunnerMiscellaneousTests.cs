using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
            Stack<StackItem> stack = new Stack<StackItem>();
            byte[] data1 = Guid.NewGuid().ToByteArray();
            byte[] data2 = Guid.NewGuid().ToByteArray();
            byte[] data3 = Guid.NewGuid().ToByteArray();
            stack.Push(data3);
            stack.Push(data2);
            stack.Push(data1);

            ImmutableList<TransactionScriptOperation> script = ImmutableList.Create<TransactionScriptOperation>
            (
                (byte)opcode
            );

            byte[] scriptBytes = Guid.NewGuid().ToByteArray();
            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder()
                .WithParsedScript(scriptBytes, script);

            ScriptEvaluationResult result = sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>(), stack);

            Assert.Equal(3, stack.Count);
            Assert.Equal(data1, (ImmutableList<byte>)stack.Pop());
            Assert.Equal(data2, (ImmutableList<byte>)stack.Pop());
            Assert.Equal(data3, (ImmutableList<byte>)stack.Pop());

            Assert.Equal(0, result.AlternateStack.Count);
        }
    }
}
