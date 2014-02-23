using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NodaTime;

namespace Evercoin.Network
{
    public sealed class Network : INetwork
    {
        private readonly INetworkParameters networkParameters;
        private readonly VersionMessageBuilder versionMessageBuilder;
        private readonly ConcurrentBag<TcpClient> clients = new ConcurrentBag<TcpClient>();

        private readonly ManualResetEvent startEvt = new ManualResetEvent(false);
        private bool isStarted = false;

        public Network(INetworkParameters networkParameters)
        {
            if (networkParameters == null)
            {
                throw new ArgumentNullException("networkParameters");
            }

            this.networkParameters = networkParameters;
            this.versionMessageBuilder = new VersionMessageBuilder(this.networkParameters);
        }

        /// <summary>
        /// Gets an observable sequence of <see cref="IBlock"/> objects
        /// received on this network.
        /// </summary>
        /// <remarks>
        /// The <see cref="IBlock"/> objects are guaranteed to be
        /// populated, but may not actually be valid according to the best
        /// blockchain.
        /// </remarks>
        public IObservable<IBlock> ReceivedBlocks { get; private set; }

        /// <summary>
        /// Gets an observable sequence of <see cref="ITransaction"/> objects
        /// received on this network.
        /// </summary>
        /// <remarks>
        /// The <see cref="ITransaction"/> objects are guaranteed to be
        /// populated, but may not actually be valid according to the best
        /// blockchain.
        /// </remarks>
        public IObservable<ITransaction> ReceivedTransactions { get; private set; }

        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> object that defines the
        /// networkParameters that this network uses.
        /// </summary>
        public INetworkParameters Parameters { get { return this.networkParameters; } }

        public async void Start()
        {
            if (this.isStarted)
            {
                return;
            }

            List<Task> connectionTasks = new List<Task>();
            foreach (DnsEndPoint endPoint in this.networkParameters.Seeds)
            {
                var tcpClient = new TcpClient(endPoint.AddressFamily);
                this.clients.Add(tcpClient);
                Task connectionTask = tcpClient.ConnectAsync(endPoint.Host, endPoint.Port);
                connectionTasks.Add(connectionTask);

                connectionTask.ContinueWith(_ =>
                {
                    Instant now = Instant.FromDateTimeUtc(DateTime.UtcNow);
                    IPEndPoint localEndPoint = (IPEndPoint)tcpClient.Client.LocalEndPoint;
                    IPEndPoint remoteEndPoint = (IPEndPoint)tcpClient.Client.LocalEndPoint;
                    Message versionMessage = this.versionMessageBuilder.BuildVersionMessage(1,
                                                                                            now,
                                                                                            new ProtocolNetworkAddress((uint)now.Ticks, 1, localEndPoint.Address, (ushort)localEndPoint.Port),
                                                                                            new ProtocolNetworkAddress((uint)now.Ticks, 1, remoteEndPoint.Address, (ushort)remoteEndPoint.Port),
                                                                                            50,
                                                                                            "/Evercoin:0.0.0/VS:0.0.0",
                                                                                            0,
                                                                                            true);
                    byte[] messageData = versionMessage.FullData.ToArray();
                    tcpClient.GetStream().Write(messageData, 0, messageData.Length);
                });
            }

            await Task.WhenAll(connectionTasks);

            this.isStarted = true;
            this.startEvt.Set();
        }

        /// <summary>
        /// Asynchronously broadcasts a blockchain node to the network.
        /// </summary>
        /// <param name="block">
        /// The <see cref="IBlock"/> to broadcast.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        public async Task BroadcastBlockAsync(IBlock block)
        {
            if (!this.isStarted)
            {
                await Task.Run(() => this.startEvt.WaitOne());
            }


        }

        /// <summary>
        /// Asynchronously broadcasts a transaction to the network.
        /// </summary>
        /// <param name="transaction">
        /// The <see cref="ITransaction"/> to broadcast.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> encapsulating the asynchronous operation.
        /// </returns>
        public async Task BroadcastTransactionAsync(ITransaction transaction)
        {
            if (!this.isStarted)
            {
                await Task.Run(() => this.startEvt.WaitOne());
            }


        }

        private void OnDataRead(byte[] data, Stream stream)
        {
            
        }
    }
}
