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

            Dictionary<IPEndPoint, Task<Guid>> connectionTasks = endPoints.ToDictionary(endPoint => endPoint, endPoint => this.network.ConnectToClientAsync(endPoint, token));
            Dictionary<HandledNetworkMessageResult, char> quickGlanceMapping = new Dictionary<HandledNetworkMessageResult, char>
                                                                               {
                                                                                   { HandledNetworkMessageResult.MessageInvalid, '*' },
                                                                                   { HandledNetworkMessageResult.ContextuallyInvalid, '@' },
                                                                                   { HandledNetworkMessageResult.UnrecognizedCommand, '?' },
                                                                                   { HandledNetworkMessageResult.Okay, '.' }
                                                                               };

            Dictionary<Guid, int> readableIdMapping = new Dictionary<Guid, int>();
            int i = 0;

            foreach (KeyValuePair<IPEndPoint, Task<Guid>> kvp in connectionTasks)
            {
                IPEndPoint endPoint = kvp.Key;
                Task<Guid> connectionTask = kvp.Value;

                try
                {
                    Guid clientId = await connectionTask;
                    readableIdMapping.Add(clientId, i);
                    Console.WriteLine("(.) {1} >> Connected to {0}",
                                      endPoint,
                                      i);
                    i++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("(*) {2} >> Failed to connect to {0}.  Error message: {1}",
                                      endPoint,
                                      ex.Message,
                                      i++);
                }
            }

            this.network.ReceivedMessages.Subscribe(
                async msg =>
                {
                    INetworkMessageHandler correctHandler = this.messageHandlers.FirstOrDefault(handler => handler.RecognizesMessage(msg));
                    HandledNetworkMessageResult result = HandledNetworkMessageResult.UnrecognizedCommand;
                    if (correctHandler != null)
                    {
                        result = await correctHandler.HandleMessageAsync(msg, token);
                    }

                    char quickGlanceChar;
                    if (!quickGlanceMapping.TryGetValue(result, out quickGlanceChar))
                    {
                        quickGlanceChar = '!';
                    }

                    Console.WriteLine("({2}) {3} >> {0} {{{1}}}",
                                      Encoding.ASCII.GetString(msg.CommandBytes.ToArray()),
                                      ByteTwiddling.ByteArrayToHexString(msg.Payload),
                                      quickGlanceChar,
                                      readableIdMapping[msg.RemoteClient]);
                });
        }
    }
}
