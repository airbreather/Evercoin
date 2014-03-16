using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    /// <summary>
    /// Encapsulates the raw messages that pass through the network,
    /// as well as the peers that send and receive those messages.
    /// </summary>
    public interface IRawNetwork
    {
        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> object that defines the
        /// parameters that this network uses.
        /// </summary>
        INetworkParameters Parameters { get; }

        /// <summary>
        /// Gets an observable sequence of messages received on this network.
        /// </summary>
        IObservable<INetworkMessage> ReceivedMessages { get; }

        /// <summary>
        /// Gets an observable sequence of 
        /// </summary>
        IObservable<INetworkPeer> PeerConnections { get; }

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <returns>
        /// A task that encapsulates the connection request.
        /// </returns>
        Task ConnectToClientAsync(IPEndPoint endPoint);

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task that encapsulates the connection request.
        /// </returns>
        Task ConnectToClientAsync(IPEndPoint endPoint, CancellationToken token);

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <returns>
        /// A task that encapsulates the connection request.
        /// </returns>
        Task ConnectToClientAsync(DnsEndPoint endPoint);

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task that encapsulates the connection request.
        /// </returns>
        Task ConnectToClientAsync(DnsEndPoint endPoint, CancellationToken token);

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
        /// Asynchronously broadcasts a message to all clients on the network.
        /// </summary>
        /// <param name="message">
        /// The <see cref="INetworkMessage"/> to broadcast.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// <see cref="INetworkMessage.RemoteClient"/> is ignored.
        /// </remarks>
        Task BroadcastMessageAsync(INetworkMessage message, CancellationToken token);

        /// <summary>
        /// Asynchronously sends a message to a single client on the network.
        /// </summary>
        /// <param name="peer">
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
        Task SendMessageToClientAsync(INetworkPeer peer, INetworkMessage message);

        /// <summary>
        /// Asynchronously sends a message to a single client on the network.
        /// </summary>
        /// <param name="peer">
        /// The ID of the client to send the message to.
        /// </param>
        /// <param name="message">
        /// The <see cref="INetworkMessage"/> to send.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// <see cref="INetworkMessage.RemoteClient"/> is ignored.
        /// </remarks>
        Task SendMessageToClientAsync(INetworkPeer peer, INetworkMessage message, CancellationToken token);
    }
}
