using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

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
                byte[] versionBytes = BitConverter.GetBytes(version).LittleEndianToOrFromBitConverterEndianness();
                byte[] prevBlockIdBytes = prevBlockId.ToLittleEndianUInt256Array();
                byte[] merkleRootBytes = merkleRoot.ToLittleEndianUInt256Array();
                byte[] timestampBytes = BitConverter.GetBytes(timestamp).LittleEndianToOrFromBitConverterEndianness();
                byte[] packedTargetBytes = BitConverter.GetBytes(bits).LittleEndianToOrFromBitConverterEndianness();
                byte[] nonceBytes = BitConverter.GetBytes(nonce).LittleEndianToOrFromBitConverterEndianness();

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
    }
}
