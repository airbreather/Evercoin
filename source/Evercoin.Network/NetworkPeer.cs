using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;

using NodaTime;

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

        /// <summary>
        /// Gets a value indicating whether we've successfully exchanged version
        /// information with this peer at this point in time.
        /// </summary>
        public bool NegotiatedProtocolVersion { get; private set; }

        /// <summary>
        /// Gets the version of the protocol to use when communicating
        /// with this peer.
        /// </summary>
        public int ProtocolVersion { get; private set; }

        /// <summary>
        /// Gets the identifiers of blocks that this peer is aware of.
        /// </summary>
        public HashSet<BigInteger> KnownBlockIdentifiers { get; private set; }

        /// <summary>
        /// Gets the identifiers of transactions that this peer is aware of.
        /// </summary>
        public HashSet<BigInteger> KnownTransactionIdentifiers { get; private set; }

        /// <summary>
        /// Gets the timestamp of the last message we received from this peer.
        /// </summary>
        public Instant LastMessageReceivedTime { get; private set; }

        public IPEndPoint LocalEndPoint { get { return this.localEndPoint; } }

        public IPEndPoint RemoteEndPoint { get { return this.remoteEndPoint; } }
    }
}