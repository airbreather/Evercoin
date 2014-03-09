using System;
using System.Collections.Immutable;
using System.Text;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class GetAddressesMessageBuilder
    {
        private const string GetAddressesText = "getaddr";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly INetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public GetAddressesMessageBuilder(INetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(GetAddressesText))
            {
                throw new ArgumentException("Command length is too short for the \"version\" command.", "network");
            }

            this.network = network;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildGetAddressesMessage(Guid clientId)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, clientId);
            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(GetAddressesText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            message.CreateFrom(commandBytes, ImmutableList<byte>.Empty);

            return message;
        }
    }
}
