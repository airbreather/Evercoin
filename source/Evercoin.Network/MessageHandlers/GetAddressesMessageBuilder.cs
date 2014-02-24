using System;
using System.Collections.Immutable;
using System.Text;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class GetAddressesMessageBuilder
    {
        private const string GetAddressesText = "getaddr";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly Network network;

        public GetAddressesMessageBuilder(INetwork network)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(GetAddressesText))
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

        public INetworkMessage BuildGetAddressesMessage(Guid clientId)
        {
            Message message = new Message(this.network.Parameters, clientId);
            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(GetAddressesText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            message.CreateFrom(commandBytes, ImmutableList<byte>.Empty);

            return message;
        }
    }
}
