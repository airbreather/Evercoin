using System;
using System.Net;

namespace Evercoin
{
    /// <summary>
    /// Represents a network peer that we're connected to.
    /// </summary>
    public interface INetworkPeer
    {
        /// <summary>
        /// Gets the direction of the connection to this peer.
        /// </summary>
        /// <remarks>
        /// <see cref="ConnectionDirection.Incoming"/> = peer connected to us.
        /// <see cref="ConnectionDirection.Outgoing"/> = we connected to peer.
        /// </remarks>
        ConnectionDirection PeerConnectionDirection { get; }

        /// <summary>
        /// Gets a value that uniquely identifies this peer.
        /// </summary>
        Guid Identifier { get; }

        /// <summary>
        /// Gets the endpoint of the local peer.
        /// TODO: abstraction!
        /// </summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the endpoint of the remote peer.
        /// TODO: abstraction!
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }
    }
}
