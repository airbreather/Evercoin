using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class VersionMessageBuilder
    {
        private const string VersionText = "version";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly Network network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public VersionMessageBuilder(INetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(VersionText))
            {
                throw new ArgumentException("Command length is too short for the \"version\" command.", "network");
            }

            Network realNetwork = network as Network;
            if (realNetwork == null)
            {
                throw new NotSupportedException("Other things not supported yet because lol");
            }

            this.network = realNetwork;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildVersionMessage(Guid clientId,
                                                   ulong services,
                                                   Instant timestamp,
                                                   ulong nonce,
                                                   string userAgent,
                                                   int lastBlockReceived,
                                                   bool pleaseRelayTransactionsToMe)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, clientId);
            TcpClient client;
            this.network.ClientLookup.TryGetValue(clientId, out client);
            IPEndPoint localEndPoint = (IPEndPoint)client.Client.LocalEndPoint;
            IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;

            ProtocolNetworkAddress destinationAddress = new ProtocolNetworkAddress(null, services, localEndPoint.Address, (ushort)localEndPoint.Port);
            ProtocolNetworkAddress sourceAddress = new ProtocolNetworkAddress(null, services, remoteEndPoint.Address, (ushort)remoteEndPoint.Port);

            byte[] versionBytes = BitConverter.GetBytes(this.network.Parameters.ProtocolVersion).LittleEndianToOrFromBitConverterEndianness();
            byte[] servicesBytes = BitConverter.GetBytes(services).LittleEndianToOrFromBitConverterEndianness();
            byte[] timestampBytes = BitConverter.GetBytes(timestamp.Ticks).LittleEndianToOrFromBitConverterEndianness();
            byte[] destinationAddressBytes = destinationAddress.Data;
            byte[] sourceAddressBytes = sourceAddress.Data;
            byte[] nonceBytes = BitConverter.GetBytes(nonce).LittleEndianToOrFromBitConverterEndianness();
            byte[] userAgentBytes = new ProtocolString(userAgent, Encoding.ASCII).Data;
            byte[] lastBlockReceivedBytes = BitConverter.GetBytes(lastBlockReceived).LittleEndianToOrFromBitConverterEndianness();
            byte[] relayTransactionsBytes = { pleaseRelayTransactionsToMe ? (byte)1 : (byte)0 };

            byte[] payload = ByteTwiddling.ConcatenateData(versionBytes, servicesBytes, timestampBytes, destinationAddressBytes, sourceAddressBytes, nonceBytes, userAgentBytes, lastBlockReceivedBytes, relayTransactionsBytes);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(VersionText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            message.CreateFrom(commandBytes, payload);

            return message;
        }
    }
}
