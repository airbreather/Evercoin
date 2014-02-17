using System;

using Evercoin.Util;

using Moq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunnerPushDataTests
    {
        // All opcodes between 0 and END_OP_DATA (= 75), inclusive, mean
        // "push the next n bytes of data onto the stack", where "n" is the
        // value of the opcode.
        [Theory]
        [InlineData(2, 1)]
        [InlineData(5, 3)]
        [InlineData(19, 18)]
        [InlineData(72, 71)]
        [InlineData(75, 74)]
        public void PushDataWithoutProvidingEnoughDataShouldFailScript(int opcode, int numberOfBytesToProvide)
        {
            byte[] scriptBytes = new byte[numberOfBytesToProvide + 1];
            scriptBytes[0] = (byte)opcode;
            scriptBytes[1] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(5, 3)]
        [InlineData(19, 18)]
        [InlineData(72, 71)]
        [InlineData(75, 74)]
        [InlineData(byte.MaxValue, byte.MaxValue - 1)]
        public void PushData1WithoutProvidingEnoughDataShouldFailScript(int numberOfBytesToExpect, int numberOfBytesToProvide)
        {
            byte[] scriptBytes = new byte[numberOfBytesToProvide + 2];
            scriptBytes[0] = (byte)ScriptOperation.OP_PUSHDATA1;
            scriptBytes[1] = (byte)numberOfBytesToExpect;
            scriptBytes[2] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(5, 3)]
        [InlineData(19, 18)]
        [InlineData(72, 71)]
        [InlineData(75, 74)]
        [InlineData(byte.MaxValue, byte.MaxValue - 1)]
        [InlineData(byte.MaxValue + 1, byte.MaxValue)]
        [InlineData(ushort.MaxValue, ushort.MaxValue - 1)]
        public void PushData2WithoutProvidingEnoughDataShouldFailScript(int numberOfBytesToExpect, int numberOfBytesToProvide)
        {
            byte[] scriptBytes = new byte[numberOfBytesToProvide + 3];

            byte[] dataSizeBytes = BitConverter.GetBytes((ushort)numberOfBytesToExpect)
                                               .MakeLittleEndian();

            scriptBytes[0] = (byte)ScriptOperation.OP_PUSHDATA2;
            scriptBytes[1] = dataSizeBytes[0];
            scriptBytes[2] = dataSizeBytes[1];
            scriptBytes[3] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(5, 3)]
        [InlineData(19, 18)]
        [InlineData(72, 71)]
        [InlineData(75, 74)]
        [InlineData(byte.MaxValue, byte.MaxValue - 1)]
        [InlineData(byte.MaxValue + 1, byte.MaxValue)]
        [InlineData(ushort.MaxValue, ushort.MaxValue - 1)]
        [InlineData(ushort.MaxValue + 1, ushort.MaxValue)]
        [InlineData(0x00FFFFFF, 0x00FFFFFE)]
        [InlineData(0x01000000, 0x00FFFFFF)]
        public void PushData4WithoutProvidingEnoughDataShouldFailScript(int numberOfBytesToExpect, int numberOfBytesToProvide)
        {
            byte[] scriptBytes = new byte[numberOfBytesToProvide + 5];

            byte[] dataSizeBytes = BitConverter.GetBytes((uint)numberOfBytesToExpect)
                                               .MakeLittleEndian();

            scriptBytes[0] = (byte)ScriptOperation.OP_PUSHDATA4;
            scriptBytes[1] = dataSizeBytes[0];
            scriptBytes[2] = dataSizeBytes[1];
            scriptBytes[3] = dataSizeBytes[2];
            scriptBytes[4] = dataSizeBytes[3];
            scriptBytes[5] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 5)]
        [InlineData(19, 19)]
        [InlineData(72, 72)]
        [InlineData(75, 75)]
        public void PushDataProvidingJustEnoughDataShouldPassScript(int opcode, int numberOfBytesToProvide)
        {
            byte[] scriptBytes = new byte[numberOfBytesToProvide + 1];
            scriptBytes[0] = (byte)opcode;
            scriptBytes[1] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.True(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(ScriptOperation.OP_0, 0)]
        [InlineData(ScriptOperation.OP_PUSHDATA1, 1)]
        [InlineData(ScriptOperation.OP_PUSHDATA2, 2)]
        [InlineData(ScriptOperation.OP_PUSHDATA4, 4)]
        public void PushZeroShouldPutFalseOnTheStack(ScriptOperation opcode, int numberOfZeroBytesAfterOpcode)
        {
            byte[] scriptBytes = new byte[numberOfZeroBytesAfterOpcode + 1];
            scriptBytes[0] = (byte)opcode;
            for (int i = 0; i < numberOfZeroBytesAfterOpcode; i++)
            {
                scriptBytes[i + 1] = 0;
            }

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.False(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(17)]
        [InlineData(42)]
        [InlineData(byte.MaxValue - 1)]
        [InlineData(byte.MaxValue)]
        public void PushData1ShouldUseNextByte(int numberOfBytesToPush)
        {
            byte[] scriptBytes = new byte[numberOfBytesToPush + 2];

            scriptBytes[0] = (byte)ScriptOperation.OP_PUSHDATA1;
            scriptBytes[1] = (byte)numberOfBytesToPush;
            scriptBytes[2] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.True(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(17)]
        [InlineData(42)]
        [InlineData(byte.MaxValue)]
        [InlineData(byte.MaxValue + 1)]
        [InlineData(ushort.MaxValue - 1)]
        [InlineData(ushort.MaxValue)]
        public void PushData2ShouldUseNextTwoBytes(int numberOfBytesToPush)
        {
            byte[] scriptBytes = new byte[numberOfBytesToPush + 3];

            byte[] dataSizeBytes = BitConverter.GetBytes((ushort)numberOfBytesToPush)
                                               .MakeLittleEndian();

            scriptBytes[0] = (byte)ScriptOperation.OP_PUSHDATA2;
            scriptBytes[1] = dataSizeBytes[0];
            scriptBytes[2] = dataSizeBytes[1];
            scriptBytes[3] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.True(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(17)]
        [InlineData(42)]
        [InlineData(byte.MaxValue)]
        [InlineData(byte.MaxValue + 1)]
        [InlineData(ushort.MaxValue - 1)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MaxValue + 1)]
        [InlineData(0x00FFFFFF)]
        [InlineData(0x01000000)]
        public void PushData4ShouldUseNextFourBytes(int numberOfBytesToPush)
        {
            byte[] scriptBytes = new byte[numberOfBytesToPush + 5];

            byte[] dataSizeBytes = BitConverter.GetBytes((uint)numberOfBytesToPush)
                                               .MakeLittleEndian();

            scriptBytes[0] = (byte)ScriptOperation.OP_PUSHDATA4;
            scriptBytes[1] = dataSizeBytes[0];
            scriptBytes[2] = dataSizeBytes[1];
            scriptBytes[3] = dataSizeBytes[2];
            scriptBytes[4] = dataSizeBytes[3];
            scriptBytes[5] = 1;

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();
            Assert.True(sut.EvaluateScript(scriptBytes, Mock.Of<ISignatureChecker>()));
        }
    }
}
