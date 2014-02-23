using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal sealed class Message
    {
        private readonly INetworkParameters networkParameters;

        private ImmutableList<byte> payloadSize;

        private ImmutableList<byte> payloadChecksum;

        public Message(INetworkParameters networkParameters)
        {
            this.networkParameters = networkParameters;
        }

        public ImmutableList<byte> FullData
        {
            get
            {
                return this.StaticPrefix
                           .AddRange(this.Command)
                           .AddRange(this.payloadSize)
                           .AddRange(this.payloadChecksum)
                           .AddRange(this.Payload);
            }
        }

        public ImmutableList<byte> StaticPrefix { get; private set; }

        public ImmutableList<byte> Command { get; private set; }

        public ImmutableList<byte> Payload { get; private set; }

        public void CreateFrom(IEnumerable<byte> staticPrefix, IEnumerable<byte> command, IEnumerable<byte> payload)
        {
            this.StaticPrefix = staticPrefix.ToImmutableList();
            this.Command = command.ToImmutableList();
            this.Payload = payload.ToImmutableList();

            uint payloadSizeInBytes = (uint)this.Payload.Count;

            this.payloadSize = BitConverter.GetBytes(payloadSizeInBytes)
                                           .LittleEndianToOrFromBitConverterEndianness()
                                           .ToImmutableList();

            IHashAlgorithm checksumAlgorithm = this.networkParameters.PayloadChecksumAlgorithm;
            int checksumLengthInBytes = this.networkParameters.PayloadChecksumLengthInBytes;

            IImmutableList<byte> checksum = checksumAlgorithm.CalculateHash(this.Payload);
            this.payloadChecksum = checksum.Take(checksumLengthInBytes)
                                           .ToImmutableList();
        }

        public async Task ReadFrom(Stream stream)
        {
            ImmutableList<byte> data = await stream.ReadBytesAsyncWithIntParam(this.networkParameters.MessagePrefixLengthInBytes);
            IImmutableList<byte> expectedStaticPrefix = this.networkParameters.StaticMessagePrefixData;
            this.StaticPrefix = data.GetRange(0, expectedStaticPrefix.Count);
            if (!expectedStaticPrefix.SequenceEqual(this.StaticPrefix))
            {
                string exceptionMessage = String.Format(CultureInfo.InvariantCulture,
                                                        "Magic number didn't match!{0}Expected: {1}{0}Actual: {2}",
                                                        Environment.NewLine,
                                                        ByteTwiddling.ByteArrayToHexString(expectedStaticPrefix),
                                                        ByteTwiddling.ByteArrayToHexString(this.StaticPrefix));
                throw new InvalidOperationException(exceptionMessage);
            }

            this.Command = data.GetRange(expectedStaticPrefix.Count, data.Count - expectedStaticPrefix.Count);

            int payloadChecksumLengthInBytes = this.networkParameters.PayloadChecksumLengthInBytes;
            data = await stream.ReadBytesAsyncWithIntParam(payloadChecksumLengthInBytes + 4);

            this.payloadSize = data.GetRange(0, 4);
            this.payloadChecksum = data.GetRange(4, payloadChecksumLengthInBytes);

            uint payloadLengthInBytes = BitConverter.ToUInt32(this.payloadSize.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);
            this.Payload = await stream.ReadBytesAsync(payloadLengthInBytes);

            IHashAlgorithm checksumAlgorithm = this.networkParameters.PayloadChecksumAlgorithm;
            IImmutableList<byte> actualChecksum = await Task.Run(() => checksumAlgorithm.CalculateHash(this.Payload));
            if (!this.payloadChecksum.SequenceEqual(actualChecksum.Take(payloadChecksumLengthInBytes)))
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
