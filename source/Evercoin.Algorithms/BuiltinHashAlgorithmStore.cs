using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly ReadOnlyDictionary<Guid, IHashAlgorithm> algorithms;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuiltinHashAlgorithmStore"/> class.
        /// </summary>
        public BuiltinHashAlgorithmStore()
        {
            DigestBasedHashAlgorithm sha1 = new DigestBasedHashAlgorithm(new Sha1Digest());
            DigestBasedHashAlgorithm sha256 = new DigestBasedHashAlgorithm(new Sha256Digest());
            DigestBasedHashAlgorithm ripemd160 = new DigestBasedHashAlgorithm(new RipeMD160Digest());

            var algorithmBuilder = new Dictionary<Guid, IHashAlgorithm>
                                   {
                                       { HashAlgorithmIdentifiers.SHA1, sha1 },
                                       { HashAlgorithmIdentifiers.SHA256, sha256 },
                                       { HashAlgorithmIdentifiers.RipeMd160, ripemd160 },
                                       { HashAlgorithmIdentifiers.DoubleSHA256, new ChainedHashAlgorithm(sha256, sha256) },
                                       { HashAlgorithmIdentifiers.SHA256ThenRipeMd160, new ChainedHashAlgorithm(sha256, ripemd160) },
                                       { HashAlgorithmIdentifiers.LitecoinSCrypt, new LitecoinSCryptHashAlgorithm() },
                                   };
            this.algorithms = new ReadOnlyDictionary<Guid, IHashAlgorithm>(algorithmBuilder);
        }

        public override bool TryGetHashAlgorithm(Guid identifier, out IHashAlgorithm algorithm)
        {
            return this.algorithms.TryGetValue(identifier, out algorithm);
        }
    }
}
