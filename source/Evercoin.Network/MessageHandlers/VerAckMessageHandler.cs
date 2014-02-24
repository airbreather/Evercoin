using System;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class VerAckMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("verack");

        private readonly GetAddressesMessageBuilder messageBuilder;

        [ImportingConstructor]
        public VerAckMessageHandler(INetwork network)
            : base(RecognizedCommand, network)
        {
            this.messageBuilder = new GetAddressesMessageBuilder(network);
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message)
        {
            Guid clientId = message.RemoteClient;

            // Respond to a "verack" with a "getaddr".
            INetworkMessage response = this.messageBuilder.BuildGetAddressesMessage(clientId);

            // except don't for now...
            ////await this.Network.SendMessageToClientAsync(clientId, response);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
