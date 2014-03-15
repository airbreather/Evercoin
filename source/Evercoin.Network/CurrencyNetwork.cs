using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Network.MessageBuilders;
using Evercoin.ProtocolObjects;

using NodaTime;

namespace Evercoin.Network
{
    public sealed class CurrencyNetwork : CurrencyNetworkBase
    {
        private readonly ICurrencyParameters currencyParameters;

        private readonly IRawNetwork rawNetwork;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        private readonly ConcurrentDictionary<Guid, INetworkPeer> connectedPeers = new ConcurrentDictionary<Guid, INetworkPeer>();

        private readonly Subject<INetworkPeer> peerConnections = new Subject<INetworkPeer>();

        private readonly Subject<ProtocolInventoryVector[]> inventoryOffers = new Subject<ProtocolInventoryVector[]>();

        private readonly Subject<ProtocolBlock> blocksReceived = new Subject<ProtocolBlock>();

        private readonly Subject<ProtocolTransaction> transactionsReceived = new Subject<ProtocolTransaction>();

        private readonly Subject<Tuple<INetworkPeer, ProtocolVersionPacket>> versionOffersReceived = new Subject<Tuple<INetworkPeer, ProtocolVersionPacket>>();

        private readonly Subject<INetworkPeer> versionAcknowledgementsReceived = new Subject<INetworkPeer>();

        public CurrencyNetwork(IRawNetwork rawNetwork, IHashAlgorithmStore hashAlgorithmStore, ICurrencyParameters currencyParameters)
        {
            this.rawNetwork = rawNetwork;
            this.hashAlgorithmStore = hashAlgorithmStore;
            this.currencyParameters = currencyParameters;
        }

        public override ICurrencyParameters CurrencyParameters { get { return this.currencyParameters; } }

        /// <summary>
        /// Gets the list of peers that are currently connected.
        /// </summary>
        public override IReadOnlyDictionary<Guid, INetworkPeer> ConnectedPeers { get { return new ReadOnlyDictionary<Guid, INetworkPeer>(this.connectedPeers); } }

        /// <summary>
        /// Gets an object that notifies each time a peer is connected.
        /// </summary>
        public override IObservable<INetworkPeer> PeerConnections { get { return this.peerConnections; } }

        /// <summary>
        /// Gets the identifiers of inventory items we've been offered.
        /// </summary>
        public override IObservable<ProtocolInventoryVector[]> ReceivedInventoryOffers { get { return this.inventoryOffers; } }

        /// <summary>
        /// Gets the identifiers of blocks we've been offered.
        /// </summary>
        public override IObservable<BigInteger> ReceivedBlockRequests
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the identifiers of transactions we've been offered.
        /// </summary>
        public override IObservable<BigInteger> ReceivedTransactionRequests
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the addresses of peers we've been offered.
        /// </summary>
        public override IObservable<ProtocolNetworkAddress> ReceivedPeerOffers
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the block messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        public override IObservable<ProtocolBlock> ReceivedBlocks { get { return this.blocksReceived; } }

        /// <summary>
        /// Gets the transaction messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        public override IObservable<ProtocolTransaction> ReceivedTransactions { get { return this.transactionsReceived; } }

        /// <summary>
        /// Gets the ping responses we've received.
        /// </summary>
        public override IObservable<ProtocolPingResponse> ReceivedPingResponses
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the version packages we've received.
        /// </summary>
        public override IObservable<Tuple<INetworkPeer, ProtocolVersionPacket>> ReceivedVersionPackets { get { return this.versionOffersReceived; } }

        /// <summary>
        /// Gets the peers that have acknowledged that the version exchange is completed.
        /// </summary>
        public override IObservable<INetworkPeer> ReceivedVersionAcknowledgements { get { return this.versionAcknowledgementsReceived; } }

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
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedBlockOffers"/> for responses.
        /// </remarks>
        public override async Task RequestBlockOffersAsync(IEnumerable<BigInteger> knownBlockIdentifiers, CancellationToken token)
        {
            BigInteger[] blockIdentifierCollection = knownBlockIdentifiers.GetArray();
            List<BigInteger> listToRequest = new List<BigInteger>();

            int step = 1;
            int start = 0;
            for (int i = blockIdentifierCollection.Length - 1; i > 0; start++, i -= step)
            {
                if (start >= 10)
                {
                    step *= 2;
                }

                listToRequest.Add(blockIdentifierCollection[i]);
            }

            listToRequest.Add(blockIdentifierCollection[0]);

            GetBlocksMessageBuilder b = new GetBlocksMessageBuilder(this.rawNetwork, this.hashAlgorithmStore);
            var message = b.BuildGetBlocksMessage(Guid.NewGuid(), listToRequest, BigInteger.Zero);
            await this.rawNetwork.BroadcastMessageAsync(message, token);
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
        /// completes, observe <see cref="ICurrencyNetwork.ReceivedTransactionOffers"/>
        /// for responses.
        /// </remarks>
        public override Task RequestTransactionOffersAsync(CancellationToken token)
        {
            throw new NotImplementedException();
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
        public override async Task RequestInventoryAsync(IEnumerable<ProtocolInventoryVector> inventoryVectors, CancellationToken token)
        {
            Guid clientId = this.ConnectedPeers.First().Key;

            INetworkMessage response = new GetDataMessageBuilder(this.rawNetwork, this.hashAlgorithmStore).BuildGetDataMessage(clientId, inventoryVectors);
            await this.rawNetwork.SendMessageToClientAsync(clientId, response, token);
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
        public override Task AnnounceBlockAsync(BigInteger blockIdentifier, CancellationToken token)
        {
            throw new NotImplementedException();
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
        public override async Task SendPingAsync(INetworkPeer peer, ulong nonce, CancellationToken token)
        {
            PingMessageBuilder messageBuilder = new PingMessageBuilder(this.rawNetwork, this.hashAlgorithmStore);
            INetworkMessage message = messageBuilder.BuildPingMessage(peer.Identifier, nonce);
            await this.rawNetwork.SendMessageToClientAsync(peer.Identifier, message, token);
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
        public override async Task AcknowledgePeerVersionAsync(INetworkPeer peer, CancellationToken token)
        {
            VerAckMessageBuilder builder = new VerAckMessageBuilder(this.rawNetwork, this.hashAlgorithmStore);
            INetworkMessage response = builder.BuildVerAckMessage(peer.Identifier);
            await this.rawNetwork.SendMessageToClientAsync(peer.Identifier, response, token);
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
        public override async Task<INetworkPeer> ConnectToPeerAsync(ProtocolNetworkAddress peerAddress, CancellationToken token)
        {
            IPAddress address = peerAddress.Address;
            ushort port = peerAddress.Port;

            IPEndPoint endPoint = new IPEndPoint(address, port);
            Guid peerIdentifier = await this.rawNetwork.ConnectToClientAsync(endPoint, token);
            if (peerIdentifier == Guid.Empty)
            {
                return null;
            }

            TcpClient client = this.rawNetwork.GetClientButPleaseBeNice(peerIdentifier);
            NetworkPeer peer = new NetworkPeer(peerIdentifier, ConnectionDirection.Outgoing, (IPEndPoint)client.Client.LocalEndPoint, (IPEndPoint)client.Client.RemoteEndPoint);
            this.connectedPeers[peerIdentifier] = peer;
            this.peerConnections.OnNext(peer);
            return peer;
        }

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
        public override async Task AnnounceVersionToPeerAsync(INetworkPeer peer, int version, ulong services, Instant timestamp, ulong nonce, string userAgent, int startHeight, bool pleaseRelayTransactionsToMe, CancellationToken token)
        {
            ProtocolNetworkAddress sendingAddress = new ProtocolNetworkAddress(null, 1, peer.LocalEndPoint.Address, (ushort)peer.LocalEndPoint.Port);
            ProtocolNetworkAddress receivingAddress = new ProtocolNetworkAddress(null, 1, peer.RemoteEndPoint.Address, (ushort)peer.RemoteEndPoint.Port);
            ProtocolVersionPacket packet = new ProtocolVersionPacket(version, services, timestamp, receivingAddress, sendingAddress, nonce, userAgent, startHeight, pleaseRelayTransactionsToMe);

            VersionMessageBuilder messageBuilder = new VersionMessageBuilder(this.rawNetwork, this.hashAlgorithmStore);
            INetworkMessage message = messageBuilder.BuildVersionMessage(peer.Identifier, packet);
            await this.rawNetwork.SendMessageToClientAsync(peer.Identifier, message, token);
        }

        public override void Start(CancellationToken token)
        {
            this.rawNetwork.ReceivedConnections.Subscribe
            (
                x =>
                {
                    TcpClient client = this.rawNetwork.GetClientButPleaseBeNice(x);
                    NetworkPeer peer = new NetworkPeer(x, ConnectionDirection.Incoming, (IPEndPoint)client.Client.LocalEndPoint, (IPEndPoint)client.Client.RemoteEndPoint);
                    this.peerConnections.OnNext(this.connectedPeers[x] = peer);
                },
                this.peerConnections.OnError,
                this.peerConnections.OnCompleted,
                token
            );

            byte[] invBytes = Encoding.ASCII.GetBytes("inv");
            byte[] blockBytes = Encoding.ASCII.GetBytes("block");
            byte[] txBytes = Encoding.ASCII.GetBytes("tx");
            byte[] versionBytes = Encoding.ASCII.GetBytes("version");
            byte[] verackBytes = Encoding.ASCII.GetBytes("verack");

            Array.Resize(ref invBytes, this.CurrencyParameters.NetworkParameters.CommandLengthInBytes);
            Array.Resize(ref blockBytes, this.CurrencyParameters.NetworkParameters.CommandLengthInBytes);
            Array.Resize(ref txBytes, this.CurrencyParameters.NetworkParameters.CommandLengthInBytes);
            Array.Resize(ref versionBytes, this.CurrencyParameters.NetworkParameters.CommandLengthInBytes);
            Array.Resize(ref verackBytes, this.CurrencyParameters.NetworkParameters.CommandLengthInBytes);

            this.rawNetwork.ReceivedMessages.Subscribe
            (
                async x =>
                {
                    if (!x.CommandBytes.SequenceEqual(invBytes))
                    {
                        return;
                    }

                    List<ProtocolInventoryVector> inventoryVectors = new List<ProtocolInventoryVector>();

                    using (MemoryStream stream = new MemoryStream(x.Payload))
                    using (ProtocolStreamReader reader = new ProtocolStreamReader(stream, true, this.hashAlgorithmStore))
                    {
                        ulong neededItemCount = await reader.ReadCompactSizeAsync(token);

                        while (neededItemCount-- > 0)
                        {
                            ProtocolInventoryVector vector = await reader.ReadInventoryVectorAsync(token);
                            inventoryVectors.Add(vector);
                        }
                    }

                    this.inventoryOffers.OnNext(inventoryVectors.ToArray());
                }
            , token);

            this.rawNetwork.ReceivedMessages.Subscribe
            (
                async x =>
                {
                    if (!x.CommandBytes.SequenceEqual(blockBytes))
                    {
                        return;
                    }

                    using (MemoryStream stream = new MemoryStream(x.Payload))
                    using (ProtocolStreamReader reader = new ProtocolStreamReader(stream, true, this.hashAlgorithmStore))
                    {
                        ProtocolBlock protoBlock = await reader.ReadBlockAsync(token);
                        this.blocksReceived.OnNext(protoBlock);
                    }
                }
            , token);

            this.rawNetwork.ReceivedMessages.Subscribe
            (
                async x =>
                {
                    if (!x.CommandBytes.SequenceEqual(txBytes))
                    {
                        return;
                    }

                    using (MemoryStream stream = new MemoryStream(x.Payload))
                    using (ProtocolStreamReader reader = new ProtocolStreamReader(stream, true, this.hashAlgorithmStore))
                    {
                        ProtocolTransaction protoTransaction = await reader.ReadTransactionAsync(token);
                        this.transactionsReceived.OnNext(protoTransaction);
                    }
                }
            , token);

            this.rawNetwork.ReceivedMessages.Subscribe
            (
                async x =>
                {
                    if (!x.NetworkParameters.Equals(this.currencyParameters.NetworkParameters) ||
                        !x.CommandBytes.SequenceEqual(versionBytes))
                    {
                        return;
                    }

                    using (MemoryStream stream = new MemoryStream(x.Payload))
                    using (ProtocolStreamReader reader = new ProtocolStreamReader(stream, true, this.hashAlgorithmStore))
                    {
                        ProtocolVersionPacket versionPacket = await reader.ReadVersionPacketAsync(token);
                        this.versionOffersReceived.OnNext(Tuple.Create(this.connectedPeers[x.RemoteClient], versionPacket));
                    }
                }
            , token);

            this.rawNetwork.ReceivedMessages.Subscribe
            (
                x =>
                {
                    if (!x.CommandBytes.SequenceEqual(verackBytes))
                    {
                        return;
                    }

                    this.versionAcknowledgementsReceived.OnNext(this.connectedPeers[x.RemoteClient]);
                }
            , token);
        }
    }

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
