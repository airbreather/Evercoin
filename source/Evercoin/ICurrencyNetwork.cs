using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.ProtocolObjects;

using NodaTime;

namespace Evercoin
{
    /// <summary>
    /// Represents a dynamic view of the currency network.
    /// Implementations are likely to use <see cref="IRawNetwork"/>.
    /// </summary>
    /// <remarks>
    /// TODO: make this "intended" part a reality.
    /// This is actually intended to work for both SPV and Full-Node operation,
    /// even in the filtering mode specified in BIP 37, via "dummy"
    /// placeholder transactions that only have just enough data to fit into
    /// the Merkle tree.
    /// </remarks>
    public interface ICurrencyNetwork
    {
        /// <summary>
        /// Gets the parameters that define this currency.
        /// </summary>
        ICurrencyParameters CurrencyParameters { get; }

        /// <summary>
        /// Gets the list of peers that are currently connected.
        /// </summary>
        IReadOnlyDictionary<Guid, INetworkPeer> ConnectedPeers { get; }

        /// <summary>
        /// Gets an object that notifies each time a peer is connected.
        /// </summary>
        IObservable<INetworkPeer> PeerConnections { get; }

        /// <summary>
        /// Gets the identifiers of inventory items we've been offered.
        /// </summary>
        IObservable<Tuple<INetworkPeer, ProtocolInventoryVector[]>> ReceivedInventoryOffers { get; }

        /// <summary>
        /// Gets the identifiers of blocks we've been offered.
        /// </summary>
        IObservable<Tuple<INetworkPeer, ProtocolInventoryVector[]>> ReceivedInventoryRequests { get; }

        /// <summary>
        /// Gets the addresses of peers we've been offered.
        /// </summary>
        IObservable<ProtocolNetworkAddress> ReceivedPeerOffers { get; }

        /// <summary>
        /// Gets the block messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        IObservable<Tuple<INetworkPeer, ProtocolBlock>> ReceivedBlocks { get; }

        /// <summary>
        /// Gets the transaction messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        IObservable<Tuple<INetworkPeer, ProtocolTransaction>> ReceivedTransactions { get; }

        /// <summary>
        /// Gets the ping responses we've received.
        /// </summary>
        IObservable<Tuple<INetworkPeer, ulong>> ReceivedPingResponses { get; }

        /// <summary>
        /// Gets the version packages we've received.
        /// </summary>
        IObservable<Tuple<INetworkPeer, ProtocolVersionPacket>> ReceivedVersionPackets { get; }

        /// <summary>
        /// Gets the peers that have acknowledged that the version exchange is completed.
        /// </summary>
        IObservable<INetworkPeer> ReceivedVersionAcknowledgements { get; }

        /// <summary>
        /// Asks a connected client to offer us a new pack of blocks.
        /// </summary>
        /// <param name="peer">
        /// The peer to request the block offers from.
        /// </param>
        /// <param name="knownBlockIdentifiers">
        /// The blocks that we already know about.
        /// </param>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ReceivedInventoryOffers"/>
        /// for responses.
        /// </remarks>
        Task RequestBlockOffersAsync(INetworkPeer peer, IEnumerable<BigInteger> knownBlockIdentifiers);

        /// <summary>
        /// Asks a connected client to offer us a new pack of blocks.
        /// </summary>
        /// <param name="peer">
        /// The peer to request the block offers from.
        /// </param>
        /// <param name="knownBlockIdentifiers">
        /// The blocks that we already know about.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ReceivedInventoryOffers"/>
        /// for responses.
        /// </remarks>
        Task RequestBlockOffersAsync(INetworkPeer peer, IEnumerable<BigInteger> knownBlockIdentifiers, CancellationToken token);

        /// <summary>
        /// Asks connected clients to offer us a new pack
        /// of transactions that are not yet in blocks.
        /// </summary>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ReceivedInventoryOffers"/>
        /// for responses.
        /// </remarks>
        Task RequestTransactionOffersAsync();

        /// <summary>
        /// Asks connected clients to offer us a new pack
        /// of transactions that are not yet in blocks.
        /// </summary>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ReceivedInventoryOffers"/>
        /// for responses.
        /// </remarks>
        Task RequestTransactionOffersAsync(CancellationToken token);

        /// <summary>
        /// Asks connected clients for the given inventory data.
        /// </summary>
        /// <param name="inventoryVectors">
        /// The inventory data to ask for.
        /// </param>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedBlocks"/>
        /// or <see cref="ICurrencyNetwork.ReceivedTransactions"/>.
        /// </remarks>
        Task RequestInventoryAsync(IEnumerable<ProtocolInventoryVector> inventoryVectors);

        /// <summary>
        /// Asks connected clients for the given inventory data.
        /// </summary>
        /// <param name="inventoryVectors">
        /// The inventory data to ask for.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedBlocks"/>
        /// or <see cref="ICurrencyNetwork.ReceivedTransactions"/>.
        /// </remarks>
        Task RequestInventoryAsync(IEnumerable<ProtocolInventoryVector> inventoryVectors, CancellationToken token);

        /// <summary>
        /// Announces the existence of a block to the connected clients.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to announce.
        /// </param>
        /// <returns>
        /// A task that encapsulates the announcement.
        /// </returns>
        Task AnnounceBlockAsync(BigInteger blockIdentifier);

        /// <summary>
        /// Announces the existence of a block to the connected clients.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to announce.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task that encapsulates the announcement.
        /// </returns>
        Task AnnounceBlockAsync(BigInteger blockIdentifier, CancellationToken token);

        /// <summary>
        /// Sends a ping request to a peer.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the ping request to.
        /// </param>
        /// <param name="nonce">
        /// A random value to send with the ping.
        /// </param>
        /// <returns>
        /// A task encapsulating the ping request.
        /// </returns>
        Task SendPingAsync(INetworkPeer peer, ulong nonce);

        /// <summary>
        /// Sends a ping request to a peer.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the ping request to.
        /// </param>
        /// <param name="nonce">
        /// A random value to send with the ping.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task encapsulating the ping request.
        /// </returns>
        Task SendPingAsync(INetworkPeer peer, ulong nonce, CancellationToken token);

        /// <summary>
        /// Acknowledges a peer's version.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the acknowledgement to.
        /// </param>
        /// <returns>
        /// A task encapsulating the acknowledgement.
        /// </returns>
        Task AcknowledgePeerVersionAsync(INetworkPeer peer);

        /// <summary>
        /// Acknowledges a peer's version.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the acknowledgement to.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task encapsulating the acknowledgement.
        /// </returns>
        Task AcknowledgePeerVersionAsync(INetworkPeer peer, CancellationToken token);

        /// <summary>
        /// Attempts to connect to a peer.
        /// </summary>
        /// <param name="peerAddress">
        /// The address of the peer to attempt to connect to.
        /// </param>
        /// <returns>
        /// A task encapsulating the connection result.
        /// </returns>
        Task ConnectToPeerAsync(ProtocolNetworkAddress peerAddress);

        /// <summary>
        /// Attempts to connect to a peer.
        /// </summary>
        /// <param name="peerAddress">
        /// The address of the peer to attempt to connect to.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task encapsulating the connection result.
        /// </returns>
        Task ConnectToPeerAsync(ProtocolNetworkAddress peerAddress, CancellationToken token);

        /// <summary>
        /// Announces our version to a peer.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the packet to.
        /// </param>
        /// <returns>
        /// A task encapsulating the connection result.
        /// </returns>
        Task AnnounceVersionToPeerAsync(INetworkPeer peer, int version, ulong services, Instant timestamp, ulong nonce, string userAgent, int startHeight, bool pleaseRelayTransactionsToMe);

        /// <summary>
        /// Announces our version to a peer.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the packet to.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task encapsulating the connection result.
        /// </returns>
        Task AnnounceVersionToPeerAsync(INetworkPeer peer, int version, ulong services, Instant timestamp, ulong nonce, string userAgent, int startHeight, bool pleaseRelayTransactionsToMe, CancellationToken token);

        void Start();

        void Start(CancellationToken token);
    }
}
