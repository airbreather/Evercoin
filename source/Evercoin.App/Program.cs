using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
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
            // Don't read too far into this part... it's mainly just a sanity check for the WIP stuff.
            AssemblyCatalog catalog1 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Network")));
            AssemblyCatalog catalog2 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Algorithms")));
            AssemblyCatalog catalog3 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.Storage")));
            AssemblyCatalog catalog4 = new AssemblyCatalog(Assembly.Load(new AssemblyName("Evercoin.TransactionScript")));
            AssemblyCatalog catalog5 = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            AggregateCatalog catalog = new AggregateCatalog(catalog1, catalog2, catalog3, catalog4, catalog5);
            using (CompositionContainer container = new CompositionContainer(catalog))
            {
                CompositeChainStorage chainStorage = new CompositeChainStorage();
                CompositeHashAlgorithmStore hashAlgorithmStore = new CompositeHashAlgorithmStore();
                container.ComposeParts(chainStorage, hashAlgorithmStore);
                EvercoinModule module = new EvercoinModule(chainStorage, hashAlgorithmStore);
                using (StandardKernel kernel = new StandardKernel(module))
                {
                    NetworkRunner runner = kernel.Get<NetworkRunner>();

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
                        Console.WriteLine("{0," + outputColumnWidth + "}: {1}", hashAlgorithmIdentifier.Name, ByteTwiddling.ByteArrayToHexString(algo.CalculateHash(randomBytes)));
                    }

                    Console.WriteLine();
                    Console.WriteLine("=== Network ===");
                    Console.WriteLine("Legend:");
                    Console.WriteLine("(.) = OK");
                    Console.WriteLine("(?) = Command is not handled");
                    Console.WriteLine("(@) = Data is invalid");
                    Console.WriteLine("(*) = Message is invalid");
                    Console.WriteLine("(!) = Unknown error");
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Press Enter to quit...");
                    using (CancellationTokenSource cts = new CancellationTokenSource())
                    {
                        Task t = runner.Run(cts.Token);
                        Console.ReadLine();
                        cts.Cancel();
                        t.Wait();
                    }
                }
            }
        }
    }
}
