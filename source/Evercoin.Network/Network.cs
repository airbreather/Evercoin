using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Network.MessageHandlers;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network
{
    public sealed class Network : INetwork
    {
        private readonly INetworkParameters networkParameters;
        private readonly IHashAlgorithmStore hashAlgorithmStore;
        private readonly ConcurrentDictionary<Guid, TcpClient> clientLookup = new ConcurrentDictionary<Guid, TcpClient>();
        private readonly Subject<IObservable<INetworkMessage>> messageObservables = new Subject<IObservable<INetworkMessage>>();

        public Network(INetworkParameters networkParameters, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (networkParameters == null)
            {
                throw new ArgumentNullException("networkParameters");
            }

            if (hashAlgorithmStore == null)
            {
                throw new ArgumentNullException("hashAlgorithmStore");
            }

            this.networkParameters = networkParameters;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        /// <summary>
        /// Gets an observable sequence of messages received on this network.
        /// </summary>
        public IObservable<INetworkMessage> ReceivedMessages { get { return this.messageObservables.Merge(); } }

        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> object that defines the
        /// networkParameters that this network uses.
        /// </summary>
        public INetworkParameters Parameters { get { return this.networkParameters; } }

        internal IDictionary<Guid, TcpClient> ClientLookup { get { return this.clientLookup; } }

        public async Task BroadcastMessageAsync(INetworkMessage message)
        {
            await this.BroadcastMessageAsync(message, CancellationToken.None);
        }

        public async Task SendMessageToClientAsync(Guid clientId, INetworkMessage message)
        {
            await this.SendMessageToClientAsync(clientId, message, CancellationToken.None);
        }

        public async Task<Guid> ConnectToClientAsync(IPEndPoint endPoint)
        {
            return await this.ConnectToClientAsync(endPoint, CancellationToken.None);
        }

        public async Task<Guid> ConnectToClientAsync(DnsEndPoint endPoint)
        {
            return await this.ConnectToClientAsync(endPoint, CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <returns>
        /// An awaitable task that yields the ID of the connected client.
        /// </returns>
        public async Task<Guid> ConnectToClientAsync(IPEndPoint endPoint, CancellationToken token)
        {
            TcpClient existingClient = this.clientLookup.Values.FirstOrDefault(x => x.Client.RemoteEndPoint.Equals(endPoint));
            if (existingClient != null)
            {
                return Guid.Empty;
            }

            return await this.ConnectToClientCoreAsync(async client => await client.ConnectAsync(endPoint.Address, endPoint.Port), token);
        }

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <returns>
        /// An awaitable task that yields the ID of the connected client.
        /// </returns>
        public async Task<Guid> ConnectToClientAsync(DnsEndPoint endPoint, CancellationToken token)
        {
            return await this.ConnectToClientCoreAsync(async client => await client.ConnectAsync(endPoint.Host, endPoint.Port), token);
        }

        public void Dispose()
        {
            this.messageObservables.OnCompleted();
            foreach (var client in this.ClientLookup.Values)
            {
                client.Close();
            }
        }

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
        public async Task BroadcastMessageAsync(INetworkMessage message, CancellationToken token)
        {
            IEnumerable<Task> broadcastTasks = this.clientLookup.Values.Select(tcpClient => this.SendMessageCoreAsync(tcpClient, message, token));
            await Task.WhenAll(broadcastTasks);
        }

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
        public async Task SendMessageToClientAsync(Guid clientId, INetworkMessage message, CancellationToken token)
        {
            TcpClient client;
            if (!this.clientLookup.TryGetValue(clientId, out client))
            {
                throw new ArgumentException("Client " + clientId + " not found.", "clientId");
            }

            await this.SendMessageCoreAsync(client, message, token);
        }

        private async Task<Guid> ConnectToClientCoreAsync(Func<TcpClient, Task> connectionCallback, CancellationToken token)
        {
            Guid clientId = Guid.NewGuid();
            TcpClient client = new TcpClient(AddressFamily.InterNetwork);
            this.clientLookup.TryAdd(clientId, client);

            await connectionCallback(client);
            ProtocolStreamReader streamReader = new ProtocolStreamReader(client.GetStream(), true, this.hashAlgorithmStore);
            IObservable<INetworkMessage> messageStream = Observable.FromAsync(ct => streamReader.ReadNetworkMessageAsync(this.networkParameters, clientId, ct))
                                                                   .DoWhile(() => client.Connected)
                                                                   .TakeWhile(msg => msg != null && !token.IsCancellationRequested)
                                                                   .Finally(streamReader.Dispose);

            await Task.Run(() => this.messageObservables.OnNext(messageStream), token);

            VersionMessageBuilder builder = new VersionMessageBuilder(this, this.hashAlgorithmStore);

            INetworkMessage mm = builder.BuildVersionMessage(clientId, 1, Instant.FromDateTimeUtc(DateTime.UtcNow), 500, "/Evercoin:0.0.0/VS:0.0.0/", 1, pleaseRelayTransactionsToMe: false);
            await this.SendMessageToClientAsync(clientId, mm, token);

            return clientId;
        }

        private async Task SendMessageCoreAsync(TcpClient client, INetworkMessage messageToSend, CancellationToken token)
        {
            if (!client.Connected)
            {
                throw new InvalidOperationException("The client is not connected.");
            }

            byte[] messageBytes = messageToSend.FullData.ToArray();
            await client.GetStream().WriteAsync(messageBytes, 0, messageBytes.Length, token);
        }

        public async Task AskForMoreBlocks()
        {
            await Task.Delay(500);
            GetBlocksMessageBuilder b = new GetBlocksMessageBuilder(this, this.hashAlgorithmStore);
            var message = b.BuildGetDataMessage(Guid.NewGuid(), FetchBlockLocator().ToList(), BigInteger.Zero);
            await this.BroadcastMessageAsync(message);
        }

        internal static IEnumerable<BigInteger> FetchBlockLocator()
        {
            IReadOnlyList<BigInteger> blockIdentifierCollection = Cheating.GetBlockIdentifiers();

            int step = 1;
            int start = 0;
            for (int i = blockIdentifierCollection.Count - 1; i > 0; start++, i -= step)
            {
                if (start >= 10)
                {
                    step *= 2;
                }

                yield return blockIdentifierCollection[i];
            }

            yield return blockIdentifierCollection[0];
        }
    }
}
