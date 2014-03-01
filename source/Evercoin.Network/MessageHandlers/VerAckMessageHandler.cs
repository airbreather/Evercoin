using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class VerAckMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("verack");

        private readonly GetAddressesMessageBuilder getAddressesMessageBuilder;

        private readonly GetBlocksMessageBuilder getBlocksMessageBuilder;

        [ImportingConstructor]
        public VerAckMessageHandler(INetwork network)
            : base(RecognizedCommand, network)
        {
            this.getAddressesMessageBuilder = new GetAddressesMessageBuilder(network);
            this.getBlocksMessageBuilder = new GetBlocksMessageBuilder(network);
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            Guid clientId = message.RemoteClient;

            // Respond to a "verack" with a "getaddr".
            INetworkMessage response = this.getAddressesMessageBuilder.BuildGetAddressesMessage(clientId);

            // except don't for now...
            ////await this.Network.SendMessageToClientAsync(clientId, response, token);

            // ...and then ask for ALL THE BLOCKS!!
            BigInteger block3 = new BigInteger(ByteTwiddling.HexStringToByteArray("000000006A625F06636B8BB6AC7B960A8D03705D1ACE08B1A19DA3FDCC99DDBD"));
            BigInteger block2 = new BigInteger(ByteTwiddling.HexStringToByteArray("00000000839A8E6886AB5951D76F411475428AFC90947EE320161BBF18EB6048"));
            BigInteger block1 = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F"));
            INetworkMessage getBlocksMessage = this.getBlocksMessageBuilder.BuildGetDataMessage(clientId, new BigInteger[] { block3, block2, block1 }, block3);
            await this.Network.SendMessageToClientAsync(clientId, getBlocksMessage, token);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
