using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public abstract class MessageHandlerBase : INetworkMessageHandler
    {
        private readonly INetwork network;

        private readonly byte[] commandRecognized;

        protected MessageHandlerBase(IEnumerable<byte> commandRecognized, INetwork network)
        {
            int commandLengthInBytes = network.Parameters.CommandLengthInBytes;

            this.commandRecognized = new byte[commandLengthInBytes];
            byte[] unpaddedArray = commandRecognized.GetArray();

            Array.Copy(unpaddedArray, this.commandRecognized, Math.Min(commandLengthInBytes, unpaddedArray.Length));

            this.network = network;
        }

        INetworkParameters INetworkMessageHandler.Parameters { get { return this.network.Parameters; } }

        protected INetwork Network { get { return this.network; } }

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
            return this.network.Parameters.Equals(message.NetworkParameters) &&
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
