using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NodaTime;

namespace Evercoin
{
    /// <summary>
    /// Represents the parameters for a cryptocurrency network.
    /// </summary>
    public interface IChainParameters
    {
        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use for calculating proof-of-work.
        /// </summary>
        /// <remarks>
        /// It's likely that this will be one of the values from
        /// <see cref="HashAlgorithmIdentifiers"/>, though algorithms that
        /// aren't built-in could be used by implementing a custom
        /// <see cref="IHashAlgorithmStore"/>.
        /// </remarks>
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
        /// Gets the desired <see cref="Interval"/> of time between block.
        /// </summary>
        Interval DesiredTimeBetweenBlocks { get; }

        /// <summary>
        /// Gets how many block to wait before recalculating the minimum
        /// difficulty target for new blocks.
        /// </summary>
        uint BlocksPerDifficultyRetarget { get; }

        /// <summary>
        /// Gets the value for the block subsidy on a block chain
        /// at the time of the genesis block.
        /// </summary>
        decimal InitialSubsidyLevel { get; }

        /// <summary>
        /// Gets the multiplier to apply to the previous block subsidy level
        /// to get the next subsidy level.
        /// </summary>
        /// <remarks>
        /// At time of writing, most cryptocurrency networks use 0.5 here.
        /// </remarks>
        decimal SubsidyLevelMultiplier { get; }

        /// <summary>
        /// Gets the number of blocks that need to appear on a chain
        /// before each time that the subsidy is halved.
        /// </summary>
        /// <remarks>
        /// The genesis block counts as one.
        /// Example: suppose that this value is 3, initial subsidy is 40, and
        /// each subsidy level is half of the previous one.
        /// Then the subsidies for the first few blocks in the chain will be:
        /// 40 (genesis block)
        /// 40 (first mined block)
        /// 40 (second mined block)
        /// 20 (third mined block)
        /// 20 (fourth mined block)
        /// 20 (fifth mined block)
        /// 10 (sixth mined block)
        /// ...and so on.
        /// </remarks>
        ulong BlocksAtEachSubsidyLevel { get; }
    }
}
