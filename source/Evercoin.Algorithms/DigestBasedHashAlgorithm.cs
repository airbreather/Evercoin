using System;
using System.Collections.Generic;
using System.Threading;

using Org.BouncyCastle.Crypto;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithm"/> that's implemented
    /// using an <see cref="IDigest"/>.
    /// </summary>
    internal sealed class DigestBasedHashAlgorithm : DisposableObject, IHashAlgorithm
    {
        /// <summary>
        /// The <see cref="IDigest"/> used by this algorithm.
        /// </summary>
        private readonly ThreadLocal<IDigest> digestFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestBasedHashAlgorithm"/> class.
        /// </summary>
        /// <param name="digestFactory">
        /// A delegate that can produce the <see cref="IDigest"/> per thread.
        /// </param>
        public DigestBasedHashAlgorithm(Func<IDigest> digestFactory)
        {
            if (digestFactory == null)
            {
                throw new ArgumentNullException("digestFactory");
            }

            this.digestFactory = new ThreadLocal<IDigest>(digestFactory);
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
        public FancyByteArray CalculateHash(IEnumerable<byte> inputData)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException("inputData");
            }

            IDigest threadLocalDigest = this.digestFactory.Value;
            int resultLength = threadLocalDigest.GetDigestSize();
            byte[] result = new byte[resultLength];

            foreach (byte b in inputData)
            {
                threadLocalDigest.Update(b);
            }

            threadLocalDigest.DoFinal(result, 0);

            return result;
        }

        protected override void DisposeManagedResources()
        {
            this.digestFactory.Dispose();
            base.DisposeManagedResources();
        }
    }
}
