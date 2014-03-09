using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class TransactionMessageHandler : MessageHandlerBase
    {
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("tx");

        private readonly GetDataMessageBuilder builder;

        private readonly IChainStore chainStore;

        public TransactionMessageHandler(INetwork network, IChainStore chainStore, IHashAlgorithmStore hashAlgorithmStore)
            : base(RecognizedCommand, network)
        {
            this.builder = new GetDataMessageBuilder(network, hashAlgorithmStore);
            this.chainStore = chainStore;
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            // Nuked old stuff
            return HandledNetworkMessageResult.Okay;
        }
    }
}
