using System;
using System.Net;

namespace Evercoin.Network
{
    internal sealed class NetworkPeer : INetworkPeer
    {
        private readonly Guid identifier;

        private readonly ConnectionDirection direction;

        private readonly IPEndPoint localEndPoint;

        private readonly IPEndPoint remoteEndPoint;

        public NetworkPeer(Guid identifier, ConnectionDirection direction, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            this.identifier = identifier;
            this.direction = direction;
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// Gets the direction of the connection to this peer.
        /// </summary>
        /// <remarks>
        /// <see cref="ConnectionDirection.Incoming"/> = peer connected to us.
        /// <see cref="ConnectionDirection.Outgoing"/> = we connected to peer.
        /// </remarks>
        public ConnectionDirection PeerConnectionDirection { get { return this.direction; } }

        /// <summary>
        /// Gets a value that uniquely identifies this peer.
        /// </summary>
        public Guid Identifier { get { return this.identifier; } }

        public IPEndPoint LocalEndPoint { get { return this.localEndPoint; } }

        public IPEndPoint RemoteEndPoint { get { return this.remoteEndPoint; } }
    }
}