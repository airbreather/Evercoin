using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class VerAckMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("verack");

        private readonly GetAddressesMessageBuilder getAddressesMessageBuilder;

        public VerAckMessageHandler(IRawNetwork network, IHashAlgorithmStore hashAlgorithmStore)
            : base(RecognizedCommand, network)
        {
            this.getAddressesMessageBuilder = new GetAddressesMessageBuilder(network, hashAlgorithmStore);
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            Guid clientId = message.RemoteClient;

            // Respond to a "verack" with a "getaddr".
            INetworkMessage response = this.getAddressesMessageBuilder.BuildGetAddressesMessage(clientId);

            // except don't for now...
            ////await this.Network.SendMessageToClientAsync(clientId, response, token);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
