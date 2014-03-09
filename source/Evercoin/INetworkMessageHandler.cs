using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// A handler for <see cref="INetworkMessage"/> objects received
    /// over an <see cref="IRawNetwork"/>.
    /// </summary>
    public interface INetworkMessageHandler
    {
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
        bool RecognizesMessage(INetworkMessage message);

        /// <summary>
        /// Asynchronously handles an <see cref="INetworkMessage"/> received.
        /// </summary>
        /// <param name="message">
        /// The message that was received.
        /// </param>
        /// <returns>
        /// A task encapsulating the asynchronous operation.
        /// </returns>
        Task<HandledNetworkMessageResult> HandleMessageAsync(INetworkMessage message);

        /// <summary>
        /// Asynchronously handles an <see cref="INetworkMessage"/> received.
        /// </summary>
        /// <param name="message">
        /// The message that was received.
        /// </param>
        /// <returns>
        /// A task encapsulating the asynchronous operation.
        /// </returns>
        Task<HandledNetworkMessageResult> HandleMessageAsync(INetworkMessage message, CancellationToken token);
    }
}
