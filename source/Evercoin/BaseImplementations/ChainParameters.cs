using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.BaseImplementations
{
    public sealed class ChainParameters : IChainParameters
    {
        private readonly IBlock genesisBlock;

        private readonly Guid blockHashAlgorithmIdentifier;

        private readonly Guid transactionHashAlgorithmIdentifier;

        private readonly Guid scriptHashAlgorithmIdentifier1;

        private readonly Guid scriptHashAlgorithmIdentifier2;

        private readonly Guid scriptHashAlgorithmIdentifier3;

        private readonly Guid scriptHashAlgorithmIdentifier4;

        private readonly Guid scriptHashAlgorithmIdentifier5;

        private readonly HashSet<SecurityMechanism> securityMechanisms;

        private readonly Duration desiredTimeBetweenBlocks;

        private readonly uint blocksPerDifficultyRetarget;

        private readonly decimal initialSubsidyLevel;

        private readonly decimal subsidyLevelMultiplier;

        private readonly ulong blocksAtEachSubsidyLevel;

        private readonly BigInteger maximumDifficultyTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainParameters"/> class.
        /// </summary>
        /// <param name="genesisBlock">
        /// The value for <see cref="GenesisBlock"/>.
        /// </param>
        /// <param name="blockHashAlgorithmIdentifier">
        /// The value for <see cref="BlockHashAlgorithmIdentifier"/>.
        /// </param>
        /// <param name="transactionHashAlgorithmIdentifier">
        /// The value for <see cref="TransactionHashAlgorithmIdentifier"/>.
        /// </param>
        /// <param name="scriptHashAlgorithmIdentifier1">
        /// The value for <see cref="ScriptHashAlgorithmIdentifier1"/>.
        /// </param>
        /// <param name="scriptHashAlgorithmIdentifier2">
        /// The value for <see cref="ScriptHashAlgorithmIdentifier2"/>.
        /// </param>
        /// <param name="scriptHashAlgorithmIdentifier3">
        /// The value for <see cref="ScriptHashAlgorithmIdentifier3"/>.
        /// </param>
        /// <param name="scriptHashAlgorithmIdentifier4">
        /// The value for <see cref="ScriptHashAlgorithmIdentifier4"/>.
        /// </param>
        /// <param name="scriptHashAlgorithmIdentifier5">
        /// The value for <see cref="ScriptHashAlgorithmIdentifier5"/>.
        /// </param>
        /// <param name="securityMechanisms">
        /// The values for <see cref="SecurityMechanisms"/>.
        /// </param>
        /// <param name="desiredTimeBetweenBlocks">
        /// The value for <see cref="DesiredTimeBetweenBlocks"/>.
        /// </param>
        /// <param name="blocksPerDifficultyRetarget">
        /// The value for <see cref="BlocksPerDifficultyRetarget"/>.
        /// </param>
        /// <param name="initialSubsidyLevel">
        /// The value for <see cref="InitialSubsidyLevel"/>.
        /// </param>
        /// <param name="subsidyLevelMultiplier">
        /// The value for <see cref="SubsidyLevelMultiplier"/>.
        /// </param>
        /// <param name="blocksAtEachSubsidyLevel">
        /// The value for <see cref="BlocksAtEachSubsidyLevel"/>.
        /// </param>
        /// <param name="maximumDifficultyTarget">
        /// The value for <see cref="MaximumDifficultyTarget"/>.
        /// </param>
        public ChainParameters(IBlock genesisBlock,
                               Guid blockHashAlgorithmIdentifier,
                               Guid transactionHashAlgorithmIdentifier,
                               Guid scriptHashAlgorithmIdentifier1,
                               Guid scriptHashAlgorithmIdentifier2,
                               Guid scriptHashAlgorithmIdentifier3,
                               Guid scriptHashAlgorithmIdentifier4,
                               Guid scriptHashAlgorithmIdentifier5,
                               IEnumerable<SecurityMechanism> securityMechanisms,
                               Duration desiredTimeBetweenBlocks,
                               uint blocksPerDifficultyRetarget,
                               decimal initialSubsidyLevel,
                               decimal subsidyLevelMultiplier,
                               ulong blocksAtEachSubsidyLevel,
                               BigInteger maximumDifficultyTarget)
        {
            this.genesisBlock = genesisBlock;
            this.blockHashAlgorithmIdentifier = blockHashAlgorithmIdentifier;
            this.transactionHashAlgorithmIdentifier = transactionHashAlgorithmIdentifier;
            this.scriptHashAlgorithmIdentifier1 = scriptHashAlgorithmIdentifier1;
            this.scriptHashAlgorithmIdentifier2 = scriptHashAlgorithmIdentifier2;
            this.scriptHashAlgorithmIdentifier3 = scriptHashAlgorithmIdentifier3;
            this.scriptHashAlgorithmIdentifier4 = scriptHashAlgorithmIdentifier4;
            this.scriptHashAlgorithmIdentifier5 = scriptHashAlgorithmIdentifier5;
            this.securityMechanisms = new HashSet<SecurityMechanism>(securityMechanisms);
            this.desiredTimeBetweenBlocks = desiredTimeBetweenBlocks;
            this.blocksPerDifficultyRetarget = blocksPerDifficultyRetarget;
            this.initialSubsidyLevel = initialSubsidyLevel;
            this.subsidyLevelMultiplier = subsidyLevelMultiplier;
            this.blocksAtEachSubsidyLevel = blocksAtEachSubsidyLevel;
            this.maximumDifficultyTarget = maximumDifficultyTarget;
        }

        /// <summary>
        /// Gets the <see cref="IBlock"/> at height 0.
        /// </summary>
        public IBlock GenesisBlock { get { return this.genesisBlock; } }

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
        public Guid BlockHashAlgorithmIdentifier { get { return this.blockHashAlgorithmIdentifier; } }

        public Guid TransactionHashAlgorithmIdentifier { get { return this.transactionHashAlgorithmIdentifier; } }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 1 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is RIPEMD-160.
        /// </remarks>
        public Guid ScriptHashAlgorithmIdentifier1 { get { return this.scriptHashAlgorithmIdentifier1; } }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 2 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-1.
        /// </remarks>
        public Guid ScriptHashAlgorithmIdentifier2 { get { return this.scriptHashAlgorithmIdentifier2; } }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 3 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-256.
        /// </remarks>
        public Guid ScriptHashAlgorithmIdentifier3 { get { return this.scriptHashAlgorithmIdentifier3; } }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 4 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-256 followed by RIPEMD-160.
        /// </remarks>
        public Guid ScriptHashAlgorithmIdentifier4 { get { return this.scriptHashAlgorithmIdentifier4; } }

        /// <summary>
        /// Gets the <see cref="Guid"/> that identifies which
        /// <see cref="IHashAlgorithm"/> to use as algorithm 5 in transaction
        /// scripts.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is SHA-256 followed by another round of SHA-256.
        /// </remarks>
        public Guid ScriptHashAlgorithmIdentifier5 { get { return this.scriptHashAlgorithmIdentifier5; } }

        /// <summary>
        /// Gets the set of <see cref="SecurityMechanism"/>s that this network
        /// uses to secure itself against untrusted entities.
        /// </summary>
        /// <remarks>
        /// Currently, only <see cref="SecurityMechanism.ProofOfWork"/> is
        /// supported in Evercoin.
        /// </remarks>
        public ISet<SecurityMechanism> SecurityMechanisms { get { return new HashSet<SecurityMechanism>(this.securityMechanisms); } }

        /// <summary>
        /// Gets the desired <see cref="Duration"/> of time between block.
        /// </summary>
        /// <remarks>
        /// This is more than just a description of the ideal.  At every
        /// retarget interval, subsequent blocks must meet a difficulty target
        /// based on the difference between expected and observed durations.
        /// </remarks>
        public Duration DesiredTimeBetweenBlocks { get { return this.desiredTimeBetweenBlocks; } }

        /// <summary>
        /// Gets how many block to wait before recalculating the minimum
        /// difficulty target for new blocks.
        /// </summary>
        public uint BlocksPerDifficultyRetarget { get { return this.blocksPerDifficultyRetarget; } }

        /// <summary>
        /// Gets the value for the block subsidy on a block chain
        /// at the time of the genesis block.
        /// </summary>
        public decimal InitialSubsidyLevel { get { return this.initialSubsidyLevel; } }

        /// <summary>
        /// Gets the multiplier to apply to the previous block subsidy level
        /// to get the next subsidy level.
        /// </summary>
        /// <remarks>
        /// At time of writing, most cryptocurrency networks use 0.5 here.
        /// </remarks>
        public decimal SubsidyLevelMultiplier { get { return this.subsidyLevelMultiplier; } }

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
        public ulong BlocksAtEachSubsidyLevel { get { return this.blocksAtEachSubsidyLevel; } }

        /// <summary>
        /// Gets the maximum difficulty target to allow for blocks.
        /// </summary>
        /// <remarks>
        /// Maximum target = minimum difficulty.  So a block with this target
        /// can also be equivalently considered "difficulty 1", a block with a
        /// target that's 1/4 of this value would be considered "difficulty 4",
        /// and so on.
        /// </remarks>
        public BigInteger MaximumDifficultyTarget { get { return this.maximumDifficultyTarget; } }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IChainParameters other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.InitialSubsidyLevel == other.InitialSubsidyLevel &&
                   this.DesiredTimeBetweenBlocks == other.DesiredTimeBetweenBlocks &&
                   this.BlocksAtEachSubsidyLevel == other.BlocksAtEachSubsidyLevel &&
                   this.MaximumDifficultyTarget == other.MaximumDifficultyTarget &&
                   this.BlocksPerDifficultyRetarget == other.BlocksPerDifficultyRetarget &&
                   this.BlockHashAlgorithmIdentifier == other.BlockHashAlgorithmIdentifier &&
                   this.SubsidyLevelMultiplier == other.SubsidyLevelMultiplier &&
                   this.ScriptHashAlgorithmIdentifier1 == other.ScriptHashAlgorithmIdentifier1 &&
                   this.ScriptHashAlgorithmIdentifier2 == other.ScriptHashAlgorithmIdentifier2 &&
                   this.ScriptHashAlgorithmIdentifier3 == other.ScriptHashAlgorithmIdentifier3 &&
                   this.ScriptHashAlgorithmIdentifier4 == other.ScriptHashAlgorithmIdentifier4 &&
                   this.ScriptHashAlgorithmIdentifier5 == other.ScriptHashAlgorithmIdentifier5 &&
                   this.SecurityMechanisms.SetEquals(other.SecurityMechanisms);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ChainParameters);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWith(this.InitialSubsidyLevel)
                .HashWith(this.DesiredTimeBetweenBlocks)
                .HashWith(this.BlocksAtEachSubsidyLevel)
                .HashWith(this.MaximumDifficultyTarget)
                .HashWith(this.BlocksPerDifficultyRetarget)
                .HashWith(this.BlockHashAlgorithmIdentifier)
                .HashWith(this.SubsidyLevelMultiplier)
                .HashWith(this.ScriptHashAlgorithmIdentifier1)
                .HashWith(this.ScriptHashAlgorithmIdentifier2)
                .HashWith(this.ScriptHashAlgorithmIdentifier3)
                .HashWith(this.ScriptHashAlgorithmIdentifier4)
                .HashWith(this.ScriptHashAlgorithmIdentifier5)
                .HashWithEnumerable(this.securityMechanisms.OrderBy(x => x));

            return builder;
        }
    }
}
