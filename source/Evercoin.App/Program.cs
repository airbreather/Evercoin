using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Network;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.App
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Don't read too far into this part... it's mainly just a sanity check for the protobuf and MEF stuff.
            AssemblyCatalog catalog2 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Algorithms")));
            AssemblyCatalog catalog3 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Storage")));
            AggregateCatalog catalog = new AggregateCatalog(catalog2, catalog3);
            CompositionContainer container = new CompositionContainer(catalog);

            FileSettings settings = new FileSettings
                                    {
                                        BlockStoragePath = @"C:\Freedom\Blocks",
                                        TransactionStoragePath = @"C:\Freedom\Transactions"
                                    };

            Catalog c = new Catalog();

            container.ComposeParts(c, settings);

            Console.WriteLine("=== Block Saving / Loading ===");
            string id = Guid.NewGuid().ToString();
            IBlock blockToSave = new SomeBlockClass { Identifier = id, Transactions = ImmutableList<ITransaction>.Empty, Coinbase = new SomeValueSourceClass { AvailableValue = 50, ScriptPubKey = ImmutableList.Create<byte>(25, 42, 254) } };
            c.ChainStores.First().PutBlock(blockToSave);
            Console.WriteLine("Saved a block with Identifier {0}.", id);
            IBlock loadedBlock = c.ReadOnlyChainStores.First().GetBlock(id);
            Console.WriteLine("Loaded a block with Identifier {0}.", loadedBlock.Identifier);
            Console.WriteLine("The two are {0}equal.", loadedBlock.Equals(blockToSave) ? String.Empty : "not ");

            Console.WriteLine();

            Console.WriteLine("=== Hash Algorithms ===");
            int randomSeed = Guid.NewGuid().GetHashCode();
            Random random = new Random(randomSeed);
            byte[] randomBytes = new byte[42];
            random.NextBytes(randomBytes);
            Console.WriteLine("Data to hash:");
            Console.WriteLine("{0}", ByteTwiddling.ByteArrayToHexString(randomBytes).ToLowerInvariant());
            Console.WriteLine();
            Console.WriteLine("Hash results:");

            Type haiType = typeof(HashAlgorithmIdentifiers);
            List<FieldInfo> algorithmFields = haiType.GetFields(BindingFlags.Public | BindingFlags.Static)
                                                     .Where(x => x.FieldType == typeof(Guid))
                                                     .ToList();
            int outputColumnWidth = algorithmFields.Max(x => x.Name.Length);
            foreach (FieldInfo hashAlgorithmIdentifier in algorithmFields)
            {
                Guid identifier = (Guid)hashAlgorithmIdentifier.GetValue(null);
                IHashAlgorithm algo = c.HashAlgorithmStores.First().GetHashAlgorithm(identifier);
                Console.WriteLine("{0," + outputColumnWidth + "}: {1}", hashAlgorithmIdentifier.Name, ByteTwiddling.ByteArrayToHexString(algo.CalculateHash(randomBytes)).ToLowerInvariant());
            }

            Console.WriteLine();
            Console.WriteLine("=== Network ===");

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Console.WriteLine("Press Enter to quit...");
                DoNetwork(cts.Token);

                Console.ReadLine();

                cts.Cancel();
            }

            Thread.Sleep(2000);
        }

        private static void DoNetwork(CancellationToken ct)
        {
            SomeNetworkParams parameters = new SomeNetworkParams();
                                           ////{
                                           ////    Seeds =
                                           ////    {
                                           ////        new DnsEndPoint("seed.bitcoin.sipa.be", 8333, AddressFamily.InterNetwork),
                                           ////        new DnsEndPoint("dnsseed.bluematt.me", 8333, AddressFamily.InterNetwork),
                                           ////        new DnsEndPoint("dnsseed.bitcoin.dashjr.org", 8333, AddressFamily.InterNetwork),
                                           ////        new DnsEndPoint("bitseed.xf2.org", 8333, AddressFamily.InterNetwork),
                                           ////    }
                                           ////};

            List<IPEndPoint> ipEndPoints = new List<IPEndPoint>
                                           {
                                               new IPEndPoint(IPAddress.Loopback, 8333),
                                               ////new IPEndPoint(IPAddress.Parse("83.165.101.118"), 8333),
                                               ////new IPEndPoint(IPAddress.Parse("176.103.112.7"), 8333),
                                               ////new IPEndPoint(IPAddress.Parse("192.3.11.20"), 8333),
                                               ////new IPEndPoint(IPAddress.Parse("199.98.20.213"), 8333),
                                           };

            INetwork net = new Network.Network(parameters, ct);
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                Task<Guid> clientIdTask = net.ConnectToClientAsync(endPoint);
                Task cont = clientIdTask.ContinueWith(t =>
                {
                    Guid clientId = t.Result;
                    INetworkMessage versionMessage = new VersionMessageBuilder(net).BuildVersionMessage(clientId, 1, Instant.FromDateTimeUtc(DateTime.UtcNow), 50, "/Evercoin:0.0.0/VS:0.0.0/", 0, true);
                    net.SendMessageToClientAsync(clientId, versionMessage);
                },
                ct);
            }

            object consoleLock = new object();
            net.ReceivedMessages.Subscribe(msg =>
            {
                lock (consoleLock)
                {
                    Console.WriteLine("Received: Command={0}, Payload={1}, Sender={2}", Encoding.ASCII.GetString(msg.CommandBytes.ToArray()), ByteTwiddling.ByteArrayToHexString(msg.Payload), msg.RemoteClient);
                }

                HandleMessage(msg, net, ct);
            });
        }

        private static async void HandleMessage(INetworkMessage message, INetwork net, CancellationToken ct)
        {
            if (!message.CommandBytes.GetRange(0, 4).SequenceEqual(Encoding.ASCII.GetBytes("addr")))
            {
                return;
            }

            List<IPEndPoint> endPointsToConnectTo = new List<IPEndPoint>();
            using (var payloadStream = new MemoryStream(message.Payload.ToArray()))
            {
                ProtocolCompactSize size = new ProtocolCompactSize();
                await size.LoadFromStreamAsync(payloadStream, ct);
                for (ulong i = 0; i < size.Value; i++)
                {
                    ProtocolNetworkAddress address = new ProtocolNetworkAddress();
                    await address.LoadFromStreamAsync(payloadStream, net.Parameters.ProtocolVersion, ct);
                    IPEndPoint endPoint = new IPEndPoint(address.Address, address.Port);
                    endPointsToConnectTo.Add(endPoint);
                }
            }

            IEnumerable<Task> taskList = endPointsToConnectTo.Select(endPoint => Task.Run(async () =>
                                                                                          {
                                                                                              if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                                                                                              {
                                                                                                  try
                                                                                                  {
                                                                                                      endPoint.Address = endPoint.Address.MapToIPv4();
                                                                                                  }
                                                                                                  catch
                                                                                                  {
                                                                                                      return;
                                                                                                  }
                                                                                              }
                                                                                          
                                                                                              Guid clientId = await net.ConnectToClientAsync(endPoint);
                                                                                              INetworkMessage versionMessage = new VersionMessageBuilder(net).BuildVersionMessage(clientId, 1, Instant.FromDateTimeUtc(DateTime.UtcNow), 50, "/Evercoin:0.0.0/VS:0.0.0/", 0, true);
                                                                                              await net.SendMessageToClientAsync(clientId, versionMessage);
                                                                                          }, 
                                                                                          ct));

            await Task.WhenAll(taskList);
        }
    }
}
