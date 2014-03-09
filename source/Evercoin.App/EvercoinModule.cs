using Evercoin.Algorithms;
using Evercoin.TransactionScript;

using Ninject.Modules;

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
            this.Bind<IReadOnlyChainStore>().ToConstant(this.underlyingChainStorage);
            this.Bind<IChainStore>().ToConstant(this.underlyingChainStorage);
            this.Bind<INetwork>().To<Network.Network>().InSingletonScope();
            this.Bind<IHashAlgorithmStore>().ToConstant(this.hashAlgorithmStore);
            this.Bind<ITransactionScriptParser>().To<TransactionScriptParser>();
            this.Bind<ITransactionScriptRunner>().To<TransactionScriptRunner>();
            this.Bind<ISignatureCheckerFactory>().To<ECDSASignatureCheckerFactory>();
            this.Bind<INetworkParameters>().To<SomeNetworkParams>();
        }
    }
}
