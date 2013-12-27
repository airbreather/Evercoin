using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Digests;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithm"/> that's implemented by running SHA256 twice.
    /// </summary>
    public sealed class DoubleSHA256HashAlgorithm : IHashAlgorithm
    {
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
        public byte[] CalculateHash(IEnumerable<byte> inputData)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException("inputData");
            }
            
            Sha256Digest digest = new Sha256Digest();

            // Round 1 start
            foreach (byte b in inputData)
            {
                digest.Update(b);
            }

            // Round 1 end
            byte[] result = new byte[32];
            digest.DoFinal(result, 0);

            // Round 2 start
            digest.BlockUpdate(result, 0, 32);

            // Round 2 end
            digest.DoFinal(result, 0);

            return result;
        }
    }
}
