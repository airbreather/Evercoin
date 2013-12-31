﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Digests;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithmStore"/> that holds instance of all the
    /// <see cref="IHashAlgorithm"/>s that we have built-in handling for.
    /// This implementation is read-only.
    /// </summary>
    public sealed class BuiltinHashAlgorithmStore : IHashAlgorithmStore
    {
        /// <summary>
        /// Backing store for the algorithms we use.
        /// </summary>
        private readonly IReadOnlyDictionary<Guid, IHashAlgorithm> algorithms;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuiltinHashAlgorithmStore"/> class.
        /// </summary>
        public BuiltinHashAlgorithmStore()
        {
            DigestBasedHashAlgorithm sha1 = new DigestBasedHashAlgorithm(new Sha1Digest());
            DigestBasedHashAlgorithm sha256 = new DigestBasedHashAlgorithm(new Sha256Digest());
            DigestBasedHashAlgorithm ripemd160 = new DigestBasedHashAlgorithm(new RipeMD160Digest());

            var mapping = new Dictionary<Guid, IHashAlgorithm>
            {
                { HashAlgorithmIdentifiers.SHA1, sha1 },
                { HashAlgorithmIdentifiers.SHA256, sha256 },
                { HashAlgorithmIdentifiers.RipeMd160, ripemd160 },
                { HashAlgorithmIdentifiers.DoubleSHA256, new ChainedHashAlgorithm(sha256, sha256) },
                { HashAlgorithmIdentifiers.SHA256ThenRipeMd160, new ChainedHashAlgorithm(sha256, ripemd160) },
                { HashAlgorithmIdentifiers.LitecoinSCrypt, new LitecoinSCryptHashAlgorithm() }
            };

            this.algorithms = new ReadOnlyDictionary<Guid, IHashAlgorithm>(mapping);
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
        public IHashAlgorithm GetHashAlgorithm(Guid identifier)
        {
            return this.algorithms[identifier];
        }

        /// <summary>
        /// Registers a new <see cref="IHashAlgorithm"/>.
        /// </summary>
        /// <param name="identifier">
        /// A <see cref="Guid"/> value that can be used to retrieve the
        /// registered <see cref="IHashAlgorithm"/> in the future.
        /// </param>
        /// <param name="algorithm">
        /// The <see cref="IHashAlgorithm"/> to register.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// This store does not support registering new algorithms.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="algorithm"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="identifier"/> already identifies another
        /// <see cref="IHashAlgorithm"/> in this store that is not
        /// equal to <paramref name="algorithm"/>.
        /// </exception>
        /// <remarks>
        /// This is expected to be used rarely, for registering the algorithms
        /// for cryptocurrencies that Evercoin does not know about.
        /// </remarks>
        void IHashAlgorithmStore.RegisterHashAlgorithm(Guid identifier, IHashAlgorithm algorithm)
        {
            throw new NotSupportedException();
        }
    }
}
