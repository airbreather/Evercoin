using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

using Evercoin.Util;

namespace Evercoin.Network
{
    [DebuggerDisplay("{Evercoin.Util.ByteTwiddling.ByteArrayToHexString(FullData)}")]
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
    }
}
