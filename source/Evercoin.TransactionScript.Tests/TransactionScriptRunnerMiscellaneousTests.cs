using Moq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunnerMiscellaneousTests
    {
        [Theory]
        [InlineData(ScriptOperation.OP_NOP)]
        [InlineData(ScriptOperation.OP_NOP1)]
        [InlineData(ScriptOperation.OP_NOP2)]
        [InlineData(ScriptOperation.OP_NOP3)]
        [InlineData(ScriptOperation.OP_NOP4)]
        [InlineData(ScriptOperation.OP_NOP5)]
        [InlineData(ScriptOperation.OP_NOP6)]
        [InlineData(ScriptOperation.OP_NOP7)]
        [InlineData(ScriptOperation.OP_NOP8)]
        [InlineData(ScriptOperation.OP_NOP9)]
        [InlineData(ScriptOperation.OP_NOP10)]
        public void NoopOnTrueStackShouldPass(ScriptOperation opcode)
        {
            byte[] passingScriptData =
            {
                (byte)ScriptOperation.OP_1,
                (byte)opcode
            };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();

            Assert.True(sut.EvaluateScript(passingScriptData, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOperation.OP_NOP)]
        [InlineData(ScriptOperation.OP_NOP1)]
        [InlineData(ScriptOperation.OP_NOP2)]
        [InlineData(ScriptOperation.OP_NOP3)]
        [InlineData(ScriptOperation.OP_NOP4)]
        [InlineData(ScriptOperation.OP_NOP5)]
        [InlineData(ScriptOperation.OP_NOP6)]
        [InlineData(ScriptOperation.OP_NOP7)]
        [InlineData(ScriptOperation.OP_NOP8)]
        [InlineData(ScriptOperation.OP_NOP9)]
        [InlineData(ScriptOperation.OP_NOP10)]
        public void NoopOnFalseStackShouldFail(ScriptOperation opcode)
        {
            byte[] failingScriptData =
            {
                (byte)ScriptOperation.OP_0,
                (byte)opcode
            };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();

            Assert.False(sut.EvaluateScript(failingScriptData, Mock.Of<ISignatureChecker>()));
        }
    }
}
