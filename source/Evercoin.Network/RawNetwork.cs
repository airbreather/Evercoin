using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network
{
    public sealed class RawNetwork : DisposableObject, IRawNetwork
    {
        private readonly INetworkParameters networkParameters;
        private readonly IHashAlgorithmStore hashAlgorithmStore;
        private readonly ConcurrentDictionary<Guid, TcpClient> clientLookup = new ConcurrentDictionary<Guid, TcpClient>();

        private readonly Subject<INetworkMessage> allMessages = new Subject<INetworkMessage>();

        private readonly TcpListener listener;

        private readonly Subject<Guid> incomingConnections = new Subject<Guid>();

        public RawNetwork(INetworkParameters networkParameters, IHashAlgorithmStore hashAlgorithmStore)
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
            this.listener = new TcpListener(new IPEndPoint(IPAddress.Any, 11223));
            this.listener.Start();
            IObservable<TcpClient> incomingClients = Observable.FromAsync(() => this.listener.AcceptTcpClientAsync()).Repeat().TakeWhile(_ => !this.IsDisposed);
            incomingClients.Subscribe
            (
                client =>
                {
                    Guid clientId = Guid.NewGuid();
                    this.clientLookup.TryAdd(clientId, client);
                    this.incomingConnections.OnNext(clientId);
                    ProtocolStreamReader streamReader = new ProtocolStreamReader(new BufferedStream(client.GetStream()), false, this.hashAlgorithmStore);
                    IObservable<INetworkMessage> messageStream = Observable.FromAsync(ct => streamReader.ReadNetworkMessageAsync(this.networkParameters, clientId, ct))
                                                                           .DoWhile(() => client.Connected)
                                                                           .TakeWhile(msg => msg != null)
                                                                           .Finally(delegate
                                                                                    {
                                                                                        streamReader.Dispose();
                                                                                        client.Close();
                                                                                        this.clientLookup.TryRemove(clientId, out client);
                                                                                    });

                    messageStream.Subscribe(this.allMessages.OnNext);
                }
            );

            ////this.allMessages.Subscribe(x => Console.WriteLine("Recv: {0} . {1}", Encoding.ASCII.GetString(x.CommandBytes), ByteTwiddling.ByteArrayToHexString(x.Payload)));
        }

        /// <summary>
        /// Gets an observable sequence of messages received on this network.
        /// </summary>
        public IObservable<INetworkMessage> ReceivedMessages { get { return this.allMessages; } }

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

        public IObservable<Guid> ReceivedConnections { get { return this.incomingConnections; } }

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

        public TcpClient GetClientButPleaseBeNice(Guid clientId)
        {
            TcpClient client;
            this.clientLookup.TryGetValue(clientId, out client);
            return client;
        }

        private async Task<Guid> ConnectToClientCoreAsync(Func<TcpClient, Task> connectionCallback, CancellationToken token)
        {
            Guid clientId = Guid.NewGuid();
            TcpClient client = new TcpClient(AddressFamily.InterNetwork);
            this.clientLookup.TryAdd(clientId, client);

            await connectionCallback(client);
            ProtocolStreamReader streamReader = new ProtocolStreamReader(client.GetStream(), true, this.hashAlgorithmStore);
            IObservable<INetworkMessage> messageStream = Observable.FromAsync(() => streamReader.ReadNetworkMessageAsync(this.networkParameters, clientId, token))
                                                                   .DoWhile(() => client.Connected)
                                                                   .TakeWhile(msg => msg != null && !token.IsCancellationRequested)
                                                                   .Finally(delegate
                                                                    {
                                                                        streamReader.Dispose();
                                                                        client.Close();
                                                                        this.clientLookup.TryRemove(clientId, out client);
                                                                    });

            messageStream.Subscribe(this.allMessages.OnNext);
            return clientId;
        }

        private async Task SendMessageCoreAsync(TcpClient client, INetworkMessage messageToSend, CancellationToken token)
        {
            if (!client.Connected)
            {
                throw new InvalidOperationException("The client is not connected.");
            }

            ////Console.WriteLine("Send: {0} . {1}", Encoding.ASCII.GetString(messageToSend.CommandBytes), ByteTwiddling.ByteArrayToHexString(messageToSend.Payload));

            byte[] messageBytes = messageToSend.FullData;
            await client.GetStream().WriteAsync(messageBytes, 0, messageBytes.Length, token);
        }
    }
}
