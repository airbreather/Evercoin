using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// An algorithm that calculates the deterministic hash of an input stream.
    /// </summary>
    public interface IHashAlgorithm
    {
        /// <summary>
        /// Calculates the hash of an input <see cref="Stream"/>.
        /// </summary>
        /// <param name="inputData">
        /// A <see cref="Stream"/> containing the data to hash.
        /// </param>
        /// <returns>
        /// A <see cref="Stream"/> containing the result of hashing the input data.
        /// </returns>
        Stream CalculateHash(Stream inputData);
    }
}
