using System.ComponentModel.Composition;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// 
    /// </summary>
    [Export(typeof(ISignatureCheckerFactory))]
    public sealed class ECDSASignatureCheckerFactory : ISignatureCheckerFactory
    {
        private readonly IHashAlgorithmStore hashAlgorithmStore;

        [ImportingConstructor]
        public ECDSASignatureCheckerFactory(IHashAlgorithmStore hashAlgorithmStore)
        {
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public ISignatureChecker CreateSignatureChecker(ITransaction transaction, int outputIndex)
        {
            return new ECDSASignatureChecker(hashAlgorithmStore.GetHashAlgorithm(HashAlgorithmIdentifiers.DoubleSHA256), transaction, outputIndex);
        }
    }
}
