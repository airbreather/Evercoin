using System;
using System.Net;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Represents the observable cryptocurrency network.
    /// </summary>
    /// <remarks>
    /// TODO: Remove this remark.
    /// This is actually intended to work for both SPV and Full-Node operation,
    /// even in the filtering mode specified in BIP 37, via "dummy"
    /// placeholder transactions that only have just enough data to fit into
    /// the Merkle tree.
    /// </remarks>
    public interface INetwork : IDisposable
    {
        /// <summary>
        /// Gets an observable sequence of messages received on this network.
        /// </summary>
        IObservable<INetworkMessage> ReceivedMessages { get; }

        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> object that defines the
        /// parameters that this network uses.
        /// </summary>
        INetworkParameters Parameters { get; }

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <returns>
        /// An awaitable task that yields the ID of the connected client.
        /// </returns>
        Task<Guid> ConnectToClientAsync(IPEndPoint endPoint);

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <returns>
        /// An awaitable task that yields the ID of the connected client.
        /// </returns>
        Task<Guid> ConnectToClientAsync(DnsEndPoint endPoint);

        /// <summary>
        /// Asynchronously broadcasts a message to all clients on the network.
        /// </summary>
        /// <param name="message">
        /// The <see cref="INetworkMessage"/> to broadcast.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// <see cref="INetworkMessage.RemoteClient"/> is ignored.
        /// </remarks>
        Task BroadcastMessageAsync(INetworkMessage message);

        /// <summary>
        /// Asynchronously sends a message to a single client on the network.
        /// </summary>
        /// <param name="clientId">
        /// The ID of the client to send the message to.
        /// </param>
        /// <param name="message">
        /// The <see cref="INetworkMessage"/> to send.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// <see cref="INetworkMessage.RemoteClient"/> is ignored.
        /// </remarks>
        Task SendMessageToClientAsync(Guid clientId, INetworkMessage message);
    }
}
