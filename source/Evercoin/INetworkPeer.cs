using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;

using NodaTime;

namespace Evercoin
{
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
        /// Gets a value indicating whether we've successfully exchanged version
        /// information with this peer at this point in time.
        /// </summary>
        bool NegotiatedProtocolVersion { get; }

        /// <summary>
        /// Gets the version of the protocol to use when communicating
        /// with this peer.
        /// </summary>
        int ProtocolVersion { get; }

        /// <summary>
        /// Gets the identifiers of blocks that this peer is aware of.
        /// </summary>
        HashSet<BigInteger> KnownBlockIdentifiers { get; }

        /// <summary>
        /// Gets the identifiers of transactions that this peer is aware of.
        /// </summary>
        HashSet<BigInteger> KnownTransactionIdentifiers { get; }

        /// <summary>
        /// Gets the timestamp of the last message we received from this peer.
        /// </summary>
        Instant LastMessageReceivedTime { get; }

        IPEndPoint LocalEndPoint { get; }

        IPEndPoint RemoteEndPoint { get; }
    }
}
