using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network
{
    internal sealed class VersionMessageBuilder
    {
        private const string VersionText = "version";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly INetworkParameters networkParameters;

        public VersionMessageBuilder(INetworkParameters networkParameters)
        {
            if (networkParameters.CommandLengthInBytes < CommandEncoding.GetByteCount(VersionText))
            {
                throw new ArgumentException("Command length is too short for the \"version\" command.", "networkParameters");
            }

            this.networkParameters = networkParameters;
        }

        public Message BuildVersionMessage(ulong services,
                                           Instant timestamp,
                                           ProtocolNetworkAddress destinationAddress,
                                           ProtocolNetworkAddress sourceAddress,
                                           ulong nonce,
                                           string userAgent,
                                           int lastBlockReceived,
                                           bool pleaseRelayTransactionsToMe)
        {
            Message message = new Message(this.networkParameters);

            ImmutableList<byte> payload = ImmutableList.CreateRange(BitConverter.GetBytes(this.networkParameters.ProtocolVersion).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(BitConverter.GetBytes(services).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(BitConverter.GetBytes(timestamp.Ticks).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(destinationAddress.Data)
                                                       .AddRange(sourceAddress.Data)
                                                       .AddRange(BitConverter.GetBytes(nonce).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(Encoding.ASCII.GetBytes(userAgent))
                                                       .AddRange(BitConverter.GetBytes(lastBlockReceived).LittleEndianToOrFromBitConverterEndianness())
                                                       .Add(pleaseRelayTransactionsToMe ? (byte)1 : (byte)0);

            byte[] commandBytes = new byte[this.networkParameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(VersionText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            message.CreateFrom(this.networkParameters.StaticMessagePrefixData, commandBytes, payload);

            return message;
        }
    }
}
