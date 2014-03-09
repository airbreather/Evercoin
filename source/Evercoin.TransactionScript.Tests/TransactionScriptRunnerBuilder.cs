using System;
using System.Collections.Generic;
using System.Linq;

using Moq;

namespace Evercoin.TransactionScript
{
    internal sealed class TransactionScriptRunnerBuilder
    {
        private readonly Mock<IHashAlgorithmStore> hashAlgorithmStore = new Mock<IHashAlgorithmStore> { DefaultValue = DefaultValue.Mock };
        private readonly Mock<ITransactionScriptParser> transactionScriptParser = new Mock<ITransactionScriptParser>();

        public TransactionScriptRunnerBuilder()
        {
            this.transactionScriptParser.Setup(x => x.Parse(It.IsAny<IEnumerable<byte>>()))
                                        .Returns(new TransactionScriptOperation[0]);
        }

        public static implicit operator TransactionScriptRunner(TransactionScriptRunnerBuilder builder)
        {
            return new TransactionScriptRunner(builder.hashAlgorithmStore.Object,
                                               builder.transactionScriptParser.Object);
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithm(Guid hashAlgorithmIdentifier, IHashAlgorithm hashAlgorithm)
        {
            this.hashAlgorithmStore.Setup(x => x.GetHashAlgorithm(hashAlgorithmIdentifier))
                                   .Returns(hashAlgorithm);

            return this;
        }

        public TransactionScriptRunnerBuilder WithParsedScript(IEnumerable<byte> scriptBytes, TransactionScriptOperation[] parsedScript)
        {
            this.transactionScriptParser.Setup(x => x.Parse(It.Is<IEnumerable<byte>>(s => scriptBytes.SequenceEqual(s))))
                                        .Returns(parsedScript);

            return this;
        }
    }
}
