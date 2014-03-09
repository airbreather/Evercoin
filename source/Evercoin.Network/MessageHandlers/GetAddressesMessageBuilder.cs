using System;
using System.Linq;
using System.Text;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class GetAddressesMessageBuilder
    {
        private const string GetAddressesText = "getaddr";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly IRawNetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public GetAddressesMessageBuilder(IRawNetwork network, IHashAlgorithmStore hashAlgorithmStore)
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

            message.CreateFrom(commandBytes, Enumerable.Empty<byte>());

            return message;
        }
    }
}
