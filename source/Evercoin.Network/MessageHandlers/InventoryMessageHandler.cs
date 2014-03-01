using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class InventoryMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("inv");

        private readonly GetDataMessageBuilder messageBuilder;

        [ImportingConstructor]
        public InventoryMessageHandler(INetwork network, IReadOnlyChainStore chainStore)
            : base(RecognizedCommand, network, chainStore)
        {
            this.messageBuilder = new GetDataMessageBuilder(network);
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            Guid clientId = message.RemoteClient;

            ImmutableList<ProtocolInventoryVector> dataNeeded = ImmutableList<ProtocolInventoryVector>.Empty;
            using (MemoryStream payloadStream = new MemoryStream(message.Payload.ToArray()))
            using (ProtocolStreamReader streamReader = new ProtocolStreamReader(payloadStream, leaveOpen: true))
            {
                ulong neededItemCount = await streamReader.ReadCompactSizeAsync(token);
                if (neededItemCount > 50000)
                {
                    return HandledNetworkMessageResult.MessageInvalid;
                }

                while (neededItemCount-- > 0)
                {
                    ProtocolInventoryVector vector = await streamReader.ReadInventoryVectorAsync(token);

                    bool skip = true;
                    switch (vector.Type)
                    {
                        case ProtocolInventoryVector.InventoryType.Block:
                            skip = await this.ReadOnlyChainStore.ContainsBlockAsync(vector.Hash, token);
                            break;

                        case ProtocolInventoryVector.InventoryType.Transaction:
                            skip = await this.ReadOnlyChainStore.ContainsTransactionAsync(vector.Hash, token);
                            break;
                    }

                    if (!skip)
                    {
                        dataNeeded = dataNeeded.Add(vector);
                    }
                }
            }

            // Respond to an "inv" with a "getdata".
            INetworkMessage response = this.messageBuilder.BuildGetDataMessage(clientId, dataNeeded);
            await this.Network.SendMessageToClientAsync(clientId, response, token);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
