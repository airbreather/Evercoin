using System;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class VersionMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("version");

        private readonly VerAckMessageBuilder messageBuilder;

        [ImportingConstructor]
        public VersionMessageHandler(INetwork network)
            : base(RecognizedCommand, network)
        {
            this.messageBuilder = new VerAckMessageBuilder(network);
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message)
        {
            Guid clientId = message.RemoteClient;

            // Respond to a "version" with a "verack".
            INetworkMessage response = this.messageBuilder.BuildVerAckMessage(clientId);
            await this.Network.SendMessageToClientAsync(clientId, response);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
