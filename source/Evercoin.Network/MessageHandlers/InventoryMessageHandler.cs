using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.ProtocolObjects;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class InventoryMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("inv");

        private readonly GetDataMessageBuilder messageBuilder;

        private readonly IReadOnlyChainStore chainStore;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public InventoryMessageHandler(IRawNetwork network, IReadOnlyChainStore chainStore, IHashAlgorithmStore hashAlgorithmStore)
            : base(RecognizedCommand, network)
        {
            this.messageBuilder = new GetDataMessageBuilder(network, hashAlgorithmStore);
            this.chainStore = chainStore;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            Guid clientId = message.RemoteClient;

            ProtocolInventoryVector[] dataNeeded;
            using (MemoryStream payloadStream = new MemoryStream(message.Payload))
            using (ProtocolStreamReader streamReader = new ProtocolStreamReader(payloadStream, true, this.hashAlgorithmStore))
            {
                ulong neededItemCount = await streamReader.ReadCompactSizeAsync(token);
                if (neededItemCount > 50000)
                {
                    return HandledNetworkMessageResult.MessageInvalid;
                }

                dataNeeded = new ProtocolInventoryVector[neededItemCount];

                int itemIndex = 0;
                while (neededItemCount-- > 0)
                {
                    ProtocolInventoryVector vector = await streamReader.ReadInventoryVectorAsync(token);

                    bool skip = true;
                    switch (vector.Type)
                    {
                        case ProtocolInventoryVector.InventoryType.Block:
                            skip = await this.chainStore.ContainsBlockAsync(vector.Hash, token);
                            break;

                        case ProtocolInventoryVector.InventoryType.Transaction:
                            skip = await this.chainStore.ContainsTransactionAsync(vector.Hash, token);
                            break;
                    }

                    if (!skip)
                    {
                        dataNeeded[itemIndex++] = vector;
                    }
                }

                Array.Resize(ref dataNeeded, itemIndex);
            }

            Task t = Task.Run(delegate { }, token);

            // Respond to an "inv" with a "getdata".
            INetworkMessage response = this.messageBuilder.BuildGetDataMessage(clientId, dataNeeded);
            await this.RawNetwork.SendMessageToClientAsync(clientId, response, token);

            return HandledNetworkMessageResult.Okay;
        }
    }
}
