using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.App
{
    public sealed class NetworkRunner
    {
        [Import(typeof(INetwork))] private INetwork network;

        [ImportMany] private readonly List<INetworkMessageHandler> messageHandlers = new List<INetworkMessageHandler>();

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

            this.network.ReceivedMessages.ObserveOn(TaskPoolScheduler.Default).Subscribe(
                async msg =>
                {
                    INetworkMessageHandler correctHandler = this.messageHandlers.FirstOrDefault(handler => handler.RecognizesMessage(msg));
                    HandledNetworkMessageResult result = HandledNetworkMessageResult.UnrecognizedCommand;
                    if (correctHandler != null)
                    {
                        try
                        {
                            result = await correctHandler.HandleMessageAsync(msg, token);
                        }
                        catch (OperationCanceledException)
                        {
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
                        Encoding.ASCII.GetString(msg.CommandBytes.ToArray()),
                        quickGlanceChar,
                        readableId,
                        Cheating.BlockIdentifiers.Count);
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
                while (!token.IsCancellationRequested)
                {
                    if (Cheating.BlockIdentifiers.Count % 500 == 1)
                    {
                        await this.network.AskForMoreBlocks();
                    }

                    await Task.Delay(100, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
