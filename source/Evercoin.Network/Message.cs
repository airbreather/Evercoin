using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal sealed class Message : INetworkMessage
    {
        private readonly INetworkParameters networkParameters;

        private readonly Guid remoteClientId;

        private ImmutableList<byte> payloadSize;

        private ImmutableList<byte> payloadChecksum;

        public Message(INetworkParameters networkParameters, Guid remoteClientId)
        {
            this.networkParameters = networkParameters;
            this.remoteClientId = remoteClientId;
        }

        public ImmutableList<byte> FullData
        {
            get
            {
                return this.networkParameters.StaticMessagePrefixData
                           .AddRange(this.CommandBytes)
                           .AddRange(this.payloadSize)
                           .AddRange(this.payloadChecksum)
                           .AddRange(this.Payload);
            }
        }

        /// <summary>
        /// Gets the command of this message.
        /// </summary>
        public ImmutableList<byte> CommandBytes { get; private set; }

        public ImmutableList<byte> Payload { get; private set; }

        /// <summary>
        /// Gets the network parameters for this message.
        /// </summary>
        public INetworkParameters NetworkParameters { get { return this.networkParameters; } }

        /// <summary>
        /// Gets the ID of the remote client sending or receiving this message.
        /// </summary>
        public Guid RemoteClient { get { return this.remoteClientId; } }

        public void CreateFrom(IEnumerable<byte> command, IEnumerable<byte> payload)
        {
            this.CommandBytes = command.ToImmutableList();
            this.Payload = payload.ToImmutableList();

            uint payloadSizeInBytes = (uint)this.Payload.Count;

            this.payloadSize = BitConverter.GetBytes(payloadSizeInBytes)
                                           .LittleEndianToOrFromBitConverterEndianness()
                                           .ToImmutableList();

            IHashAlgorithm checksumAlgorithm = this.networkParameters.PayloadChecksumAlgorithm;
            int checksumLengthInBytes = this.networkParameters.PayloadChecksumLengthInBytes;

            ImmutableList<byte> checksum = checksumAlgorithm.CalculateHash(this.Payload);
            this.payloadChecksum = checksum.GetRange(0, checksumLengthInBytes);
        }

        public async Task ReadFrom(Stream stream, CancellationToken token)
        {
            ImmutableList<byte> data = await stream.ReadBytesAsyncWithIntParam(this.networkParameters.MessagePrefixLengthInBytes, token);
            ImmutableList<byte> expectedStaticPrefix = this.networkParameters.StaticMessagePrefixData;
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

            this.CommandBytes = data.GetRange(expectedStaticPrefix.Count, data.Count - expectedStaticPrefix.Count);

            int payloadChecksumLengthInBytes = this.networkParameters.PayloadChecksumLengthInBytes;
            data = await stream.ReadBytesAsyncWithIntParam(payloadChecksumLengthInBytes + 4, token);

            this.payloadSize = data.GetRange(0, 4);
            this.payloadChecksum = data.GetRange(4, payloadChecksumLengthInBytes);

            uint payloadLengthInBytes = BitConverter.ToUInt32(this.payloadSize.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);
            this.Payload = await stream.ReadBytesAsync(payloadLengthInBytes, token);

            IHashAlgorithm checksumAlgorithm = this.networkParameters.PayloadChecksumAlgorithm;
            ImmutableList<byte> actualChecksum = await Task.Run(() => checksumAlgorithm.CalculateHash(this.Payload), token);
            if (!this.payloadChecksum.SequenceEqual(actualChecksum.GetRange(0, payloadChecksumLengthInBytes)))
            {
                string exceptionMessage = String.Format(CultureInfo.InvariantCulture,
                                                        "Payload checksum didn't match!{0}Expected: {1}{0}Actual: {2}",
                                                        Environment.NewLine,
                                                        ByteTwiddling.ByteArrayToHexString(this.payloadChecksum),
                                                        ByteTwiddling.ByteArrayToHexString(actualChecksum));
                throw new InvalidOperationException(exceptionMessage);
            }
        }
    }
}
