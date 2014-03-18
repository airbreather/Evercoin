using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Evercoin.Algorithms;
using Evercoin.BaseImplementations;
using Evercoin.Network;
using Evercoin.TransactionScript;
using Evercoin.Util;

using Moq;

using Ninject;
using Ninject.Modules;

using NodaTime;

namespace Evercoin.App
{
    internal sealed class EvercoinModule : NinjectModule
    {
        private readonly IChainStore underlyingChainStorage;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public EvercoinModule(IChainStore underlyingChainStorage, IHashAlgorithmStore hashAlgorithmStore)
        {
            this.underlyingChainStorage = underlyingChainStorage;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        /// <summary>
        /// Loads the module into the kernel.
        /// </summary>
        public override void Load()
        {
            BigInteger maximumTarget = BigInteger.Parse("00000000FFFF0000000000000000000000000000000000000000000000000000", NumberStyles.HexNumber);
            const decimal InitialBlockSubsidyInSatoshis = 5000000000;

            Mock<ICoinbaseValueSource> coinbase = new Mock<ICoinbaseValueSource>();
            coinbase.Setup(x => x.AvailableValue).Returns(InitialBlockSubsidyInSatoshis);

            Mock<IMerkleTreeNode> transactionIdentifiers = new Mock<IMerkleTreeNode>();
            transactionIdentifiers.Setup(x => x.Data).Returns(ByteTwiddling.HexStringToByteArray("4a5e1e4baab89f3a32518a88c31bc87f618f76673e2cc77ab2127b7afdeda33b").Reverse().GetArray());

            Mock<IBlock> genesisBlock = new Mock<IBlock>();
            genesisBlock.Setup(x => x.Version).Returns(1);
            genesisBlock.Setup(x => x.DifficultyTarget).Returns(maximumTarget);
            genesisBlock.Setup(x => x.Nonce).Returns(2083236893);
            genesisBlock.Setup(x => x.Timestamp).Returns(Instant.FromSecondsSinceUnixEpoch(1231006505));
            genesisBlock.Setup(x => x.Coinbase).Returns(coinbase.Object);
            genesisBlock.Setup(x => x.TransactionIdentifiers).Returns(transactionIdentifiers.Object);

            this.Bind<IReadOnlyChainStore>().ToConstant(this.underlyingChainStorage);
            this.Bind<IChainStore>().ToConstant(this.underlyingChainStorage);
            this.Bind<IRawNetwork>().To<RawNetwork>().InSingletonScope();
            this.Bind<ICurrencyNetwork>().To<CurrencyNetwork>().InSingletonScope();
            this.Bind<IHashAlgorithmStore>().ToConstant(this.hashAlgorithmStore);
            this.Bind<ITransactionScriptParser>().To<TransactionScriptParser>();
            this.Bind<ITransactionScriptRunner>().To<TransactionScriptRunner>();
            this.Bind<ISignatureCheckerFactory>().To<ECDSASignatureCheckerFactory>();
            this.Bind<INetworkParameters>().To<SomeNetworkParams>();
            this.Bind<IChainParameters>().ToMethod(ctx => new ChainParameters(genesisBlock.Object, HashAlgorithmIdentifiers.DoubleSHA256, HashAlgorithmIdentifiers.DoubleSHA256, HashAlgorithmIdentifiers.RipeMd160, HashAlgorithmIdentifiers.SHA1, HashAlgorithmIdentifiers.SHA256, HashAlgorithmIdentifiers.SHA256ThenRipeMd160, HashAlgorithmIdentifiers.DoubleSHA256, new[] { SecurityMechanism.ProofOfWork }, Duration.FromMinutes(10), 2016, InitialBlockSubsidyInSatoshis, 0.5m, 210000, maximumTarget));
            this.Bind<ICurrencyParameters>().ToMethod(ctx => new CurrencyParameters(Guid.NewGuid(), "Bitcoin", ctx.Kernel.Get<INetworkParameters>(), this.hashAlgorithmStore, ctx.Kernel.Get<IChainParameters>()));
        }
    }
}
