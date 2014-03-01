using System;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class VersionMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("version");

        private readonly VerAckMessageBuilder verAckMessageBuilder;

        [ImportingConstructor]
        public VersionMessageHandler(INetwork network)
            : base(RecognizedCommand, network)
        {
            this.verAckMessageBuilder = new VerAckMessageBuilder(network);
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
