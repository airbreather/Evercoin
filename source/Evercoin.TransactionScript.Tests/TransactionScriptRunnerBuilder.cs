using System;
using System.Collections.Generic;
using System.Linq;

using Moq;

namespace Evercoin.TransactionScript
{
    internal sealed class TransactionScriptRunnerBuilder
    {
        private readonly Mock<IHashAlgorithmStore> hashAlgorithmStore = new Mock<IHashAlgorithmStore> { DefaultValue = DefaultValue.Mock };

        public static implicit operator TransactionScriptRunner(TransactionScriptRunnerBuilder builder)
        {
            return new TransactionScriptRunner(builder.hashAlgorithmStore.Object);
        }

        public TransactionScriptRunnerBuilder WithHashAlgorithm(Guid hashAlgorithmIdentifier, IHashAlgorithm hashAlgorithm)
        {
            this.hashAlgorithmStore.Setup(x => x.GetHashAlgorithm(hashAlgorithmIdentifier))
                                   .Returns(hashAlgorithm);

            return this;
        }
    }
}
