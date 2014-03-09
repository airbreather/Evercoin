using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class VersionMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("version");

        private readonly VerAckMessageBuilder verAckMessageBuilder;

        public VersionMessageHandler(INetwork network, IHashAlgorithmStore hashAlgorithmStore)
            : base(RecognizedCommand, network)
        {
            this.verAckMessageBuilder = new VerAckMessageBuilder(network, hashAlgorithmStore);
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            Guid clientId = message.RemoteClient;

            // Respond to a "version" with a "verack".
            INetworkMessage verAckMessage = this.verAckMessageBuilder.BuildVerAckMessage(clientId);
            await this.Network.SendMessageToClientAsync(clientId, verAckMessage, token);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
