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
    public sealed class Network : INetwork
    {
        private readonly INetworkParameters networkParameters;
        private readonly ConcurrentDictionary<Guid, TcpClient> clientLookup = new ConcurrentDictionary<Guid, TcpClient>();
        private readonly ReplaySubject<IObservable<INetworkMessage>> messageObservables = new ReplaySubject<IObservable<INetworkMessage>>();
        private readonly CancellationToken token;

        public Network(INetworkParameters networkParameters, CancellationToken token)
        {
            if (networkParameters == null)
            {
                throw new ArgumentNullException("networkParameters");
            }

            this.networkParameters = networkParameters;
            this.token = token;
            token.Register(() =>
            {
                this.messageObservables.OnCompleted();
                foreach (TcpClient cl in this.clientLookup.Values)
                {
                    cl.Close();
                }
            });
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

        /// <summary>
        /// Asynchronously connects to a client.
        /// </summary>
        /// <param name="endPoint">
        /// The end point of the client to connect to.
        /// </param>
        /// <returns>
        /// An awaitable task that yields the ID of the connected client.
        /// </returns>
        public async Task<Guid> ConnectToClientAsync(IPEndPoint endPoint)
        {
            TcpClient existingClient = this.clientLookup.Values.FirstOrDefault(x => x.Client.RemoteEndPoint.Equals(endPoint));
            if (existingClient != null)
            {
                return Guid.Empty;
            }

            return await this.ConnectToClientCoreAsync(async client => await client.ConnectAsync(endPoint.Address, endPoint.Port));
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
        public async Task<Guid> ConnectToClientAsync(DnsEndPoint endPoint)
        {
            return await this.ConnectToClientCoreAsync(async client => await client.ConnectAsync(endPoint.Host, endPoint.Port));
        }

        private async Task<Guid> ConnectToClientCoreAsync(Func<TcpClient, Task> connectionCallback)
        {
            Guid clientId = Guid.NewGuid();
            TcpClient client = new TcpClient(AddressFamily.InterNetwork);
            this.clientLookup.TryAdd(clientId, client);

            await connectionCallback(client);
            Stream stream = client.GetStream();
            IObservable<Message> messageStream = Observable.FromAsync(() => this.ReadMessage(stream, clientId))
                                                           .DoWhile(() => client.Connected)
                                                           .TakeWhile(msg => msg != null);
            await Task.Run(() => this.messageObservables.OnNext(messageStream), this.token);

            return clientId;
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
        public async Task BroadcastMessageAsync(INetworkMessage message)
        {
            IEnumerable<Task> broadcastTasks = this.clientLookup.Values.Select(tcpClient => this.SendMessageCoreAsync(tcpClient, message));
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
        public async Task SendMessageToClientAsync(Guid clientId, INetworkMessage message)
        {
            TcpClient client;
            if (!this.clientLookup.TryGetValue(clientId, out client))
            {
                throw new ArgumentException("Client " + clientId + " not found.", "clientId");
            }

            await this.SendMessageCoreAsync(client, message);
        }

        private async Task<Message> ReadMessage(Stream stream, Guid clientId)
        {
            Message inc = new Message(this.networkParameters, clientId);
            try
            {
                await inc.ReadFrom(stream, token);
            }
            catch (AggregateException ex)
            {
                ex.Flatten().Handle(ch => ch is EndOfStreamException);
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            return inc;
        }

        private async Task SendMessageCoreAsync(TcpClient client, INetworkMessage messageToSend)
        {
            if (!client.Connected)
            {
                throw new InvalidOperationException("The client is not connected.");
            }

            byte[] messageBytes = messageToSend.FullData.ToArray();
            await client.GetStream().WriteAsync(messageBytes, 0, messageBytes.Length, this.token);
        }
    }
}
