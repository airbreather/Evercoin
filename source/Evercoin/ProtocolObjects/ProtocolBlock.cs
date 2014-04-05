using System;
using System.Linq;
using System.Numerics;

using Evercoin.BaseImplementations;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolBlock
    {
        private readonly uint version;

        private readonly BigInteger prevBlockId;

        private readonly BigInteger merkleRoot;

        private readonly uint timestamp;

        private readonly uint bits;

        private readonly uint nonce;

        public ProtocolBlock(uint version, BigInteger prevBlockId, BigInteger merkleRoot, uint timestamp, uint bits, uint nonce)
        {
            this.version = version;
            this.prevBlockId = prevBlockId;
            this.merkleRoot = merkleRoot;
            this.timestamp = timestamp;
            this.bits = bits;
            this.nonce = nonce;
        }

        public byte[] HeaderData
        {
            get
            {
                byte[] versionBytes = BitConverter.GetBytes(this.version).LittleEndianToOrFromBitConverterEndianness();
                byte[] prevBlockIdBytes = this.prevBlockId.ToLittleEndianUInt256Array();
                byte[] merkleRootBytes = this.merkleRoot.ToLittleEndianUInt256Array();
                byte[] timestampBytes = BitConverter.GetBytes(this.timestamp).LittleEndianToOrFromBitConverterEndianness();
                byte[] packedTargetBytes = BitConverter.GetBytes(this.bits).LittleEndianToOrFromBitConverterEndianness();
                byte[] nonceBytes = BitConverter.GetBytes(this.nonce).LittleEndianToOrFromBitConverterEndianness();

                return ByteTwiddling.ConcatenateData(versionBytes, prevBlockIdBytes, merkleRootBytes, timestampBytes, packedTargetBytes, nonceBytes);
            }
        }

        public uint Version { get { return this.version; } }

        public BigInteger PrevBlockId  { get { return this.prevBlockId; } }

        public BigInteger MerkleRoot { get { return this.merkleRoot; } }

        public uint Timestamp { get { return this.timestamp; } }

        public uint Bits { get { return this.bits; } }

        public uint Nonce { get { return this.nonce; } }

        public IBlock ToBlock(IHashAlgorithm transactionHashAlgorithm)
        {
            return new TypedBlock
            {
                DifficultyTarget = Extensions.TargetFromBits(this.Bits),
                Nonce = this.Nonce,
                PreviousBlockIdentifier = this.PrevBlockId,
                Timestamp = Instant.FromSecondsSinceUnixEpoch(this.Timestamp),
                TransactionIdentifiers = this.MerkleRoot.ToLittleEndianUInt256Array().AsSingleElementEnumerable().ToMerkleTree(transactionHashAlgorithm),
                Version = this.Version
            };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            ProtocolBlock other = obj as ProtocolBlock;
            return other != null &&
                   this.HeaderData.SequenceEqual(other.HeaderData);
        }

        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWithEnumerable(this.HeaderData);

            return builder;
        }
    }
}
