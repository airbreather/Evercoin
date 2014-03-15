using System;
using System.Collections.Generic;
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

        private readonly ProtocolTransaction[] includedTransactions;

        public ProtocolBlock(uint version, BigInteger prevBlockId, BigInteger merkleRoot, uint timestamp, uint bits, uint nonce, IEnumerable<ProtocolTransaction> includedTransactions)
        {
            this.version = version;
            this.prevBlockId = prevBlockId;
            this.merkleRoot = merkleRoot;
            this.timestamp = timestamp;
            this.bits = bits;
            this.nonce = nonce;
            this.includedTransactions = includedTransactions.GetArray();
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

        public byte[] Data
        {
            get
            {
                byte[] headerData = this.HeaderData;

                byte[] transactionCountBytes = ((ProtocolCompactSize)(ulong)this.IncludedTransactions.Length).Data;
                IEnumerable<byte[]> transactionDataSources = this.IncludedTransactions.Select(x => x.Data);

                byte[] transactionData = ByteTwiddling.ConcatenateData(transactionDataSources);

                return ByteTwiddling.ConcatenateData(headerData, transactionCountBytes, transactionData);
            }
        }

        public uint Version { get { return this.version; } }

        public BigInteger PrevBlockId  { get { return this.prevBlockId; } }

        public BigInteger MerkleRoot { get { return this.merkleRoot; } }

        public uint Timestamp { get { return this.timestamp; } }

        public uint Bits { get { return this.bits; } }

        public uint Nonce { get { return this.nonce; } }

        public ProtocolTransaction[] IncludedTransactions { get { return this.includedTransactions; } }

        public IBlock ToBlock(BigInteger blockIdentifier, IHashAlgorithm transactionHashAlgorithm)
        {
            foreach (ProtocolTransaction transaction in this.includedTransactions)
            {
                transaction.CalculateTxId(transactionHashAlgorithm);
            }

            return new TypedBlock
            {
                Coinbase = new TypedCoinbaseValueSource { AvailableValue = this.IncludedTransactions[0].Outputs.Sum(x => x.ValueInSatoshis), OriginatingBlockIdentifier = blockIdentifier },
                DifficultyTarget = TargetFromBits(this.Bits),
                Nonce = this.Nonce,
                PreviousBlockIdentifier = this.PrevBlockId,
                Timestamp = Instant.FromSecondsSinceUnixEpoch(this.Timestamp),
                TransactionIdentifiers = this.IncludedTransactions.Select(x => x.TxId.ToLittleEndianUInt256Array()).ToMerkleTree(transactionHashAlgorithm),
                Version = this.Version
            };
        }

        private static BigInteger TargetFromBits(uint bits)
        {
            uint mantissa = bits & 0x007fffff;
            bool negative = (bits & 0x00800000) != 0;
            byte exponent = (byte)(bits >> 24);
            BigInteger result;

            if (exponent <= 3)
            {
                mantissa >>= 8 * (3 - exponent);
                result = mantissa;
            }
            else
            {
                result = mantissa;
                result <<= 8 * (exponent - 3);
            }

            if ((result.Sign < 0) != negative)
            {
                result = -result;
            }

            return result;
        }
    }
}
