using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;

using Evercoin.BaseImplementations;
using Evercoin.ProtocolObjects;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.App
{
    [Export(typeof(IChainSerializer))]
    public sealed class BitcoinChainSerializer : IChainSerializer
    {
        public FancyByteArray GetBytesForBlock(IBlock block)
        {
            byte[] versionBytes = BitConverter.GetBytes(block.Version).LittleEndianToOrFromBitConverterEndianness();
            byte[] prevBlockIdBytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(block.PreviousBlockIdentifier, 32, Endianness.LittleEndian);
            byte[] merkleRootBytes = block.TransactionIdentifiers.Data;
            byte[] timestampBytes = BitConverter.GetBytes((uint)((block.Timestamp - NodaConstants.UnixEpoch).Ticks / NodaConstants.TicksPerSecond)).LittleEndianToOrFromBitConverterEndianness();
            byte[] packedTargetBytes = BitConverter.GetBytes(Extensions.TargetToBits(block.DifficultyTarget)).LittleEndianToOrFromBitConverterEndianness();
            byte[] nonceBytes = BitConverter.GetBytes(block.Nonce).LittleEndianToOrFromBitConverterEndianness();

            return ByteTwiddling.ConcatenateData(versionBytes, prevBlockIdBytes, merkleRootBytes, timestampBytes, packedTargetBytes, nonceBytes);
        }

        public FancyByteArray GetBytesForTransaction(ITransaction transaction)
        {
            // Version
            byte[] versionBytes = BitConverter.GetBytes(transaction.Version).LittleEndianToOrFromBitConverterEndianness();

            // Inputs
            byte[] inputCountBytes = ((ProtocolCompactSize)(ulong)transaction.Inputs.Count).Data;
            IEnumerable<byte[]> inputByteSources = transaction.Inputs.Select(x =>
            {
                byte[] prevOutTxIdBytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(x.SpentTransactionIdentifier, 32, Endianness.LittleEndian);
                byte[] prevOutNBytes = BitConverter.GetBytes(x.SpentTransactionOutputIndex).LittleEndianToOrFromBitConverterEndianness();
                byte[] scriptSigLengthBytes = ((ProtocolCompactSize)(ulong)x.ScriptSignature.Value.Length).Data;
                byte[] scriptSigBytes = x.ScriptSignature;
                byte[] sequenceBytes = BitConverter.GetBytes(x.SequenceNumber).LittleEndianToOrFromBitConverterEndianness();

                return ByteTwiddling.ConcatenateData(prevOutTxIdBytes, prevOutNBytes, scriptSigLengthBytes, scriptSigBytes, sequenceBytes);
            });

            // Outputs
            byte[] outputCountBytes = ((ProtocolCompactSize)(ulong)transaction.Outputs.Count).Data;
            IEnumerable<byte[]> outputByteSources = transaction.Outputs.Select(x =>
            {
                byte[] valueBytes = BitConverter.GetBytes((long)x.AvailableValue).LittleEndianToOrFromBitConverterEndianness();
                byte[] scriptPubKeyLengthBytes = ((ProtocolCompactSize)(ulong)x.ScriptPublicKey.Value.Length).Data;
                byte[] scriptPubKeyBytes = x.ScriptPublicKey;

                return ByteTwiddling.ConcatenateData(valueBytes, scriptPubKeyLengthBytes, scriptPubKeyBytes);
            });

            // Lock time
            byte[] lockTimeBytes = BitConverter.GetBytes(transaction.LockTime).LittleEndianToOrFromBitConverterEndianness();

            byte[] inputBytes = ByteTwiddling.ConcatenateData(inputByteSources);
            byte[] outputBytes = ByteTwiddling.ConcatenateData(outputByteSources);

            return ByteTwiddling.ConcatenateData(versionBytes, inputCountBytes, inputBytes, outputCountBytes, outputBytes, lockTimeBytes);
        }

        public IBlock GetBlockForBytes(IEnumerable<byte> serializedBlock)
        {
            byte[] serializedBlockArray = serializedBlock.GetArray();

            int offset = 0;
            return new TypedBlock
            {
                Version = GetUInt32(serializedBlockArray, ref offset),
                PreviousBlockIdentifier = GetUInt256(serializedBlockArray, ref offset),
                TransactionIdentifiers = GetBytes(32, serializedBlockArray, ref offset).AsSingleElementEnumerable().ToMerkleTree(null),
                Timestamp = Instant.FromSecondsSinceUnixEpoch(GetUInt32(serializedBlockArray, ref offset)),
                DifficultyTarget = Extensions.TargetFromBits(GetUInt32(serializedBlockArray, ref offset)),
                Nonce = GetUInt32(serializedBlockArray, ref offset)
            };
        }

        public ITransaction GetTransactionForBytes(IEnumerable<byte> serializedTransaction)
        {
            byte[] serializedTransactionArray = serializedTransaction.GetArray();

            int offset = 0;
            uint version = GetUInt32(serializedTransactionArray, ref offset);

            ulong inputCount = GetCompactSize(serializedTransactionArray, ref offset);
            List<ProtocolTxIn> inputs = new List<ProtocolTxIn>();

            while (inputCount-- > 0)
            {
                FancyByteArray prevOutTxId = GetUInt256(serializedTransactionArray, ref offset);

                uint prevOutIndex = GetUInt32(serializedTransactionArray, ref offset);

                ulong scriptSigLength = GetCompactSize(serializedTransactionArray, ref offset);
                IReadOnlyList<byte> scriptSig = GetBytes(scriptSigLength, serializedTransactionArray, ref offset);

                uint seq = GetUInt32(serializedTransactionArray, ref offset);
                ProtocolTxIn nextInput = new ProtocolTxIn(prevOutTxId, prevOutIndex, scriptSig, seq);
                inputs.Add(nextInput);
            }

            ulong outputCount = GetCompactSize(serializedTransactionArray, ref offset);
            List<ProtocolTxOut> outputs = new List<ProtocolTxOut>();

            while (outputCount-- > 0)
            {
                long valueInSatoshis = GetInt64(serializedTransactionArray, ref offset);

                ulong scriptPubKeyLength = GetCompactSize(serializedTransactionArray, ref offset);
                IReadOnlyList<byte> scriptPubKey = GetBytes(scriptPubKeyLength, serializedTransactionArray, ref offset);
                ProtocolTxOut nextOutput = new ProtocolTxOut(valueInSatoshis, scriptPubKey);
                outputs.Add(nextOutput);
            }

            uint lockTime = GetUInt32(serializedTransactionArray, ref offset);
            ProtocolTransaction protocolTransaction = new ProtocolTransaction(version, inputs, outputs, lockTime);
            return protocolTransaction.ToTransaction();
        }

        private static ushort GetUInt16(IReadOnlyList<byte> bytes, ref int offset)
        {
            byte[] subArray = bytes.GetRange(offset, 2).ToArray().LittleEndianToOrFromBitConverterEndianness();
            offset += 2;
            return BitConverter.ToUInt16(subArray, 0);
        }

        private static uint GetUInt32(IReadOnlyList<byte> bytes, ref int offset)
        {
            return (uint)bytes[offset++] << 00 |
                   (uint)bytes[offset++] << 08 |
                   (uint)bytes[offset++] << 16 |
                   (uint)bytes[offset++] << 24;
        }

        private static ulong GetUInt64(IReadOnlyList<byte> bytes, ref int offset)
        {
            byte[] subArray = bytes.GetRange(offset, 8).ToArray().LittleEndianToOrFromBitConverterEndianness();
            offset += 8;
            return BitConverter.ToUInt64(subArray, 0);
        }

        private static long GetInt64(IReadOnlyList<byte> bytes, ref int offset)
        {
            byte[] subArray = bytes.GetRange(offset, 8).ToArray().LittleEndianToOrFromBitConverterEndianness();
            offset += 8;
            return BitConverter.ToInt64(subArray, 0);
        }

        private static FancyByteArray GetUInt256(IReadOnlyList<byte> bytes, ref int offset)
        {
            IReadOnlyList<byte> subArray = bytes.GetRange(offset, 32);
            offset += 32;
            return FancyByteArray.CreateFromBytes(subArray);
        }

        private static ulong GetCompactSize(IReadOnlyList<byte> bytes, ref int offset)
        {
            byte firstByte = bytes[offset++];
            switch (firstByte)
            {
                case 0xfd:
                    return GetUInt16(bytes, ref offset);

                case 0xfe:
                    return GetUInt32(bytes, ref offset);

                case 0xff:
                    return GetUInt64(bytes, ref offset);

                default:
                    return firstByte;
            }
        }

        private static IReadOnlyList<byte> GetBytes(ulong n, IReadOnlyList<byte> bytes, ref int offset)
        {
            if (n > int.MaxValue)
            {
                throw new NotSupportedException("TOO MANY BYTES!!");
            }

            int integerN = (int)n;
            IReadOnlyList<byte> result = bytes.GetRange(offset, integerN);
            offset += integerN;
            return result;
        }
    }
}
