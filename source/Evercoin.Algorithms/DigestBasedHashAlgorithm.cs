using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Org.BouncyCastle.Crypto;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithm"/> that's implemented
    /// using an <see cref="IDigest"/>.
    /// </summary>
    internal sealed class DigestBasedHashAlgorithm : IHashAlgorithm
    {
        /// <summary>
        /// The <see cref="IDigest"/> used by this algorithm.
        /// </summary>
        private readonly IDigest digest;

        /// <summary>
        /// An object to lock on for thread synchronization.
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestBasedHashAlgorithm"/> class.
        /// </summary>
        /// <param name="digest">
        /// The <see cref="IDigest"/> that provides 
        /// </param>
        public DigestBasedHashAlgorithm(IDigest digest)
        {
            if (digest == null)
            {
                throw new ArgumentNullException("digest");
            }

            this.digest = digest;
        }

        /// <summary>
        /// Calculates the hash of a sequence of bytes.
        /// </summary>
        /// <param name="inputData">
        /// The input data to hash.
        /// </param>
        /// <returns>
        /// The hash result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputData"/> is <c>null</c>.
        /// </exception>
        public IImmutableList<byte> CalculateHash(IEnumerable<byte> inputData)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException("inputData");
            }

            byte[] result;
            lock (this.syncLock)
            {
                int resultLength = this.digest.GetDigestSize();
                result = new byte[resultLength];

                foreach (byte b in inputData)
                {
                    digest.Update(b);
                }

                digest.DoFinal(result, 0);
            }

            return result.ToImmutableList();
        }
    }
}
