using System;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

using NodaTime;

namespace Evercoin.Storage.Model
{
    [Serializable]
    internal sealed class Block : IBlock, ISerializable
    {
        private const string SerializationName_Identifier = "Identifier";
        private const string SerializationName_PreviousBlockIdentifier = "PreviousBlockIdentifier";
        private const string SerializationName_Height = "Height";
        private const string SerializationName_Timestamp = "Timestamp";
        private const string SerializationName_Nonce = "Nonce";
        private const string SerializationName_DifficultyTarget = "DifficultyTarget";
        private const string SerializationName_TransactionIdentifiers = "TransactionIdentifiers";
        private const string SerializationName_Version = "Version";
        private const string SerializationName_TypedCoinbase = "TypedCoinbase";

        public Block()
        {
        }

        public Block(IBlock copyFrom)
            : this()
        {
            this.Identifier = copyFrom.Identifier;
            this.PreviousBlockIdentifier = copyFrom.PreviousBlockIdentifier;
            this.Height = copyFrom.Height;
            this.Timestamp = copyFrom.Timestamp;
            this.Nonce = copyFrom.Nonce;
            this.DifficultyTarget = copyFrom.DifficultyTarget;
            this.TransactionIdentifiers = new MerkleTreeNode(copyFrom.TransactionIdentifiers);
            this.Version = copyFrom.Version;
            this.TypedCoinbase = new CoinbaseValueSource(copyFrom.Coinbase);
        }

        private Block(SerializationInfo info, StreamingContext context)
        {
            this.Identifier = info.GetValue<BigInteger>(SerializationName_Identifier);
            this.PreviousBlockIdentifier = info.GetValue<BigInteger>(SerializationName_PreviousBlockIdentifier);
            this.Height = info.GetUInt64(SerializationName_Height);
            this.Timestamp = info.GetValue<Instant>(SerializationName_Timestamp);
            this.Nonce = info.GetUInt32(SerializationName_Nonce);
            this.DifficultyTarget = info.GetValue<BigInteger>(SerializationName_DifficultyTarget);
            this.Version = info.GetUInt32(SerializationName_Version);
            this.TypedCoinbase = info.GetValue<CoinbaseValueSource>(SerializationName_TypedCoinbase);
            this.TransactionIdentifiers = info.GetValue<MerkleTreeNode>(SerializationName_TransactionIdentifiers);
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
                   this.PreviousBlockIdentifier == other.PreviousBlockIdentifier &&
                   this.Height == other.Height &&
                   this.Timestamp == other.Timestamp &&
                   this.Nonce == other.Nonce &&
                   this.DifficultyTarget == other.DifficultyTarget &&
                   this.TransactionIdentifiers.Data.SequenceEqual(other.TransactionIdentifiers.Data) &&
                   this.Version == other.Version;
        }

        /// <summary>
        /// Gets an integer that identifies this block.
        /// </summary>
        public BigInteger Identifier { get; set; }

        /// <summary>
        /// Gets the ordered list of the identifiers of
        /// <see cref="ITransaction"/> objects contained within this block.
        /// </summary>
        public MerkleTreeNode TransactionIdentifiers { get; set; }

        IMerkleTreeNode IBlock.TransactionIdentifiers { get { return this.TransactionIdentifiers; } }

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

        public CoinbaseValueSource TypedCoinbase { get; set; }

        public ICoinbaseValueSource Coinbase { get { return this.TypedCoinbase; } }

        public BigInteger DifficultyTarget { get; set; }

        /// <summary>
        /// Gets how high this block is in the chain.
        /// </summary>
        /// <remarks>
        /// In other words, how many nodes come before this one.
        /// So, the genesis block is at height zero.
        /// </remarks>
        public ulong Height { get; set; }

        /// <summary>
        /// Gets the identifier of the previous block in the chain.
        /// </summary>
        /// <remarks>
        /// When <see cref="IBlock.Height"/> equals 0, the return value is undefined.
        /// </remarks>
        public BigInteger PreviousBlockIdentifier { get; set; }

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SerializationName_Identifier, this.Identifier);
            info.AddValue(SerializationName_PreviousBlockIdentifier, this.PreviousBlockIdentifier);
            info.AddValue(SerializationName_Height, this.Height);
            info.AddValue(SerializationName_Timestamp, this.Timestamp);
            info.AddValue(SerializationName_Nonce, this.Nonce);
            info.AddValue(SerializationName_DifficultyTarget, this.DifficultyTarget);
            info.AddValue(SerializationName_Version, this.Version);
            info.AddValue(SerializationName_TypedCoinbase, this.TypedCoinbase);
            info.AddValue(SerializationName_TransactionIdentifiers, this.TransactionIdentifiers);
        }
    }
}
