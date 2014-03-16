using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
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

        private readonly Subject<Tuple<INetworkPeer, ProtocolInventoryVector[]>> inventoryOffers = new Subject<Tuple<INetworkPeer, ProtocolInventoryVector[]>>();

        private readonly Subject<Tuple<INetworkPeer, ProtocolInventoryVector[]>> inventoryRequests = new Subject<Tuple<INetworkPeer, ProtocolInventoryVector[]>>();

        private readonly Subject<Tuple<INetworkPeer, ProtocolBlock>> blocksReceived = new Subject<Tuple<INetworkPeer, ProtocolBlock>>();

        private readonly Subject<Tuple<INetworkPeer, ProtocolTransaction>> transactionsReceived = new Subject<Tuple<INetworkPeer, ProtocolTransaction>>();

        private readonly Subject<Tuple<INetworkPeer, ProtocolVersionPacket>> versionOffersReceived = new Subject<Tuple<INetworkPeer, ProtocolVersionPacket>>();

        private readonly Subject<Tuple<INetworkPeer, ulong>> pongsReceived = new Subject<Tuple<INetworkPeer, ulong>>();

        private readonly Subject<INetworkPeer> versionAcknowledgementsReceived = new Subject<INetworkPeer>();

        private readonly Subject<ProtocolNetworkAddress> peerOffers = new Subject<ProtocolNetworkAddress>();

        private readonly Subject<Tuple<INetworkPeer, string, byte[]>> unrecognizedMessagesReceived = new Subject<Tuple<INetworkPeer, string, byte[]>>();

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
        public override IObservable<Tuple<INetworkPeer, ProtocolInventoryVector[]>> ReceivedInventoryOffers { get { return this.inventoryOffers; } }

        /// <summary>
        /// Gets the identifiers of blocks we've been offered.
        /// </summary>
        public override IObservable<Tuple<INetworkPeer, ProtocolInventoryVector[]>> ReceivedInventoryRequests { get { return this.inventoryRequests; } }

        /// <summary>
        /// Gets the addresses of peers we've been offered.
        /// </summary>
        public override IObservable<ProtocolNetworkAddress> ReceivedPeerOffers { get { return this.peerOffers; } }

        /// <summary>
        /// Gets the block messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        public override IObservable<Tuple<INetworkPeer, ProtocolBlock>> ReceivedBlocks { get { return this.blocksReceived; } }

        /// <summary>
        /// Gets the transaction messages we've received.
        /// </summary>
        /// <remarks>
        /// Ordering, validity, etc. not guaranteed.
        /// </remarks>
        public override IObservable<Tuple<INetworkPeer, ProtocolTransaction>> ReceivedTransactions { get { return this.transactionsReceived; } }

        /// <summary>
        /// Gets the ping responses we've received.
        /// </summary>
        public override IObservable<Tuple<INetworkPeer, ulong>> ReceivedPingResponses { get { return this.pongsReceived; } }

        /// <summary>
        /// Gets the version packages we've received.
        /// </summary>
        public override IObservable<Tuple<INetworkPeer, ProtocolVersionPacket>> ReceivedVersionPackets { get { return this.versionOffersReceived; } }

        /// <summary>
        /// Gets the peers that have acknowledged that the version exchange is completed.
        /// </summary>
        public override IObservable<INetworkPeer> ReceivedVersionAcknowledgements { get { return this.versionAcknowledgementsReceived; } }

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
        public override async Task RequestBlockOffersAsync(INetworkPeer peer, IEnumerable<BigInteger> knownBlockIdentifiers, CancellationToken token)
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
            var message = b.BuildGetBlocksMessage(peer, listToRequest, BigInteger.Zero);
            await this.rawNetwork.SendMessageToClientAsync(peer, message, token);
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
        /// completes, observe <see cref="ReceivedInventoryOffers"/>
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
        /// completes, observe <see cref="ReceivedBlocks"/>
        /// or <see cref="ReceivedTransactions"/>.
        /// </remarks>
        public override async Task RequestInventoryAsync(IEnumerable<ProtocolInventoryVector> inventoryVectors, CancellationToken token)
        {
            INetworkPeer peer = this.ConnectedPeers.First().Value;

            INetworkMessage response = new GetDataMessageBuilder(this.rawNetwork, this.hashAlgorithmStore).BuildGetDataMessage(peer, inventoryVectors);
            await this.rawNetwork.SendMessageToClientAsync(peer, response, token);
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
            INetworkMessage message = messageBuilder.BuildPingMessage(peer, nonce);
            await this.rawNetwork.SendMessageToClientAsync(peer, message, token);
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
            INetworkMessage response = builder.BuildVerAckMessage(peer);
            await this.rawNetwork.SendMessageToClientAsync(peer, response, token);
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
        public override Task ConnectToPeerAsync(ProtocolNetworkAddress peerAddress, CancellationToken token)
        {
            IPAddress address = peerAddress.Address;
            ushort port = peerAddress.Port;

            IPEndPoint endPoint = new IPEndPoint(address, port);
            return this.rawNetwork.ConnectToClientAsync(endPoint, token);
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
            INetworkMessage message = messageBuilder.BuildVersionMessage(peer, packet);
            await this.rawNetwork.SendMessageToClientAsync(peer, message, token);
        }

        public override void Start(CancellationToken token)
        {
            this.rawNetwork.PeerConnections.Subscribe
            (
                x =>
                {
                    this.connectedPeers.TryAdd(x.Identifier, x);
                    this.peerConnections.OnNext(x);
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

            string invCommand = Encoding.ASCII.GetString(invBytes);
            string blockCommand = Encoding.ASCII.GetString(blockBytes);
            string txCommand = Encoding.ASCII.GetString(txBytes);
            string versionCommand = Encoding.ASCII.GetString(versionBytes);
            string verackCommand = Encoding.ASCII.GetString(verackBytes);

            Dictionary<string, Action<INetworkMessage>> mapping = new Dictionary<string, Action<INetworkMessage>>
            {
                {
                    invCommand,
                    async x =>
                    {
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

                        this.inventoryOffers.OnNext(Tuple.Create(x.RemotePeer, inventoryVectors.ToArray()));
                    }
                },
                {
                    blockCommand,
                    async x =>
                    {
                        using (MemoryStream stream = new MemoryStream(x.Payload))
                        using (ProtocolStreamReader reader = new ProtocolStreamReader(stream, true, this.hashAlgorithmStore))
                        {
                            ProtocolBlock protoBlock = await reader.ReadBlockAsync(token);
                            this.blocksReceived.OnNext(Tuple.Create(x.RemotePeer, protoBlock));
                        }
                    }
                },
                {
                    txCommand,
                    async x =>
                    {
                        using (MemoryStream stream = new MemoryStream(x.Payload))
                        using (ProtocolStreamReader reader = new ProtocolStreamReader(stream, true, this.hashAlgorithmStore))
                        {
                            ProtocolTransaction protoTransaction = await reader.ReadTransactionAsync(token);
                            this.transactionsReceived.OnNext(Tuple.Create(x.RemotePeer, protoTransaction));
                        }
                    }
                },
                {
                    versionCommand,
                    async x =>
                    {
                        using (MemoryStream stream = new MemoryStream(x.Payload))
                        using (ProtocolStreamReader reader = new ProtocolStreamReader(stream, true, this.hashAlgorithmStore))
                        {
                            ProtocolVersionPacket versionPacket = await reader.ReadVersionPacketAsync(token);
                            this.versionOffersReceived.OnNext(Tuple.Create(x.RemotePeer, versionPacket));
                        }
                    }
                },
                {
                    verackCommand,
                    x =>
                    {
                        this.versionAcknowledgementsReceived.OnNext(x.RemotePeer);
                    }
                },
            };

            Func<INetworkMessage, bool> messageHandler = this.HandleMessage(mapping);
            this.rawNetwork.ReceivedMessages.Subscribe
            (
                x =>
                {
                    if (!messageHandler(x))
                    {
                        this.unrecognizedMessagesReceived.OnNext(Tuple.Create(x.RemotePeer, Encoding.ASCII.GetString(x.CommandBytes), x.Payload));
                    }
                }
            , token);
        }

        private Func<INetworkMessage, bool> HandleMessage(Dictionary<string, Action<INetworkMessage>> handlers)
        {
            return x =>
            {
                string command = Encoding.ASCII.GetString(x.CommandBytes);
                Action<INetworkMessage> handler;
                if (!handlers.TryGetValue(command, out handler))
                {
                    return false;
                }

                handler(x);
                return true;
            };
        }
    }
}
