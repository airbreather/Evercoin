using System;

using Moq;

namespace Evercoin.TransactionScript
{
    internal sealed class TransactionScriptRunnerBuilder
    {
        private readonly Mock<IHashAlgorithmStore> hashAlgorithmStore = new Mock<IHashAlgorithmStore> { DefaultValue = DefaultValue.Mock };

        private readonly Mock<IChainParameters> chainParameters = new Mock<IChainParameters>();

        public static implicit operator TransactionScriptRunner(TransactionScriptRunnerBuilder builder)
        {
            return new TransactionScriptRunner(builder.hashAlgorithmStore.Object, builder.chainParameters.Object);
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithm(Guid hashAlgorithmIdentifier, IHashAlgorithm hashAlgorithm)
        {
            this.hashAlgorithmStore.Setup(x => x.GetHashAlgorithm(hashAlgorithmIdentifier))
                                   .Returns(hashAlgorithm);

            return this;
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithmIdentifier1(Guid hashAlgorithmIdentifier)
        {
            this.chainParameters.Setup(x => x.ScriptHashAlgorithmIdentifier1)
                                .Returns(hashAlgorithmIdentifier);

            return this;
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithmIdentifier2(Guid hashAlgorithmIdentifier)
        {
            this.chainParameters.Setup(x => x.ScriptHashAlgorithmIdentifier2)
                                .Returns(hashAlgorithmIdentifier);

            return this;
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithmIdentifier3(Guid hashAlgorithmIdentifier)
        {
            this.chainParameters.Setup(x => x.ScriptHashAlgorithmIdentifier3)
                                .Returns(hashAlgorithmIdentifier);

            return this;
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithmIdentifier4(Guid hashAlgorithmIdentifier)
        {
            this.chainParameters.Setup(x => x.ScriptHashAlgorithmIdentifier4)
                                .Returns(hashAlgorithmIdentifier);

            return this;
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithmIdentifier5(Guid hashAlgorithmIdentifier)
        {
            this.chainParameters.Setup(x => x.ScriptHashAlgorithmIdentifier5)
                                .Returns(hashAlgorithmIdentifier);

            return this;
        }
    }
}
