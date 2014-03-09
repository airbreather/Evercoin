using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Evercoin.BaseImplementations;

using Org.BouncyCastle.Crypto.Digests;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithmStore"/> that holds instance of all the
    /// <see cref="IHashAlgorithm"/>s that we have built-in handling for.
    /// This implementation is read-only.
    /// </summary>
    [Export(typeof(IHashAlgorithmStore))]
    public sealed class BuiltinHashAlgorithmStore : HashAlgorithmStoreBase
    {
        /// <summary>
        /// Backing store for the algorithms we use.
        /// </summary>
        private readonly ImmutableDictionary<Guid, IHashAlgorithm> algorithms;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuiltinHashAlgorithmStore"/> class.
        /// </summary>
        public BuiltinHashAlgorithmStore()
        {
            DigestBasedHashAlgorithm sha1 = new DigestBasedHashAlgorithm(new Sha1Digest());
            DigestBasedHashAlgorithm sha256 = new DigestBasedHashAlgorithm(new Sha256Digest());
            DigestBasedHashAlgorithm ripemd160 = new DigestBasedHashAlgorithm(new RipeMD160Digest());

            var algorithmBuilder = ImmutableDictionary.CreateBuilder<Guid, IHashAlgorithm>();
            algorithmBuilder.Add(HashAlgorithmIdentifiers.SHA1, sha1);
            algorithmBuilder.Add(HashAlgorithmIdentifiers.SHA256, sha256);
            algorithmBuilder.Add(HashAlgorithmIdentifiers.RipeMd160, ripemd160);
            algorithmBuilder.Add(HashAlgorithmIdentifiers.DoubleSHA256, new ChainedHashAlgorithm(sha256, sha256));
            algorithmBuilder.Add(HashAlgorithmIdentifiers.SHA256ThenRipeMd160, new ChainedHashAlgorithm(sha256, ripemd160));
            algorithmBuilder.Add(HashAlgorithmIdentifiers.LitecoinSCrypt, new LitecoinSCryptHashAlgorithm());
            this.algorithms = algorithmBuilder.ToImmutable();
        }

        public override bool TryGetHashAlgorithm(Guid identifier, out IHashAlgorithm algorithm)
        {
            return this.algorithms.TryGetValue(identifier, out algorithm);
        }
    }
}
