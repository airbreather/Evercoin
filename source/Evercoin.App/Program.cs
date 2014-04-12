using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

using Ninject;

namespace Evercoin.App
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Don't read too far into this assembly... it's mainly just a sanity check for the WIP stuff.
            AssemblyCatalog catalog1 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Network")));
            AssemblyCatalog catalog2 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Algorithms")));
            AssemblyCatalog catalog3 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Storage")));
            AssemblyCatalog catalog4 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.TransactionScript")));
            AssemblyCatalog catalog5 = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            AggregateCatalog catalog = new AggregateCatalog(catalog1, catalog2, catalog3, catalog4, catalog5);
            using (CompositeChainStorage chainStorage = new CompositeChainStorage())
            using (CompositionContainer container = new CompositionContainer(catalog))
            {
                CompositeHashAlgorithmStore hashAlgorithmStore = new CompositeHashAlgorithmStore();
                container.ComposeParts(chainStorage, hashAlgorithmStore);

                Dictionary<char, Tuple<string, string>> chainMapping = new Dictionary<char, Tuple<string, string>>
                                                       {
                                                           { '1', Tuple.Create("Bitcoin  ", "(Port:  8333)") },
                                                           { '2', Tuple.Create("Dogecoin ", "(Port: 22556)") },
                                                           { '3', Tuple.Create("Testnet3 ", "(Port: 18333)") }
                                                       };

                Console.WriteLine("Choose the chain you wish to work on.  For now, you need to have a full proper");
                Console.WriteLine("client running locally on the listed port, C:\\Freedom needs to exist as a");
                Console.WriteLine("writable location, and the residual data from C:\\Freedom needs to be");
                Console.WriteLine("completely wiped after switching from one chain to another.");
                foreach (var kvp in chainMapping)
                {
                    char userKey = kvp.Key;
                    Tuple<string, string> explanation = kvp.Value;

                    Console.WriteLine("{0}: {1} {2}", userKey, explanation.Item1, explanation.Item2);
                }

                int failures = 0;
                Tuple<string, string> meaning;
                while (!chainMapping.TryGetValue(Console.ReadKey(true).KeyChar, out meaning))
                {
                    string failureMessage;
                    switch (failures++)
                    {
                        case 1:
                            failureMessage = "...";
                            break;

                        case 30:
                            failureMessage = "cut it out...";
                            break;

                        case 45:
                            failureMessage = "quit fooling around...";
                            break;

                        case 68:
                            failureMessage = "just pick one of the numbers already...";
                            break;

                        case 90:
                            failureMessage = String.Format(CultureInfo.CurrentCulture, "there are only {0} choices...                      ", chainMapping.Count);
                            break;

                        case 156:
                            failureMessage = "I give up...";
                            break;

                        default:
                            failureMessage = null;
                            break;
                    }

                    if (!String.IsNullOrEmpty(failureMessage))
                    {
                        Console.Write("\r");
                        Console.Write(failureMessage.PadRight(80));
                        Console.Write("\r");
                    }
                }

                Console.WriteLine("\r".PadRight(80));

                using (EvercoinModule module = new EvercoinModule(meaning.Item1.Trim(), chainStorage, hashAlgorithmStore))
                using (StandardKernel kernel = new StandardKernel(module))
                {
                    Console.WriteLine("=== Hash Algorithms ===");
                    Random random = new Random(Guid.NewGuid().GetHashCode());
                    byte[] randomBytes = new byte[42];
                    random.NextBytes(randomBytes);
                    Console.WriteLine("Data to hash:");
                    Console.WriteLine("{0}", ByteTwiddling.ByteArrayToHexString(randomBytes));
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
                        IHashAlgorithm algo = hashAlgorithmStore.GetHashAlgorithm(identifier);
                        Console.WriteLine("{0," + outputColumnWidth + "}: {1}", hashAlgorithmIdentifier.Name, ByteTwiddling.ByteArrayToHexString(algo.CalculateHash(randomBytes).Value));
                    }

                    Console.WriteLine();
                    Console.WriteLine("=== Network ===");
                    Console.WriteLine("Press Enter to quit...");
                    using (CancellationTokenSource cts = new CancellationTokenSource())
                    {
                        try
                        {
                            NetworkRunner runner = kernel.Get<NetworkRunner>();
                            Task t = runner.Run(module.Port, cts.Token);
                            Console.ReadLine();
                            cts.Cancel();
                            t.Wait();
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                }
            }
        }
    }
}
