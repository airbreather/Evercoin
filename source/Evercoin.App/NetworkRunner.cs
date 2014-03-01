using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.App
{
    public sealed class NetworkRunner
    {
        [Import(typeof(INetwork))]
        private INetwork network;

        [ImportMany]
        private readonly List<INetworkMessageHandler> messageHandlers = new List<INetworkMessageHandler>();

        public INetwork Network { get { return this.network; } }

        public async Task Run(CancellationToken token)
        {
            List<IPEndPoint> endPoints = new List<IPEndPoint>
                                         {
                                             new IPEndPoint(IPAddress.Loopback, 8333),
                                             ////new IPEndPoint(IPAddress.Parse("83.165.101.118"), 8333),
                                             ////new IPEndPoint(IPAddress.Parse("176.103.112.7"), 8333),
                                             ////new IPEndPoint(IPAddress.Parse("192.3.11.20"), 8333),
                                             ////new IPEndPoint(IPAddress.Parse("199.98.20.213"), 8333),
                                         };

            foreach (IPEndPoint endPoint in endPoints)
            {
                await this.network.ConnectToClientAsync(endPoint, token);
            }

            object consoleLock = new object();
            this.network.ReceivedMessages.Subscribe(
                async msg =>
                {
                    INetworkMessageHandler correctHandler = this.messageHandlers.FirstOrDefault(handler => handler.RecognizesMessage(msg));
                    HandledNetworkMessageResult result = HandledNetworkMessageResult.UnrecognizedCommand;
                    if (correctHandler != null)
                    {
                        result = await correctHandler.HandleMessageAsync(msg, token);
                    }

                    lock (consoleLock)
                    {
                        Console.WriteLine("Received message: Command={0}, Payload={1}, HandlingResult={2}",
                                          Encoding.ASCII.GetString(msg.CommandBytes.ToArray()),
                                          ByteTwiddling.ByteArrayToHexString(msg.Payload),
                                          result);
                    }
                },
                ex => this.Network.Dispose(),
                () => this.Network.Dispose());
        }
    }
}
