using System;
using System.Text;

using Evercoin.ProtocolObjects;

namespace Evercoin.Network.MessageBuilders
{
    internal sealed class VersionMessageBuilder
    {
        private const string VersionText = "version";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly IRawNetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public VersionMessageBuilder(IRawNetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(VersionText))
            {
                throw new ArgumentException("Command length is too short for the \"version\" command.", "network");
            }

            this.network = network;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildVersionMessage(INetworkPeer peer, ProtocolVersionPacket versionPacket)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, peer);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(VersionText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            byte[] payload = versionPacket.Data;

            message.CreateFrom(commandBytes, payload);
            return message;
        }
    }
}
