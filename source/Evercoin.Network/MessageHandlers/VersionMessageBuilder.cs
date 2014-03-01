using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class VersionMessageBuilder
    {
        private const string VersionText = "version";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly Network network;

        public VersionMessageBuilder(INetwork network)
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
        }

        public INetworkMessage BuildVersionMessage(Guid clientId,
                                                   ulong services,
                                                   Instant timestamp,
                                                   ulong nonce,
                                                   string userAgent,
                                                   int lastBlockReceived,
                                                   bool pleaseRelayTransactionsToMe)
        {
            Message message = new Message(this.network.Parameters, clientId);
            TcpClient client;
            this.network.ClientLookup.TryGetValue(clientId, out client);
            IPEndPoint localEndPoint = (IPEndPoint)client.Client.LocalEndPoint;
            IPEndPoint remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;

            ProtocolNetworkAddress destinationAddress = new ProtocolNetworkAddress(null, services, localEndPoint.Address, (ushort)localEndPoint.Port);
            ProtocolNetworkAddress sourceAddress = new ProtocolNetworkAddress(null, services, remoteEndPoint.Address, (ushort)remoteEndPoint.Port);

            ImmutableList<byte> payload = ImmutableList.CreateRange(BitConverter.GetBytes(this.network.Parameters.ProtocolVersion).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(BitConverter.GetBytes(services).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(BitConverter.GetBytes(timestamp.Ticks).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(destinationAddress.Data)
                                                       .AddRange(sourceAddress.Data)
                                                       .AddRange(BitConverter.GetBytes(nonce).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(new ProtocolString(userAgent, Encoding.ASCII).Data)
                                                       .AddRange(BitConverter.GetBytes(lastBlockReceived).LittleEndianToOrFromBitConverterEndianness())
                                                       .Add(pleaseRelayTransactionsToMe ? (byte)1 : (byte)0);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(VersionText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            message.CreateFrom(commandBytes, payload);

            return message;
        }
    }
}
