using System;
using System.Collections.Generic;
using System.Linq;

using Moq;

using Xunit;
using Xunit.Extensions;

namespace Evercoin.TransactionScript
{
    public sealed class TransactionScriptRunnerPushDataTests
    {
        public static IEnumerable<object[]> DataOpcodes
        {
            get
            {
                return Enumerable.Range((int)ScriptOpcode.BEGIN_OP_DATA, ScriptOpcode.END_OP_DATA - ScriptOpcode.BEGIN_OP_DATA + 1)
                                 .Select(opcode => new object[] { (ScriptOpcode)opcode });
            }
        }

        [Theory]
        [PropertyData("DataOpcodes")]
        public void PushDataShouldPushData(int opcode)
        {
            int seed = Guid.NewGuid().GetHashCode();
            Console.WriteLine("seed: {0}", seed);

            Random random = new Random(seed);
            int numberOfBytesToProvide = random.Next(4000);
            byte[] dataArray = new byte[numberOfBytesToProvide];

            random.NextBytes(dataArray);

            TransactionScriptOperation[] script =
            {
                new TransactionScriptOperation((byte)opcode, dataArray)
            };

            TransactionScriptRunner sut = new TransactionScriptRunnerBuilder();

            ScriptEvaluationResult result = sut.EvaluateScript(script, Mock.Of<ISignatureChecker>());

            Stack<FancyByteArray> mainStack = result.MainStack;
            Assert.Equal(1, mainStack.Count);
            Assert.Equal<byte>(dataArray, (byte[])mainStack.Pop());
        }
    }
}
