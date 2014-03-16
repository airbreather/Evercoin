using System;
using System.Collections.Generic;
using System.Diagnostics;

using Evercoin.Util;

namespace Evercoin.Network
{
    [DebuggerDisplay("{System.Text.Encoding.ASCII.GetString(CommandBytes)}, {Evercoin.Util.ByteTwiddling.ByteArrayToHexString(Payload)}")]
    internal sealed class Message : INetworkMessage
    {
        private readonly INetworkParameters networkParameters;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        private readonly INetworkPeer remotePeer;

        private byte[] payloadSize;

        private byte[] payloadChecksum;

        public Message(INetworkParameters networkParameters, IHashAlgorithmStore hashAlgorithmStore, INetworkPeer remotePeer)
        {
            this.networkParameters = networkParameters;
            this.hashAlgorithmStore = hashAlgorithmStore;
            this.remotePeer = remotePeer;
        }

        public byte[] FullData
        {
            get
            {
                return ByteTwiddling.ConcatenateData(this.networkParameters.StaticMessagePrefixData, this.CommandBytes, this.payloadSize, this.payloadChecksum, this.Payload);
            }
        }

        /// <summary>
        /// Gets the command of this message.
        /// </summary>
        public byte[] CommandBytes { get; private set; }

        public byte[] Payload { get; private set; }

        /// <summary>
        /// Gets the network parameters for this message.
        /// </summary>
        public INetworkParameters NetworkParameters { get { return this.networkParameters; } }

        /// <summary>
        /// Gets the ID of the remote client sending or receiving this message.
        /// </summary>
        public INetworkPeer RemotePeer { get { return this.remotePeer; } }

        public void CreateFrom(IEnumerable<byte> command, IEnumerable<byte> payload)
        {
            this.CommandBytes = command.GetArray();
            this.Payload = payload.GetArray();

            uint payloadSizeInBytes = (uint)this.Payload.Length;

            this.payloadSize = BitConverter.GetBytes(payloadSizeInBytes)
                .LittleEndianToOrFromBitConverterEndianness();

            IHashAlgorithm checksumAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(this.networkParameters.PayloadChecksumAlgorithmIdentifier);
            int checksumLengthInBytes = this.networkParameters.PayloadChecksumLengthInBytes;

            byte[] checksum = checksumAlgorithm.CalculateHash(this.Payload);
            Array.Resize(ref checksum, checksumLengthInBytes);
            this.payloadChecksum = checksum;
        }
    }
}
