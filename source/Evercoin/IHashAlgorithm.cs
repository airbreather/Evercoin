using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents an algorithm that can be used to calculate hashes, often
    /// used for getting proof-of-work.
    /// </summary>
    public interface IHashAlgorithm
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
        byte[] CalculateHash(IEnumerable<byte> inputData);
    }
}
