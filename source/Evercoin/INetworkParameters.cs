using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NodaTime;

namespace Evercoin
{
    /// <summary>
    /// Represents the parameters for a cryptocurrency network.
    /// </summary>
    public interface INetworkParameters
    {
        /// <summary>
        /// Gets the magic number that begins messages for this network.
        /// </summary>
        uint MessageMagicNumber { get; }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use for calculating proof-of-work.
        /// </summary>
        Guid ProofOfWorkAlgorithmIdentifier { get; }

        /// <summary>
        /// Gets the set of <see cref="SecurityMechanism"/>s that this network
        /// uses to secure itself against untrusted entities.
        /// </summary>
        /// <remarks>
        /// Currently, only <see cref="SecurityMechanism.ProofOfWork"/> is
        /// supported.
        /// </remarks>
        ISet<SecurityMechanism> SecurityMechanisms { get; }

        /// <summary>
        /// Gets the desired <see cref="Interval"/> of time between blocks.
        /// </summary>
        Interval TargetBlockInterval { get; }

        /// <summary>
        /// Gets how many blocks to wait before recalculating the minimum
        /// difficulty target for new blocks.
        /// </summary>
        uint BlocksPerDifficultyRetarget { get; }
    }
}
