using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Evercoin.BaseImplementations;

using Org.BouncyCastle.Crypto.Digests;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithmStore"/> that holds instance of all the
    /// <see cref="IHashAlgorithm"/>s that we have built-in handling for.
    /// This implementation is read-only.
    /// </summary>
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

        /// <summary>
        /// Gets the <see cref="IHashAlgorithm"/> identified by a given
        /// <see cref="Guid"/>.
        /// </summary>
        /// <param name="identifier">
        /// The <see cref="Guid"/> that identifies the
        /// <see cref="IHashAlgorithm"/> value to get.
        /// </param>
        /// <returns>
        /// The <see cref="IHashAlgorithm"/> identified by
        /// <paramref name="identifier"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="identifier"/> does not map to an algorithm
        /// that we know about.
        /// </exception>
        public override IHashAlgorithm GetHashAlgorithm(Guid identifier)
        {
            return this.algorithms[identifier];
        }
    }
}
