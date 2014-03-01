using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public abstract class MessageHandlerBase : INetworkMessageHandler
    {
        private readonly INetwork network;

        private readonly ImmutableList<byte> commandRecognized;

        private readonly IReadOnlyChainStore readOnlyChainStore;
        private readonly IChainStore chainStore;

        protected MessageHandlerBase(IEnumerable<byte> commandRecognized, INetwork network)
        {
            int commandLengthInBytes = network.Parameters.CommandLengthInBytes;

            byte[] paddedArray = new byte[commandLengthInBytes];
            byte[] unpaddedArray = commandRecognized.ToArray();

            Array.Copy(unpaddedArray, paddedArray, unpaddedArray.Length);
            this.commandRecognized = paddedArray.ToImmutableList();

            this.network = network;
        }

        protected MessageHandlerBase(IEnumerable<byte> commandRecognized, INetwork network, IReadOnlyChainStore chainStore)
            : this(commandRecognized, network)
        {
            this.readOnlyChainStore = chainStore;
        }

        protected MessageHandlerBase(IEnumerable<byte> commandRecognized, INetwork network, IChainStore chainStore)
            : this(commandRecognized, network, (IReadOnlyChainStore)chainStore)
        {
            this.chainStore = chainStore;
        }

        INetworkParameters INetworkMessageHandler.Parameters { get { return this.network.Parameters; } }

        protected INetwork Network { get { return this.network; } }

        protected IChainStore ChainStore { get { return this.chainStore; } }

        protected IReadOnlyChainStore ReadOnlyChainStore { get { return this.readOnlyChainStore; } }

        /// <summary>
        /// Indicates whether or not this handler recognizes a given
        /// <see cref="INetworkMessage"/> as something it can handle.
        /// </summary>
        /// <param name="message">
        /// The <see cref="INetworkMessage"/> to check.
        /// </param>
        /// <returns>
        /// A value indicating whether or not <paramref name="message"/> is
        /// recognized by this handler.
        /// </returns>
        public bool RecognizesMessage(INetworkMessage message)
        {
            return this.network.Parameters.IsCompatibleWith(message.NetworkParameters) &&
                   this.commandRecognized.SequenceEqual(message.CommandBytes);
        }

        public async Task<HandledNetworkMessageResult> HandleMessageAsync(INetworkMessage message)
        {
            if (!this.RecognizesMessage(message))
            {
                return HandledNetworkMessageResult.UnrecognizedCommand;
            }

            return await this.HandleMessageAsyncCore(message, CancellationToken.None);
        }

        public async Task<HandledNetworkMessageResult> HandleMessageAsync(INetworkMessage message, CancellationToken token)
        {
            if (!this.RecognizesMessage(message))
            {
                return HandledNetworkMessageResult.UnrecognizedCommand;
            }

            return await this.HandleMessageAsyncCore(message, token);
        }

        protected abstract Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token);
    }
}
