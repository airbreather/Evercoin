using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using CryptSharp.Utility;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithm"/> that's implemented by running SCrypt,
    /// with the parameters used by the Litecoin network (N=1024, r=1, p=1).
    /// </summary>
    internal sealed class LitecoinSCryptHashAlgorithm : IHashAlgorithm
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
        public ImmutableList<byte> CalculateHash(IEnumerable<byte> inputData)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException("inputData");
            }

            byte[] inArray = inputData.ToArray();
            byte[] outArray = new byte[32];
            SCrypt.ComputeKey(inArray, inArray, 1024, 1, 1, null, outArray);
            return outArray.ToImmutableList();
        }
    }
}
