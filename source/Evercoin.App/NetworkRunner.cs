using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Network.MessageHandlers;
using Evercoin.Util;

namespace Evercoin.App
{
    public sealed class NetworkRunner
    {
        private readonly IRawNetwork network;

        private readonly Collection<INetworkMessageHandler> messageHandlers = new Collection<INetworkMessageHandler>();

        public NetworkRunner(IRawNetwork network,
                             BlockMessageHandler blockMessageHandler,
                             InventoryMessageHandler inventoryMessageHandler,
                             TransactionMessageHandler transactionMessageHandler,
                             VerAckMessageHandler verAckMessageHandler,
                             VersionMessageHandler versionMessageHandler)
        {
            this.network = network;
            this.messageHandlers = new Collection<INetworkMessageHandler>
                                   {
                                       blockMessageHandler,
                                       inventoryMessageHandler,
                                       transactionMessageHandler,
                                       verAckMessageHandler,
                                       versionMessageHandler
                                   };
        }

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

            Dictionary<HandledNetworkMessageResult, char> quickGlanceMapping = new Dictionary<HandledNetworkMessageResult, char>
                                                                               {
                                                                                   { HandledNetworkMessageResult.MessageInvalid, '*' },
                                                                                   { HandledNetworkMessageResult.ContextuallyInvalid, '@' },
                                                                                   { HandledNetworkMessageResult.UnrecognizedCommand, '?' },
                                                                                   { HandledNetworkMessageResult.Okay, '.' }
                                                                               };

            ConcurrentDictionary<Guid, int> readableIdMapping = new ConcurrentDictionary<Guid, int>();

            this.network.ReceivedMessages.Subscribe(
                msg =>
                {
                    INetworkMessageHandler correctHandler = this.messageHandlers.FirstOrDefault(handler => handler.RecognizesMessage(msg));
                    HandledNetworkMessageResult result = HandledNetworkMessageResult.UnrecognizedCommand;
                    if (correctHandler != null)
                    {
                        try
                        {
                            result = correctHandler.HandleMessageAsync(msg, token).Result;
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                    }

                    char quickGlanceChar;
                    if (!quickGlanceMapping.TryGetValue(result, out quickGlanceChar))
                    {
                        quickGlanceChar = '!';
                    }

                    int readableId;
                    if (!readableIdMapping.TryGetValue(msg.RemoteClient, out readableId))
                    {
                        readableId = -1;
                    }

                    Console.Write("\r({3}) ({1}) {2} >> {0}",
                        Encoding.ASCII.GetString(msg.CommandBytes),
                        quickGlanceChar,
                        readableId,
                        Cheating.GetBlockIdentifierCount());
                });

            Dictionary<IPEndPoint, Task<Guid>> connectionTasks = endPoints.ToDictionary(endPoint => endPoint, endPoint => this.network.ConnectToClientAsync(endPoint, token));

            int i = 0;
            foreach (KeyValuePair<IPEndPoint, Task<Guid>> kvp in connectionTasks)
            {
                IPEndPoint endPoint = kvp.Key;
                Task<Guid> connectionTask = kvp.Value;

                connectionTask.ContinueWith(
                    t =>
                    {
                        int j = Interlocked.Increment(ref i);
                        readableIdMapping.TryAdd(t.Result, i);
                        Console.WriteLine("(.) {1} << Connected to {0}",
                            endPoint,
                            j);
                    },
                    token,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.Current);

                connectionTask.ContinueWith(
                    t =>
                    {
                        int j = Interlocked.Increment(ref i);
                        Console.WriteLine("(*) {2} << Failed to connect to {0}.  Error message: {1}",
                            endPoint,
                            t.Exception.Flatten().InnerExceptions.First().Message,
                            j);
                    },
                    token,
                    TaskContinuationOptions.NotOnRanToCompletion,
                    TaskScheduler.Current);
            }

            // The remainder of this method is 100% cheating, but it's enough
            // to exercise the "start fetching blocks on the network" code.
            try
            {
                await Task.Delay(1000, token);
                int startingCount = Cheating.GetBlockIdentifierCount();
                int prev = -1;
                while (!token.IsCancellationRequested)
                {
                    var blidCount = Cheating.GetBlockIdentifierCount();
                    if ((blidCount - startingCount) % 500 == 0 &&
                        blidCount != prev)
                    {
                        prev = blidCount;
                        await this.network.AskForMoreBlocks();
                    }

                    await Task.Delay(50, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
