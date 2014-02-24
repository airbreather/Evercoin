﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network
{
    internal sealed class NetworkBlock : IBlock
    {
        private readonly List<NetworkTransaction> transactions = new List<NetworkTransaction>();

        private readonly List<byte> difficultyTargetBytes = new List<byte>(); 

        public NetworkBlock()
        {
        }

        public NetworkBlock(IBlock block)
        {
            this.InitFrom(block);
        }

        public string Identifier { get; set; }

        /// <summary>
        /// Gets the ordered list of <see cref="ITransaction"/> objects
        /// contained within this block.
        /// </summary>
        public IList<NetworkTransaction> Transactions { get { return this.transactions; } }

        /// <summary>
        /// Gets the version of this block.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets the <see cref="NodaTime.Instant"/> in time when this block was created.
        /// </summary>
        public Instant Timestamp { get; set; }

        /// <summary>
        /// Gets the nonce for this block.
        /// </summary>
        public uint Nonce { get; set; }

        /// <summary>
        /// Gets the <see cref="IValueSource"/> that represents the reward
        /// for mining this block.
        /// </summary>
        public NetworkValueSource Coinbase { get; set; }

        /// <summary>
        /// Gets the difficulty target being used for this block.
        /// </summary>
        public IList<byte> DifficultyTargetBytes { get { return this.difficultyTargetBytes; } }

        /// <summary>
        /// Gets how high this block is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many nodes come before this one.
        /// So, the genesis block is at height zero.
        /// </remarks>
        public ulong Height { get; set; }

        /// <summary>
        /// Gets the previous block in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="IBlock.Height"/> equals 0, the return value is undefined.
        /// </remarks>
        public string PreviousBlockIdentifier { get; set; }

        public BigInteger DifficultyTarget
        {
            get { return new BigInteger(this.difficultyTargetBytes.ToArray()); }
            set { this.difficultyTargetBytes.Clear(); this.difficultyTargetBytes.AddRange(value.ToByteArray()); }
        }

        IValueSource IBlock.Coinbase { get { return this.Coinbase; } }

        IImmutableList<ITransaction> IBlock.Transactions { get { return ImmutableList.CreateRange<ITransaction>(this.transactions); } }

        private static NetworkValueSource CreateSerializableValueSource(IValueSource valueSource)
        {
            NetworkValueSource networkValueSource = valueSource as NetworkValueSource;
            return networkValueSource ?? new NetworkValueSource(valueSource);
        }
        private static NetworkTransaction CreateSerializableTransaction(ITransaction transaction)
        {
            NetworkTransaction serializableTransaction = transaction as NetworkTransaction;
            return serializableTransaction ?? new NetworkTransaction(transaction);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IBlock other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.Identifier == other.Identifier &&
                   this.Height == other.Height &&
                   this.PreviousBlockIdentifier == other.PreviousBlockIdentifier &&
                   this.Nonce == other.Nonce &&
                   this.Version == other.Version &&
                   this.Timestamp == other.Timestamp &&
                   this.DifficultyTarget == other.DifficultyTarget &&
                   this.Transactions.SequenceEqual(other.Transactions) &&
                   Equals(this.Coinbase, other.Coinbase);
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
            return this.Equals(obj as IBlock);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return new HashCodeBuilder()
                .HashWith(this.Identifier)
                .HashWith(this.Nonce)
                .HashWith(this.PreviousBlockIdentifier)
                .HashWith(this.Version)
                .HashWith(this.Height)
                .HashWith(this.Timestamp)
                .HashWith(this.Transactions.Count)
                .HashWith(this.Coinbase);
        }

        private void InitFrom(IBlock block)
        {
            this.Identifier = block.Identifier;
            this.Version = block.Version;
            this.Timestamp = block.Timestamp;
            this.Nonce = block.Nonce;
            this.Height = block.Height;

            NetworkBlock other = block as NetworkBlock;
            if (other != null)
            {
                this.transactions.AddRange(other.Transactions);
                this.Coinbase = other.Coinbase;
                this.difficultyTargetBytes.AddRange(other.difficultyTargetBytes);
            }
            else
            {
                this.transactions.AddRange(block.Transactions.Select(CreateSerializableTransaction));
                this.Coinbase = CreateSerializableValueSource(block.Coinbase);
                this.DifficultyTarget = block.DifficultyTarget;
            }
        }
    }
}
