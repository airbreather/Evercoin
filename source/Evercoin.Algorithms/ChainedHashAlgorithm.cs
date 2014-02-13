using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// An <see cref="IHashAlgorithm"/> implementation that just uses multiple
    /// other <see cref="IHashAlgorithm"/>s in a row.
    /// </summary>
    internal sealed class ChainedHashAlgorithm : IHashAlgorithm
    {
        /// <summary>
        /// The <see cref="IHashAlgorithm"/>s to use, in order.
        /// </summary>
        private readonly IImmutableList<IHashAlgorithm> algorithms;

        /// <summary>
        /// Initializes a new instance of the <see cref="IHashAlgorithm"/> class.
        /// </summary>
        /// <param name="algorithms">
        /// The <see cref="IHashAlgorithm"/> objects to use, in sequence.
        /// </param>
        public ChainedHashAlgorithm(params IHashAlgorithm[] algorithms)
            : this(algorithms.ToImmutableList())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IHashAlgorithm"/> class.
        /// </summary>
        /// <param name="algorithms">
        /// The <see cref="IHashAlgorithm"/> objects to use, in sequence.
        /// </param>
        public ChainedHashAlgorithm(IImmutableList<IHashAlgorithm> algorithms)
        {
            if (algorithms == null)
            {
                throw new ArgumentNullException("algorithms");
            }

            if (algorithms.Count == 0)
            {
                throw new ArgumentException("Must provide at least one algorithm", "algorithms");
            }

            this.algorithms = algorithms;
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
        public byte[] CalculateHash(IEnumerable<byte> inputData)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException("inputData");
            }

            return (byte[])this.algorithms.Aggregate(inputData, (lastResult, algorithm) => algorithm.CalculateHash(lastResult));
        }
    }
}
