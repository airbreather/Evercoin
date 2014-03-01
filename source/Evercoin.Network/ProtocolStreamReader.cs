using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    public sealed class ProtocolStreamReader : BinaryReader
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolStreamReader"/> class based on the specified stream and using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The input stream. </param><exception cref="T:System.ArgumentException">The stream does not support reading, is null, or is already closed. </exception>
        public ProtocolStreamReader(Stream input, bool leaveOpen)
            : base(input, Encoding.UTF8, leaveOpen)
        {
        }

        public int ProtocolVersion { get; set; }

        public async Task<ProtocolCompactSize> ReadCompactSizeAsync(CancellationToken token)
        {
            using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token))
            {
                return await this.ReadCompactSizeAsyncCore(linkedCancellationTokenSource.Token);
            }
        }

        public async Task<ProtocolInventoryVector> ReadInventoryVectorAsync(CancellationToken token)
        {
            using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token))
            {
                return await this.ReadInventoryVectorAsyncCore(linkedCancellationTokenSource.Token);
            }
        }

        public async Task<ProtocolNetworkAddress> ReadNetworkAddressAsync(CancellationToken token)
        {
            using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token))
            {
                return await this.ReadNetworkAddressAsyncCore(linkedCancellationTokenSource.Token);
            }
        }

        public async Task<INetworkMessage> ReadNetworkMessageAsync(INetworkParameters networkParameters, Guid clientId, CancellationToken token)
        {
            using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token))
            {
                return await this.ReadNetworkMessageAsyncCore(networkParameters, clientId, linkedCancellationTokenSource.Token);
            }
        }

        public async Task<ProtocolTxIn> ReadTxInAsync(CancellationToken token)
        {
            using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token))
            {
                return await this.ReadTxInAsyncCore(linkedCancellationTokenSource.Token);
            }
        }

        public async Task<ProtocolTxOut> ReadTxOutAsync(CancellationToken token)
        {
            using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token))
            {
                return await this.ReadTxOutAsyncCore(linkedCancellationTokenSource.Token);
            }
        }

        public async Task<ProtocolTransaction> ReadTransactionAsync(CancellationToken token)
        {
            using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token))
            {
                return await this.ReadTransactionAsyncCore(linkedCancellationTokenSource.Token);
            }
        }

        private async Task<byte> ReadByteAsync(CancellationToken token)
        {
            ImmutableList<byte> bytes = await this.ReadBytesAsyncWithIntParam(1, token);
            return bytes[0];
        }

        private async Task<ProtocolCompactSize> ReadCompactSizeAsyncCore(CancellationToken token)
        {
            ulong value;
            byte firstByte = await this.ReadByteAsync(token);
            switch (firstByte)
            {
                case 0xfd:
                    value = await this.ReadUInt16Async(token);
                    break;

                case 0xfe:
                    value = await this.ReadUInt32Async(token);
                    break;

                case 0xff:
                    value = await this.ReadUInt64Async(token);
                    break;

                default:
                    value = firstByte;
                    break;
            }

            return new ProtocolCompactSize(value);
        }

        private async Task<ProtocolInventoryVector> ReadInventoryVectorAsyncCore(CancellationToken token)
        {
            ProtocolInventoryVector.InventoryType inventoryType = (ProtocolInventoryVector.InventoryType)await this.ReadUInt32Async(token);
            BigInteger hash = await this.ReadUInt256Async(token);
            return new ProtocolInventoryVector(inventoryType, hash);
        }

        private async Task<ProtocolNetworkAddress> ReadNetworkAddressAsyncCore(CancellationToken token)
        {
            uint? time = null;
            if (this.ProtocolVersion >= 31402)
            {
                time = await this.ReadUInt32Async(token);
            }

            uint services = await this.ReadUInt32Async(token);

            ImmutableList<byte> addressBytes = await this.ReadBytesAsyncWithIntParam(16, token);
            var v6Address = new IPAddress(addressBytes.ToArray());
            var v4Address = new IPAddress(addressBytes.GetRange(12, 4).ToArray());

            ushort port = await this.ReadUInt16Async(token, littleEndian: false);
            return new ProtocolNetworkAddress(time, services, v4Address, port);
        }

        private async Task<INetworkMessage> ReadNetworkMessageAsyncCore(INetworkParameters networkParameters, Guid clientId, CancellationToken token)
        {
            ImmutableList<byte> data = await this.ReadBytesAsyncWithIntParam(networkParameters.MessagePrefixLengthInBytes, token);
            ImmutableList<byte> expectedStaticPrefix = networkParameters.StaticMessagePrefixData;
            ImmutableList<byte> actualStaticPrefix = data.GetRange(0, expectedStaticPrefix.Count);
            if (!expectedStaticPrefix.SequenceEqual(actualStaticPrefix))
            {
                string exceptionMessage = String.Format(CultureInfo.InvariantCulture,
                                                        "Magic number didn't match!{0}Expected: {1}{0}Actual: {2}",
                                                        Environment.NewLine,
                                                        ByteTwiddling.ByteArrayToHexString(expectedStaticPrefix),
                                                        ByteTwiddling.ByteArrayToHexString(actualStaticPrefix));
                throw new InvalidOperationException(exceptionMessage);
            }

            ImmutableList<byte> commandBytes = data.GetRange(expectedStaticPrefix.Count, data.Count - expectedStaticPrefix.Count);

            int payloadChecksumLengthInBytes = networkParameters.PayloadChecksumLengthInBytes;
            data = await this.ReadBytesAsyncWithIntParam(payloadChecksumLengthInBytes + 4, token);

            ImmutableList<byte> payloadSize = data.GetRange(0, 4);
            ImmutableList<byte> payloadChecksum = data.GetRange(4, payloadChecksumLengthInBytes);

            uint payloadLengthInBytes = BitConverter.ToUInt32(payloadSize.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);
            ImmutableList<byte> payload = await this.ReadBytesAsync(payloadLengthInBytes, token);

            IHashAlgorithm checksumAlgorithm = networkParameters.PayloadChecksumAlgorithm;
            ImmutableList<byte> actualChecksum = await Task.Run(() => checksumAlgorithm.CalculateHash(payload), token);
            if (!payloadChecksum.SequenceEqual(actualChecksum.GetRange(0, payloadChecksumLengthInBytes)))
            {
                string exceptionMessage = String.Format(CultureInfo.InvariantCulture,
                                                        "Payload checksum didn't match!{0}Expected: {1}{0}Actual: {2}",
                                                        Environment.NewLine,
                                                        ByteTwiddling.ByteArrayToHexString(payloadChecksum),
                                                        ByteTwiddling.ByteArrayToHexString(actualChecksum));
                throw new InvalidOperationException(exceptionMessage);
            }

            Message message = new Message(networkParameters, clientId);
            message.CreateFrom(commandBytes, payload);
            return message;
        }

        private async Task<ushort> ReadUInt16Async(CancellationToken token, bool littleEndian = true)
        {
            byte[] bytes = (await this.ReadBytesAsyncWithIntParam(2, token)).ToArray().LittleEndianToOrFromBitConverterEndianness();
            if (!littleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt16(bytes, 0);
        }

        public async Task<uint> ReadUInt32Async(CancellationToken token)
        {
            byte[] bytes = (await this.ReadBytesAsyncWithIntParam(4, token)).ToArray();
            return BitConverter.ToUInt32(bytes.LittleEndianToOrFromBitConverterEndianness(), 0);
        }

        private async Task<ulong> ReadUInt64Async(CancellationToken token)
        {
            byte[] bytes = (await this.ReadBytesAsyncWithIntParam(8, token)).ToArray();
            return BitConverter.ToUInt64(bytes.LittleEndianToOrFromBitConverterEndianness(), 0);
        }

        private async Task<long> ReadInt64Async(CancellationToken token)
        {
            byte[] bytes = (await this.ReadBytesAsyncWithIntParam(8, token)).ToArray();
            return BitConverter.ToInt64(bytes.LittleEndianToOrFromBitConverterEndianness(), 0);
        }

        private async Task<BigInteger> ReadUInt256Async(CancellationToken token)
        {
            byte[] bytes = (await this.ReadBytesAsyncWithIntParam(32, token)).ToArray();
            return new BigInteger(bytes.LittleEndianToOrFromBitConverterEndianness());
        }

        private async Task<ImmutableList<byte>> ReadBytesAsync(ulong numberOfBytesToRead, CancellationToken token)
        {
            ulong bytesRead = 0;
            ImmutableList<byte> bytes = ImmutableList<byte>.Empty;

            while (bytesRead < numberOfBytesToRead)
            {
                int numberOfBytesToReadThisOuterLoop = (int)Math.Min(Int32.MaxValue, numberOfBytesToRead - bytesRead);
                ImmutableList<byte> bytesReadThisOuterLoop = await this.ReadBytesAsyncWithIntParam(numberOfBytesToReadThisOuterLoop, token);
                bytes = bytes.AddRange(bytesReadThisOuterLoop);
                bytesRead += (ulong)bytesReadThisOuterLoop.Count;
            }

            return bytes;
        }

        private async Task<ImmutableList<byte>> ReadBytesAsyncWithIntParam(int numberOfBytesToRead, CancellationToken token)
        {
            int numberOfBytesRead = 0;
            byte[] data = new byte[numberOfBytesToRead];
            while (numberOfBytesRead < numberOfBytesToRead)
            {
                int bytesReadThisLoop = await this.BaseStream.ReadAsync(data, numberOfBytesRead, numberOfBytesToRead - numberOfBytesRead, token);
                if (bytesReadThisLoop == 0)
                {
                    throw new EndOfStreamException("Reached the end of the stream before all requested data was read.");
                }

                numberOfBytesRead += bytesReadThisLoop;
            }

            return data.ToImmutableList();
        }

        public async Task<ProtocolTxIn> ReadTxInAsyncCore(CancellationToken token)
        {
            BigInteger prevOutTxId = await this.ReadUInt256Async(token);

            uint prevOutIndex = await this.ReadUInt32Async(token);

            ulong scriptSigLength = await this.ReadCompactSizeAsyncCore(token);
            ImmutableList<byte> scriptSig = await this.ReadBytesAsync(scriptSigLength, token);

            uint seq = await this.ReadUInt32Async(token);

            return new ProtocolTxIn(prevOutTxId, prevOutIndex, scriptSig, seq);
        }

        public async Task<ProtocolTxOut> ReadTxOutAsyncCore(CancellationToken token)
        {
            long valueInSatoshis = await this.ReadInt64Async(token);
            
            ulong scriptPubKeyLength = await this.ReadCompactSizeAsyncCore(token);
            ImmutableList<byte> scriptPubKey = await this.ReadBytesAsync(scriptPubKeyLength, token);

            return new ProtocolTxOut(valueInSatoshis, scriptPubKey);
        }

        public async Task<ProtocolTransaction> ReadTransactionAsyncCore(CancellationToken token)
        {
            uint version = await this.ReadUInt32Async(token);
            
            ulong inputCount = await this.ReadCompactSizeAsyncCore(token);
            ImmutableList<ProtocolTxIn> inputs = ImmutableList<ProtocolTxIn>.Empty;

            while (inputCount-- > 0)
            {
                ProtocolTxIn nextInput = await this.ReadTxInAsyncCore(token);
                inputs = inputs.Add(nextInput);
            }

            ulong outputCount = await this.ReadCompactSizeAsyncCore(token);
            ImmutableList<ProtocolTxOut> outputs = ImmutableList<ProtocolTxOut>.Empty;

            while (outputCount-- > 0)
            {
                ProtocolTxOut nextOutput = await this.ReadTxOutAsyncCore(token);
                outputs = outputs.Add(nextOutput);
            }

            uint lockTime = await this.ReadUInt32Async(token);

            return new ProtocolTransaction(version, inputs, outputs, lockTime);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ProtocolStreamReader"/> class and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.cts.Cancel();
                this.cts.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
