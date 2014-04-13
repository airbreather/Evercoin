using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network
{
    public sealed class ProtocolStreamReader : BinaryReader
    {
        private readonly IHashAlgorithmStore hashAlgorithmStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolStreamReader"/> class based on the specified stream and using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The input stream. </param><exception cref="T:System.ArgumentException">The stream does not support reading, is null, or is already closed. </exception>
        public ProtocolStreamReader(Stream input, bool leaveOpen, IHashAlgorithmStore hashAlgorithmStore)
            : base(input, Encoding.UTF8, leaveOpen)
        {
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public int ProtocolVersion { get; set; }

        public Task<uint> ReadUInt32Async(CancellationToken token)
        {
            return this.ReadUInt32AsyncCore(token);
        }

        public Task<FancyByteArray> ReadUInt256Async(CancellationToken token)
        {
            return this.ReadUInt256AsyncCore(token);
        }

        public Task<ProtocolCompactSize> ReadCompactSizeAsync(CancellationToken token)
        {
            return this.ReadCompactSizeAsyncCore(token);
        }

        public Task<ProtocolInventoryVector> ReadInventoryVectorAsync(CancellationToken token)
        {
            return this.ReadInventoryVectorAsyncCore(token);
        }

        public Task<ProtocolNetworkAddress> ReadNetworkAddressAsync(CancellationToken token)
        {
            return this.ReadNetworkAddressAsyncCore(token);
        }

        public Task<INetworkMessage> ReadNetworkMessageAsync(INetworkParameters networkParameters, INetworkPeer peer, CancellationToken token)
        {
            return this.ReadNetworkMessageAsyncCore(networkParameters, peer, token);
        }

        public Task<ProtocolTxIn> ReadTxInAsync(CancellationToken token)
        {
            return this.ReadTxInAsyncCore(token);
        }

        public Task<ProtocolTxOut> ReadTxOutAsync(CancellationToken token)
        {
            return this.ReadTxOutAsyncCore(token);
        }

        public Task<ProtocolTransaction> ReadTransactionAsync(CancellationToken token)
        {
            return this.ReadTransactionAsyncCore(token);
        }

        public Task<Tuple<ProtocolBlock, IEnumerable<ProtocolTransaction>>> ReadBlockAsync(CancellationToken token)
        {
            return this.ReadBlockAsyncCore(token);
        }

        public Task<ProtocolVersionPacket> ReadVersionPacketAsync(CancellationToken token)
        {
            return this.ReadVersionPacketAsyncCore(token);
        }

        private async Task<byte> ReadByteAsync(CancellationToken token)
        {
            byte[] bytes = await this.ReadBytesAsyncWithIntParam(1, token).ConfigureAwait(false);
            return bytes[0];
        }

        private async Task<ProtocolCompactSize> ReadCompactSizeAsyncCore(CancellationToken token)
        {
            ulong value;
            byte firstByte = await this.ReadByteAsync(token).ConfigureAwait(false);
            switch (firstByte)
            {
                case 0xfd:
                    value = await this.ReadUInt16Async(token).ConfigureAwait(false);
                    break;

                case 0xfe:
                    value = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);
                    break;

                case 0xff:
                    value = await this.ReadUInt64Async(token).ConfigureAwait(false);
                    break;

                default:
                    value = firstByte;
                    break;
            }

            return new ProtocolCompactSize(value);
        }

        private async Task<ProtocolInventoryVector> ReadInventoryVectorAsyncCore(CancellationToken token)
        {
            ProtocolInventoryVector.InventoryType inventoryType = (ProtocolInventoryVector.InventoryType)await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);
            FancyByteArray hash = await this.ReadUInt256AsyncCore(token).ConfigureAwait(false);
            return new ProtocolInventoryVector(inventoryType, hash);
        }

        private async Task<ProtocolNetworkAddress> ReadNetworkAddressAsyncCore(CancellationToken token)
        {
            uint? time = null;
            if (this.ProtocolVersion >= 31402)
            {
                time = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);
            }

            ulong services = await this.ReadUInt64Async(token).ConfigureAwait(false);

            byte[] addressBytes = await this.ReadBytesAsyncWithIntParam(16, token).ConfigureAwait(false);
            var v6Address = new IPAddress(addressBytes);
            var v4Address = new IPAddress(addressBytes.GetRange(12, 4).GetArray());

            ushort port = await this.ReadUInt16Async(token, littleEndian: false).ConfigureAwait(false);
            return new ProtocolNetworkAddress(time, services, v4Address, port);
        }

        private async Task<INetworkMessage> ReadNetworkMessageAsyncCore(INetworkParameters networkParameters, INetworkPeer peer, CancellationToken token)
        {
            byte[] data = await this.ReadBytesAsyncWithIntParam(networkParameters.MessagePrefixLengthInBytes, token).ConfigureAwait(false);
            byte[] expectedStaticPrefix = networkParameters.StaticMessagePrefixData;
            IReadOnlyList<byte> actualStaticPrefix = data.GetRange(0, expectedStaticPrefix.Length);
            if (!expectedStaticPrefix.SequenceEqual(actualStaticPrefix))
            {
                string exceptionMessage = String.Format(CultureInfo.InvariantCulture,
                                                        "Magic number didn't match!{0}Expected: {1}{0}Actual: {2}",
                                                        Environment.NewLine,
                                                        ByteTwiddling.ByteArrayToHexString(expectedStaticPrefix),
                                                        ByteTwiddling.ByteArrayToHexString(actualStaticPrefix));
                throw new InvalidOperationException(exceptionMessage);
            }

            IReadOnlyList<byte> commandBytes = data.GetRange(expectedStaticPrefix.Length, data.Length - expectedStaticPrefix.Length);

            int payloadChecksumLengthInBytes = networkParameters.PayloadChecksumLengthInBytes;
            data = await this.ReadBytesAsyncWithIntParam(payloadChecksumLengthInBytes + 4, token).ConfigureAwait(false);

            IReadOnlyList<byte> payloadSize = data.GetRange(0, 4);
            IReadOnlyList<byte> payloadChecksum = data.GetRange(4, payloadChecksumLengthInBytes);

            uint payloadLengthInBytes = BitConverter.ToUInt32(payloadSize.GetArray().LittleEndianToOrFromBitConverterEndianness(), 0);
            byte[] payload = await this.ReadBytesAsync(payloadLengthInBytes, token).ConfigureAwait(false);

            IHashAlgorithm checksumAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(networkParameters.PayloadChecksumAlgorithmIdentifier);
            byte[] actualChecksum = checksumAlgorithm.CalculateHash(payload);
            if (!payloadChecksum.SequenceEqual(actualChecksum.GetRange(0, payloadChecksumLengthInBytes)))
            {
                string exceptionMessage = String.Format(CultureInfo.InvariantCulture,
                                                        "Payload checksum didn't match!{0}Expected: {1}{0}Actual: {2}",
                                                        Environment.NewLine,
                                                        ByteTwiddling.ByteArrayToHexString(payloadChecksum),
                                                        ByteTwiddling.ByteArrayToHexString(actualChecksum));
                throw new InvalidOperationException(exceptionMessage);
            }

            Message message = new Message(networkParameters, this.hashAlgorithmStore, peer);
            message.CreateFrom(commandBytes, payload);
            return message;
        }

        private async Task<ushort> ReadUInt16Async(CancellationToken token, bool littleEndian = true)
        {
            byte[] bytes = (await this.ReadBytesAsyncWithIntParam(2, token).ConfigureAwait(false)).LittleEndianToOrFromBitConverterEndianness();
            if (!littleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt16(bytes, 0);
        }

        private async Task<uint> ReadUInt32AsyncCore(CancellationToken token)
        {
            byte[] bytes = await this.ReadBytesAsyncWithIntParam(4, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(bytes.LittleEndianToOrFromBitConverterEndianness(), 0);
        }

        private async Task<int> ReadInt32AsyncCore(CancellationToken token)
        {
            byte[] bytes = await this.ReadBytesAsyncWithIntParam(4, token).ConfigureAwait(false);
            return BitConverter.ToInt32(bytes.LittleEndianToOrFromBitConverterEndianness(), 0);
        }

        private async Task<ulong> ReadUInt64Async(CancellationToken token)
        {
            byte[] bytes = await this.ReadBytesAsyncWithIntParam(8, token).ConfigureAwait(false);
            return BitConverter.ToUInt64(bytes.LittleEndianToOrFromBitConverterEndianness(), 0);
        }

        private async Task<long> ReadInt64Async(CancellationToken token)
        {
            byte[] bytes = await this.ReadBytesAsyncWithIntParam(8, token).ConfigureAwait(false);
            return BitConverter.ToInt64(bytes.LittleEndianToOrFromBitConverterEndianness(), 0);
        }

        private async Task<FancyByteArray> ReadUInt256AsyncCore(CancellationToken token)
        {
            return await this.ReadBytesAsyncWithIntParam(32, token).ConfigureAwait(false);
        }

        private async Task<byte[]> ReadBytesAsync(ulong numberOfBytesToRead, CancellationToken token)
        {
            ulong bytesRead = 0;
            List<byte[]> sequencesRead = new List<byte[]>();

            while (bytesRead < numberOfBytesToRead)
            {
                int numberOfBytesToReadThisOuterLoop = (int)Math.Min(Int32.MaxValue, numberOfBytesToRead - bytesRead);
                byte[] bytesReadThisOuterLoop = await this.ReadBytesAsyncWithIntParam(numberOfBytesToReadThisOuterLoop, token).ConfigureAwait(false);
                sequencesRead.Add(bytesReadThisOuterLoop);
                bytesRead += (ulong)bytesReadThisOuterLoop.Length;
            }

            return ByteTwiddling.ConcatenateData(sequencesRead);
        }

        private async Task<byte[]> ReadBytesAsyncWithIntParam(int numberOfBytesToRead, CancellationToken token)
        {
            int numberOfBytesRead = 0;
            byte[] data = new byte[numberOfBytesToRead];
            while (numberOfBytesRead < numberOfBytesToRead)
            {
                int bytesReadThisLoop = await this.BaseStream.ReadAsync(data, numberOfBytesRead, numberOfBytesToRead - numberOfBytesRead, token).ConfigureAwait(false);
                if (bytesReadThisLoop == 0)
                {
                    throw new EndOfStreamException("Reached the end of the stream before all requested data was read.");
                }

                numberOfBytesRead += bytesReadThisLoop;
            }

            return data;
        }

        private async Task<ProtocolTxIn> ReadTxInAsyncCore(CancellationToken token)
        {
            FancyByteArray prevOutTxId = await this.ReadUInt256AsyncCore(token).ConfigureAwait(false);

            uint prevOutIndex = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);

            ulong scriptSigLength = await this.ReadCompactSizeAsyncCore(token).ConfigureAwait(false);
            byte[] scriptSig = await this.ReadBytesAsync(scriptSigLength, token).ConfigureAwait(false);

            uint seq = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);

            return new ProtocolTxIn(prevOutTxId, prevOutIndex, scriptSig, seq);
        }

        private async Task<ProtocolTxOut> ReadTxOutAsyncCore(CancellationToken token)
        {
            long valueInSatoshis = await this.ReadInt64Async(token).ConfigureAwait(false);

            ulong scriptPubKeyLength = await this.ReadCompactSizeAsyncCore(token).ConfigureAwait(false);
            byte[] scriptPubKey = await this.ReadBytesAsync(scriptPubKeyLength, token).ConfigureAwait(false);

            return new ProtocolTxOut(valueInSatoshis, scriptPubKey);
        }

        private async Task<Tuple<ProtocolBlock, IEnumerable<ProtocolTransaction>>> ReadBlockAsyncCore(CancellationToken token)
        {
            uint version = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);
            FancyByteArray prevBlockId = await this.ReadUInt256AsyncCore(token).ConfigureAwait(false);
            FancyByteArray merkleRoot = await this.ReadUInt256AsyncCore(token).ConfigureAwait(false);
            uint timestamp = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);
            uint bits = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);
            uint nonce = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);

            ulong transactionCount = await this.ReadCompactSizeAsyncCore(token).ConfigureAwait(false);
            ProtocolTransaction[] includedTransactions = new ProtocolTransaction[transactionCount];

            int transactionIndex = 0;
            while (transactionCount-- > 0)
            {
                ProtocolTransaction nextTransaction = await this.ReadTransactionAsyncCore(token).ConfigureAwait(false);
                includedTransactions[transactionIndex++] = nextTransaction;
            }

            return Tuple.Create(new ProtocolBlock(version, prevBlockId, merkleRoot, timestamp, bits, nonce), includedTransactions.AsEnumerable());
        }

        private async Task<ProtocolTransaction> ReadTransactionAsyncCore(CancellationToken token)
        {
            uint version = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);

            ulong inputCount = await this.ReadCompactSizeAsyncCore(token).ConfigureAwait(false);
            List<ProtocolTxIn> inputs = new List<ProtocolTxIn>();

            while (inputCount-- > 0)
            {
                ProtocolTxIn nextInput = await this.ReadTxInAsyncCore(token).ConfigureAwait(false);
                inputs.Add(nextInput);
            }

            ulong outputCount = await this.ReadCompactSizeAsyncCore(token).ConfigureAwait(false);
            List<ProtocolTxOut> outputs = new List<ProtocolTxOut>();

            while (outputCount-- > 0)
            {
                ProtocolTxOut nextOutput = await this.ReadTxOutAsyncCore(token).ConfigureAwait(false);
                outputs.Add(nextOutput);
            }

            uint lockTime = await this.ReadUInt32AsyncCore(token).ConfigureAwait(false);

            return new ProtocolTransaction(version, inputs, outputs, lockTime);
        }

        private async Task<string> ReadProtocolStringAsyncCore(Encoding encoding, CancellationToken token)
        {
            ulong length = await this.ReadCompactSizeAsyncCore(token).ConfigureAwait(false);
            byte[] data = await this.ReadBytesAsync(length, token).ConfigureAwait(false);
            return encoding.GetString(data);
        }

        private async Task<bool> ReadBooleanAsyncCore(CancellationToken token)
        {
            byte[] singleByte = await this.ReadBytesAsyncWithIntParam(1, token).ConfigureAwait(false);
            return singleByte[0] != 0;
        }

        private async Task<ProtocolVersionPacket> ReadVersionPacketAsyncCore(CancellationToken token)
        {
            int version = await this.ReadInt32AsyncCore(token).ConfigureAwait(false);
            ulong services = await this.ReadUInt64Async(token).ConfigureAwait(false);
            long timestampInSecondsSinceUnixEpoch = await this.ReadInt64Async(token).ConfigureAwait(false);
            ProtocolNetworkAddress receivingAddress = await this.ReadNetworkAddressAsyncCore(token).ConfigureAwait(false);
            ProtocolNetworkAddress sendingAddress = await this.ReadNetworkAddressAsyncCore(token).ConfigureAwait(false);
            ulong nonce = await this.ReadUInt64Async(token).ConfigureAwait(false);
            string userAgent = await this.ReadProtocolStringAsyncCore(Encoding.UTF8, token).ConfigureAwait(false);
            int startHeight = await this.ReadInt32AsyncCore(token).ConfigureAwait(false);

            Instant timestamp = Instant.FromSecondsSinceUnixEpoch(timestampInSecondsSinceUnixEpoch);
            return new ProtocolVersionPacket(version, services, timestamp, receivingAddress, sendingAddress, nonce, userAgent, startHeight, false);
        }
    }
}
