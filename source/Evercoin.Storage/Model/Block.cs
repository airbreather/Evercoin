using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

using NodaTime;

namespace Evercoin.Storage.Model
{
    [DataContract(Name = "Block", Namespace = "Evercoin.Storage.Model")]
    internal sealed class Block : IBlock
    {
        private const string SerializationName_Identifier = "Identifier";
        private const string SerializationName_PreviousBlockIdentifier = "PreviousBlockIdentifier";
        private const string SerializationName_Timestamp = "Timestamp";
        private const string SerializationName_Nonce = "Nonce";
        private const string SerializationName_DifficultyTarget = "DifficultyTarget";
        private const string SerializationName_TransactionIdentifiers = "TransactionIdentifiers";
        private const string SerializationName_Version = "Version";
        private const string SerializationName_TypedCoinbase = "TypedCoinbase";

        public Block()
        {
        }

        public Block(BigInteger identifier, IBlock copyFrom)
            : this()
        {
            this.Identifier = identifier;
            this.PreviousBlockIdentifier = copyFrom.PreviousBlockIdentifier;
            this.Timestamp = copyFrom.Timestamp;
            this.Nonce = copyFrom.Nonce;
            this.DifficultyTarget = copyFrom.DifficultyTarget;
            this.TransactionIdentifiers = new MerkleTreeNode(copyFrom.TransactionIdentifiers);
            this.Version = copyFrom.Version;
            this.TypedCoinbase = new ValueSource(copyFrom.Coinbase);
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
                   this.PreviousBlockIdentifier == other.PreviousBlockIdentifier &&
                   this.Timestamp == other.Timestamp &&
                   this.Nonce == other.Nonce &&
                   this.DifficultyTarget == other.DifficultyTarget &&
                   this.TransactionIdentifiers.Data.SequenceEqual(other.TransactionIdentifiers.Data) &&
                   this.Version == other.Version;
        }

        /// <summary>
        /// Gets an integer that identifies this block.
        /// </summary>
        [DataMember(Name = SerializationName_Identifier)]
        public BigInteger Identifier { get; set; }

        /// <summary>
        /// Gets the ordered list of the identifiers of
        /// <see cref="ITransaction"/> objects contained within this block.
        /// </summary>
        [DataMember(Name = SerializationName_TransactionIdentifiers)]
        public MerkleTreeNode TransactionIdentifiers { get; set; }

        IMerkleTreeNode IBlock.TransactionIdentifiers { get { return this.TransactionIdentifiers; } }

        /// <summary>
        /// Gets the version of this block.
        /// </summary>
        [DataMember(Name = SerializationName_Version)]
        public uint Version { get; set; }

        /// <summary>
        /// Gets the <see cref="NodaTime.Instant"/> in time when this block was created.
        /// </summary>
        [DataMember(Name = SerializationName_Timestamp)]
        public Instant Timestamp { get; set; }

        /// <summary>
        /// Gets the nonce for this block.
        /// </summary>
        [DataMember(Name = SerializationName_Nonce)]
        public uint Nonce { get; set; }

        [DataMember(Name = SerializationName_TypedCoinbase)]
        public ValueSource TypedCoinbase { get; set; }

        public IValueSource Coinbase { get { return this.TypedCoinbase; } }

        [DataMember(Name = SerializationName_DifficultyTarget)]
        public BigInteger DifficultyTarget { get; set; }

        /// <summary>
        /// Gets the identifier of the previous block in the chain.
        /// </summary>
        [DataMember(Name = SerializationName_PreviousBlockIdentifier)]
        public BigInteger PreviousBlockIdentifier { get; set; }
    }
}
