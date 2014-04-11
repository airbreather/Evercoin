using System;
using System.Linq;
using System.Net;
using System.Numerics;

using Evercoin.Algorithms;
using Evercoin.BaseImplementations;
using Evercoin.Network;
using Evercoin.TransactionScript;

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
            ////FancyByteArray maximumTarget = FancyByteArray.CreateLittleEndianFromHexString("00000000FFFF0000000000000000000000000000000000000000000000000000", Endianness.BigEndian);
            ////FancyByteArray genesisBlockIdentifier = FancyByteArray.CreateLittleEndianFromHexString("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F", Endianness.BigEndian);
            ////FancyByteArray genesisBlockMerkleRoot = FancyByteArray.CreateLittleEndianFromHexString("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B", Endianness.BigEndian);
            ////FancyByteArray maximumTarget = FancyByteArray.CreateLittleEndianFromHexString("00000000FFFF0000000000000000000000000000000000000000000000000000", Endianness.BigEndian);
            ////FancyByteArray genesisBlockIdentifier = FancyByteArray.CreateLittleEndianFromHexString("000000000933EA01AD0EE984209779BAAEC3CED90FA3F408719526F8D77F4943", Endianness.BigEndian);
            ////FancyByteArray genesisBlockMerkleRoot = FancyByteArray.CreateLittleEndianFromHexString("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B", Endianness.BigEndian);
            FancyByteArray maximumTarget = FancyByteArray.CreateFromBigIntegerWithDesiredEndianness(Extensions.TargetFromBits(0x1e0ffff0), Endianness.LittleEndian);
            FancyByteArray genesisBlockIdentifier = FancyByteArray.CreateLittleEndianFromHexString("1A91E3DACE36E2BE3BF030A65679FE821AA1D6EF92E7C9902EB318182C355691", Endianness.BigEndian);
            FancyByteArray genesisBlockMerkleRoot = FancyByteArray.CreateLittleEndianFromHexString("5B2A3F53F605D62C53E62932DAC6925E3D74AFA5A4B459745C36D42D0ED26A69", Endianness.BigEndian);

            ////const decimal InitialBlockSubsidyInSatoshis = 5000000000;
            const decimal InitialBlockSubsidyInSatoshis = 8800000000;

            Mock<IMerkleTreeNode> transactionIdentifiers = new Mock<IMerkleTreeNode>();
            transactionIdentifiers.Setup(x => x.Data).Returns(genesisBlockMerkleRoot);

            Mock<IBlock> genesisBlock = new Mock<IBlock>();
            genesisBlock.Setup(x => x.Version).Returns(1);
            genesisBlock.Setup(x => x.DifficultyTarget).Returns(maximumTarget);
            ////genesisBlock.Setup(x => x.Nonce).Returns(2083236893);
            ////genesisBlock.Setup(x => x.Timestamp).Returns(Instant.FromSecondsSinceUnixEpoch(1231006505));
            ////genesisBlock.Setup(x => x.Nonce).Returns(414098458);
            ////genesisBlock.Setup(x => x.Timestamp).Returns(Instant.FromSecondsSinceUnixEpoch(1296688602));
            genesisBlock.Setup(x => x.Nonce).Returns(99943);
            genesisBlock.Setup(x => x.Timestamp).Returns(Instant.FromSecondsSinceUnixEpoch(1386325540));
            genesisBlock.Setup(x => x.TransactionIdentifiers).Returns(transactionIdentifiers.Object);

            this.underlyingChainStorage.PutBlock(genesisBlockIdentifier, genesisBlock.Object);
            IBlockChain blockChain = new BlockChain();
            blockChain.AddBlockAtHeight(genesisBlockIdentifier, 0);

            INetworkParameters dogeNetworkParameters = new NetworkParameters(70002, 209, HashAlgorithmIdentifiers.DoubleSHA256, 4, 12, 16, FancyByteArray.CreateFromBytes(new byte[] { 0xc0, 0xc0, 0xc0, 0xc0 }), Enumerable.Empty<DnsEndPoint>());

            this.Bind<IReadableChainStore>().ToConstant(this.underlyingChainStorage);
            this.Bind<IChainStore>().ToConstant(this.underlyingChainStorage);
            this.Bind<IRawNetwork>().To<RawNetwork>().InSingletonScope();
            this.Bind<ICurrencyNetwork>().To<CurrencyNetwork>().InSingletonScope();
            this.Bind<IHashAlgorithmStore>().ToConstant(this.hashAlgorithmStore);
            this.Bind<ITransactionScriptParser>().To<TransactionScriptParser>();
            this.Bind<ITransactionScriptRunner>().To<TransactionScriptRunner>();
            this.Bind<ISignatureCheckerFactory>().To<ECDSASignatureCheckerFactory>();
            this.Bind<INetworkParameters>().ToConstant(dogeNetworkParameters);
            ////this.Bind<IChainValidator>().To<BitcoinChainValidator>();
            this.Bind<IChainValidator>().To<DogecoinChainValidator>();
            this.Bind<IBlockChain>().ToConstant(blockChain);
            this.Bind<IChainSerializer>().To<BitcoinChainSerializer>();
            ////this.Bind<IChainParameters>().ToMethod(ctx => new ChainParameters(genesisBlock.Object, HashAlgorithmIdentifiers.DoubleSHA256, HashAlgorithmIdentifiers.DoubleSHA256, HashAlgorithmIdentifiers.RipeMd160, HashAlgorithmIdentifiers.SHA1, HashAlgorithmIdentifiers.SHA256, HashAlgorithmIdentifiers.SHA256ThenRipeMd160, HashAlgorithmIdentifiers.DoubleSHA256, new[] { SecurityMechanism.ProofOfWork }, Duration.FromMinutes(10), 2016, InitialBlockSubsidyInSatoshis, 0.5m, 210000, maximumTarget));
            this.Bind<IChainParameters>().ToMethod(ctx => new ChainParameters(genesisBlock.Object, HashAlgorithmIdentifiers.LitecoinSCrypt, HashAlgorithmIdentifiers.DoubleSHA256, HashAlgorithmIdentifiers.DoubleSHA256, HashAlgorithmIdentifiers.RipeMd160, HashAlgorithmIdentifiers.SHA1, HashAlgorithmIdentifiers.SHA256, HashAlgorithmIdentifiers.SHA256ThenRipeMd160, HashAlgorithmIdentifiers.DoubleSHA256, new[] { SecurityMechanism.ProofOfWork }, Duration.FromMinutes(1), 240, InitialBlockSubsidyInSatoshis, 0.5m, 100000, maximumTarget));
            ////this.Bind<ICurrencyParameters>().ToMethod(ctx => new CurrencyParameters(Guid.NewGuid(), "Bitcoin", ctx.Kernel.Get<INetworkParameters>(), this.hashAlgorithmStore, ctx.Kernel.Get<IChainParameters>(), ctx.Kernel.Get<IChainSerializer>(), ctx.Kernel.Get<IChainValidator>()));
            ////this.Bind<ICurrencyParameters>().ToMethod(ctx => new CurrencyParameters(Guid.NewGuid(), "Testnet3", ctx.Kernel.Get<INetworkParameters>(), this.hashAlgorithmStore, ctx.Kernel.Get<IChainParameters>(), ctx.Kernel.Get<IChainSerializer>(), ctx.Kernel.Get<IChainValidator>()));
            this.Bind<ICurrencyParameters>().ToMethod(ctx => new CurrencyParameters(Guid.NewGuid(), "Dogecoin", ctx.Kernel.Get<INetworkParameters>(), this.hashAlgorithmStore, ctx.Kernel.Get<IChainParameters>(), ctx.Kernel.Get<IChainSerializer>(), ctx.Kernel.Get<IChainValidator>()));
        }
    }
}
