using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

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

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message)
        {
            Guid clientId = message.RemoteClient;

            ImmutableList<ProtocolInventoryVector> dataNeeded = ImmutableList<ProtocolInventoryVector>.Empty;
            using (MemoryStream payloadStream = new MemoryStream(message.Payload.ToArray()))
            {
                ProtocolCompactSize inventorySize = new ProtocolCompactSize();
                await inventorySize.LoadFromStreamAsync(payloadStream, CancellationToken.None);
                ulong neededItemCount = inventorySize.Value;
                if (neededItemCount > 50000)
                {
                    return HandledNetworkMessageResult.MessageInvalid;
                }

                while (neededItemCount-- > 0)
                {
                    ProtocolInventoryVector vector = new ProtocolInventoryVector();
                    await vector.LoadFromStreamAsync(payloadStream, CancellationToken.None);

                    bool skip = true;
                    switch (vector.Type)
                    {
                        case ProtocolInventoryVector.InventoryType.Block:
                            skip = await this.ReadOnlyChainStore.ContainsBlockAsync(ByteTwiddling.ByteArrayToHexString(vector.Hash));
                            break;

                        case ProtocolInventoryVector.InventoryType.Transaction:
                            skip = await this.ReadOnlyChainStore.ContainsTransactionAsync(ByteTwiddling.ByteArrayToHexString(vector.Hash));
                            break;
                    }

                    ////if (!skip)
                    {
                        dataNeeded = dataNeeded.Add(vector);
                    }
                }
            }

            // Respond to an "inv" with a "getdata".
            INetworkMessage response = this.messageBuilder.BuildGetDataMessage(clientId, dataNeeded.Select(x => x.GetData()));
            await this.Network.SendMessageToClientAsync(clientId, response);
            return HandledNetworkMessageResult.Okay;
        }
    }
}
