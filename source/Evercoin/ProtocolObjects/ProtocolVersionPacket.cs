using System;
using System.Text;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolVersionPacket
    {
        private readonly int version;

        private readonly ulong services;

        private readonly Instant timestamp;

        private readonly ProtocolNetworkAddress receivingAddress;

        private readonly ProtocolNetworkAddress sendingAddress;

        private readonly ulong nonce;

        private readonly string userAgent;

        private readonly int startHeight;

        private readonly bool pleaseRelayTransactionsToMe;

        public ProtocolVersionPacket(int version, ulong services, Instant timestamp, ProtocolNetworkAddress receivingAddress, ProtocolNetworkAddress sendingAddress, ulong nonce, string userAgent, int startHeight, bool pleaseRelayTransactionsToMe)
        {
            this.version = version;
            this.services = services;
            this.timestamp = timestamp;
            this.receivingAddress = receivingAddress;
            this.sendingAddress = sendingAddress;
            this.nonce = nonce;
            this.userAgent = userAgent;
            this.startHeight = startHeight;
            this.pleaseRelayTransactionsToMe = pleaseRelayTransactionsToMe;
        }

        public int Version { get { return this.version; } }

        public ulong Services { get { return this.services; } }

        public Instant Timestamp { get { return this.timestamp; } }

        public ProtocolNetworkAddress ReceivingAddress { get { return this.receivingAddress; } }

        public ProtocolNetworkAddress SendingAddress { get { return this.sendingAddress; } }

        public ulong Nonce { get { return this.nonce; } }

        public string UserAgent { get { return this.userAgent; } }

        public int StartHeight { get { return this.startHeight; } }

        public bool PleaseRelayTransactionsToMe { get { return this.pleaseRelayTransactionsToMe; } }

        public byte[] Data
        {
            get
            {
                byte[] versionBytes = BitConverter.GetBytes(this.Version).LittleEndianToOrFromBitConverterEndianness();
                byte[] servicesBytes = BitConverter.GetBytes(this.Services).LittleEndianToOrFromBitConverterEndianness();
                byte[] timestampBytes = BitConverter.GetBytes((long)(this.Timestamp - Instant.FromTicksSinceUnixEpoch(0)).ToTimeSpan().TotalSeconds).LittleEndianToOrFromBitConverterEndianness();
                byte[] receivingAddressBytes = this.ReceivingAddress.Data;
                byte[] sendingAddressBytes = this.SendingAddress.Data;
                byte[] nonceBytes = BitConverter.GetBytes(this.Nonce).LittleEndianToOrFromBitConverterEndianness();
                byte[] userAgentBytes = new ProtocolString(this.UserAgent, Encoding.ASCII).Data;
                byte[] startHeightBytes = BitConverter.GetBytes(this.StartHeight).LittleEndianToOrFromBitConverterEndianness();
                byte[] pleaseRelayTransactionsToMeBytes = BitConverter.GetBytes(this.PleaseRelayTransactionsToMe).LittleEndianToOrFromBitConverterEndianness();

                return ByteTwiddling.ConcatenateData(versionBytes, servicesBytes, timestampBytes, receivingAddressBytes, sendingAddressBytes, nonceBytes, userAgentBytes, startHeightBytes, pleaseRelayTransactionsToMeBytes);
            }
        }
    }
}
