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
        public void NoopOnTrueStackShouldPass(ScriptOpcode opcode)
        {
            byte[] passingScriptData =
            {
                (byte)ScriptOpcode.OP_1,
                (byte)opcode
            };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();

            Assert.True(sut.EvaluateScript(passingScriptData, Mock.Of<ISignatureChecker>()));
        }

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
        public void NoopOnFalseStackShouldFail(ScriptOpcode opcode)
        {
            byte[] failingScriptData =
            {
                (byte)ScriptOpcode.OP_0,
                (byte)opcode
            };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();

            Assert.False(sut.EvaluateScript(failingScriptData, Mock.Of<ISignatureChecker>()));
        }
    }
}
