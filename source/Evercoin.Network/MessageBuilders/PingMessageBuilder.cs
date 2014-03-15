using System;
using System.Text;

namespace Evercoin.Network.MessageBuilders
{
    internal sealed class PingMessageBuilder
    {
        private const string PingMessageText = "ping";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly IRawNetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public PingMessageBuilder(IRawNetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(PingMessageText))
            {
                throw new ArgumentException("Command length is too short for the \"version\" command.", "network");
            }

            this.network = network;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildPingMessage(Guid clientId, ulong nonce)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, clientId);
            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(PingMessageText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            byte[] pingBytes = BitConverter.GetBytes(nonce).LittleEndianToOrFromBitConverterEndianness();

            message.CreateFrom(commandBytes, pingBytes);

            return message;
        }
    }
}
