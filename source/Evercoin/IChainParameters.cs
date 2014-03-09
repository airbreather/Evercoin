using System;
using System.Collections.Immutable;
using System.Numerics;

using NodaTime;

namespace Evercoin
{
    /// <summary>
    /// Represents the parameters for a cryptocurrency blockchain.
    /// Changes to any of these parameters definitely result in a hardfork.
    /// </summary>
    public interface IChainParameters : IEquatable<IChainParameters>
    {
        /// <summary>
        /// Gets the <see cref="IBlock"/> at height 0.
        /// </summary>
        IBlock GenesisBlock { get; }

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
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 1 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is RIPEMD-160.
        /// </remarks>
        Guid ScriptHashAlgorithmIdentifier1 { get; }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 2 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-1.
        /// </remarks>
        Guid ScriptHashAlgorithmIdentifier2 { get; }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 3 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-256.
        /// </remarks>
        Guid ScriptHashAlgorithmIdentifier3 { get; }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 4 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-256 followed by RIPEMD-160.
        /// </remarks>
        Guid ScriptHashAlgorithmIdentifier4 { get; }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 5 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-256 followed by another round of SHA-256.
        /// </remarks>
        Guid ScriptHashAlgorithmIdentifier5 { get; }

        /// <summary>
        /// Gets the set of <see cref="SecurityMechanism"/>s that this network
        /// uses to secure itself against untrusted entities.
        /// </summary>
        /// <remarks>
        /// Currently, only <see cref="SecurityMechanism.ProofOfWork"/> is
        /// supported in Evercoin.
        /// </remarks>
        ImmutableHashSet<SecurityMechanism> SecurityMechanisms { get; }

        /// <summary>
        /// Gets the desired <see cref="Duration"/> of time between block.
        /// </summary>
        /// <remarks>
        /// This is more than just a description of the ideal.  At every
        /// retarget interval, subsequent blocks must meet a difficulty target
        /// based on the difference between expected and observed durations.
        /// </remarks>
        Duration DesiredTimeBetweenBlocks { get; }

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

        /// <summary>
        /// Gets the maximum difficulty target to allow for blocks.
        /// </summary>
        /// <remarks>
        /// Maximum target = minimum difficulty.  So a block with this target
        /// can also be equivalently considered "difficulty 1", a block with a
        /// target that's 1/4 of this value would be considered "difficulty 4",
        /// and so on.
        /// </remarks>
        BigInteger MaximumDifficultyTarget { get; }

        /// <summary>
        /// Gets the set of legacy behaviors to emulate, mapped to the highest
        /// block in the chain to stop emulating that behavior.
        /// </summary>
        /// <remarks>
        /// Legacy behaviors are unintuitive quirks present in released
        /// versions of the reference implementations that need to be
        /// emulated in order for any client to be compatible.
        /// </remarks>
        ImmutableDictionary<LegacyBehavior, long> LegacyBehaviorsToEmulate { get; }
    }
}
