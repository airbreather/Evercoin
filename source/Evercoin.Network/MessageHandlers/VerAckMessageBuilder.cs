using System;
using System.Linq;
using System.Text;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class VerAckMessageBuilder
    {
        private const string VerAckText = "verack";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly IRawNetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public VerAckMessageBuilder(IRawNetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(VerAckText))
            {
                throw new ArgumentException("Command length is too short for the \"version\" command.", "network");
            }

            this.network = network;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildVerAckMessage(Guid clientId)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, clientId);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(VerAckText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            message.CreateFrom(commandBytes, Enumerable.Empty<byte>());
            return message;
        }
    }
}
