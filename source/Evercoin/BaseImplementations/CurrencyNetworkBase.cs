using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.ProtocolObjects;

using NodaTime;

namespace Evercoin.BaseImplementations
{
    public abstract class CurrencyNetworkBase : ICurrencyNetwork
    {
        public abstract ICurrencyParameters CurrencyParameters { get; }

        /// <summary>
        /// Gets the list of peers that are currently connected.
        /// </summary>
        public abstract IReadOnlyDictionary<Guid, INetworkPeer> ConnectedPeers { get; }

        /// <summary>
        /// Gets an object that notifies each time a peer is connected.
        /// </summary>
        public abstract IObservable<INetworkPeer> PeerConnections { get; }

        /// <summary>
        /// Gets the identifiers of inventory items we've been offered.
        /// </summary>
        public abstract IObservable<ProtocolInventoryVector[]> ReceivedInventoryOffers { get; }

        /// <summary>
        /// Gets the identifiers of blocks we've been offered.
        /// </summary>
        public abstract IObservable<BigInteger> ReceivedBlockRequests { get; }

        /// <summary>
        /// Gets the identifiers of transactions we've been offered.
        /// </summary>
        public abstract IObservable<BigInteger> ReceivedTransactionRequests { get; }

        /// <summary>
        /// Gets the addresses of peers we've been offered.
        /// </summary>
        public abstract IObservable<ProtocolNetworkAddress> ReceivedPeerOffers { get; }

        /// <summary>
        /// Gets the block messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        public abstract IObservable<ProtocolBlock> ReceivedBlocks { get; }

        /// <summary>
        /// Gets the transaction messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        public abstract IObservable<ProtocolTransaction> ReceivedTransactions { get; }

        /// <summary>
        /// Gets the ping responses we've received.
        /// </summary>
        public abstract IObservable<ProtocolPingResponse> ReceivedPingResponses { get; }

        /// <summary>
        /// Gets the version packages we've received.
        /// </summary>
        public abstract IObservable<Tuple<INetworkPeer, ProtocolVersionPacket>> ReceivedVersionPackets { get; }

        /// <summary>
        /// Gets the peers that have acknowledged that the version exchange is completed.
        /// </summary>
        public abstract IObservable<INetworkPeer> ReceivedVersionAcknowledgements { get; }

        /// <summary>
        /// Asks connected clients to offer us a new pack of blocks.
        /// </summary>
        /// <param name="knownBlockIdentifiers">
        /// The blocks that we already know about.
        /// </param>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedInventoryOffers"/> for responses.
        /// </remarks>
        public Task RequestBlockOffersAsync(IEnumerable<BigInteger> knownBlockIdentifiers)
        {
            return this.RequestBlockOffersAsync(knownBlockIdentifiers, CancellationToken.None);
        }

        /// <summary>
        /// Asks connected clients to offer us a new pack of blocks.
        /// </summary>
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
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedInventoryOffers"/> for responses.
        /// </remarks>
        public abstract Task RequestBlockOffersAsync(IEnumerable<BigInteger> knownBlockIdentifiers, CancellationToken token);

        /// <summary>
        /// Asks connected clients to offer us a new pack
        /// of transactions that are not yet in blocks.
        /// </summary>
        /// <returns>
        /// A task that encapsulates the request.
        /// </returns>
        /// <remarks>
        /// Note that the task only encapsulates the request.  Once it
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedInventoryOffers"/>
        /// for responses.
        /// </remarks>
        public Task RequestTransactionOffersAsync()
        {
            return this.RequestTransactionOffersAsync(CancellationToken.None);
        }

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
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedInventoryOffers"/>
        /// for responses.
        /// </remarks>
        public abstract Task RequestTransactionOffersAsync(CancellationToken token);

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
        public Task RequestInventoryAsync(IEnumerable<ProtocolInventoryVector> inventoryVectors)
        {
            return this.RequestInventoryAsync(inventoryVectors, CancellationToken.None);
        }

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
        public abstract Task RequestInventoryAsync(IEnumerable<ProtocolInventoryVector> inventoryVectors, CancellationToken token);

        /// <summary>
        /// Announces the existence of a block to the connected clients.
        /// </summary>
        /// <param name="blockIdentifier">
        /// The identifier of the block to announce.
        /// </param>
        /// <returns>
        /// A task that encapsulates the announcement.
        /// </returns>
        public Task AnnounceBlockAsync(BigInteger blockIdentifier)
        {
            return this.AnnounceBlockAsync(blockIdentifier, CancellationToken.None);
        }

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
        public abstract Task AnnounceBlockAsync(BigInteger blockIdentifier, CancellationToken token);

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
        public Task SendPingAsync(INetworkPeer peer, ulong nonce)
        {
            return this.SendPingAsync(peer, nonce, CancellationToken.None);
        }

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
        public abstract Task SendPingAsync(INetworkPeer peer, ulong nonce, CancellationToken token);

        /// <summary>
        /// Acknowledges a peer's version.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the acknowledgement to.
        /// </param>
        /// <returns>
        /// A task encapsulating the acknowledgement.
        /// </returns>
        public Task AcknowledgePeerVersionAsync(INetworkPeer peer)
        {
            return this.AcknowledgePeerVersionAsync(peer, CancellationToken.None);
        }

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
        public abstract Task AcknowledgePeerVersionAsync(INetworkPeer peer, CancellationToken token);

        /// <summary>
        /// Attempts to connect to a peer.
        /// </summary>
        /// <param name="peerAddress">
        /// The address of the peer to attempt to connect to.
        /// </param>
        /// <returns>
        /// A task encapsulating the connection result.
        /// </returns>
        public Task<INetworkPeer> ConnectToPeerAsync(ProtocolNetworkAddress peerAddress)
        {
            return this.ConnectToPeerAsync(peerAddress, CancellationToken.None);
        }

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
        public abstract Task<INetworkPeer> ConnectToPeerAsync(ProtocolNetworkAddress peerAddress, CancellationToken token);

        /// <summary>
        /// Announces our version to a peer.
        /// </summary>
        /// <param name="peer">
        /// The peer to send the packet to.
        /// </param>
        /// <returns>
        /// A task encapsulating the connection result.
        /// </returns>
        public Task AnnounceVersionToPeerAsync(INetworkPeer peer, int version, ulong services, Instant timestamp, ulong nonce, string userAgent, int startHeight, bool pleaseRelayTransactionsToMe)
        {
            return this.AnnounceVersionToPeerAsync(peer, version, services, timestamp, nonce, userAgent, startHeight, pleaseRelayTransactionsToMe, CancellationToken.None);
        }

        /// <summary>
        /// Announces our version to a peer.
        /// </summary>
        /// <param name="peer">
        /// The the peer to send the packet to.
        /// </param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to use to signal cancellation.
        /// </param>
        /// <returns>
        /// A task encapsulating the connection result.
        /// </returns>
        public abstract Task AnnounceVersionToPeerAsync(INetworkPeer peer, int version, ulong services, Instant timestamp, ulong nonce, string userAgent, int startHeight, bool pleaseRelayTransactionsToMe, CancellationToken token);

        public void Start()
        {
            this.Start(CancellationToken.None);
        }

        public abstract void Start(CancellationToken token);
    }
}
