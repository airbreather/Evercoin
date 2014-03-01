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

            // ...and then ask for ALL THE BLOCKS!!  Well, kind of.
            // Let's just start off by asking for a few.
            // This is block at height=3 in the Bitcoin main chain:
            BigInteger block3 = new BigInteger(ByteTwiddling.HexStringToByteArray("82B5015589A3FDF2D4BAFF403E6F0BE035A5D9742C1CAE6295464449").Reverse().ToArray());

            // This is the block at height=2 in the Bitcoin main chain:
            ////BigInteger block2 = new BigInteger(ByteTwiddling.HexStringToByteArray("6A625F06636B8BB6AC7B960A8D03705D1ACE08B1A19DA3FDCC99DDBD").Reverse().ToArray());

            // And these are height=1 and height=0:
            BigInteger block1 = new BigInteger(ByteTwiddling.HexStringToByteArray("839A8E6886AB5951D76F411475428AFC90947EE320161BBF18EB6048").Reverse().ToArray());
            BigInteger block0 = new BigInteger(ByteTwiddling.HexStringToByteArray("19D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").Reverse().ToArray());

            // So we'll ask the remote node:
            // "Hey, I know about everything from height=0 to height=1.
            // Give me everything between that and height=3?"
            INetworkMessage getBlocksMessage = this.getBlocksMessageBuilder.BuildGetDataMessage(clientId, new[] { block1, block0 }, block3);
            await this.Network.SendMessageToClientAsync(clientId, getBlocksMessage, token);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
