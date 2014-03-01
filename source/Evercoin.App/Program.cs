using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

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
            AssemblyCatalog catalog4 = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            AggregateCatalog catalog = new AggregateCatalog(catalog1, catalog2, catalog3, catalog4);
            using (CompositionContainer container = new CompositionContainer(catalog))
            {
                Catalog c = new Catalog();
                NetworkRunner runner = new NetworkRunner();

                container.ComposeParts(c, runner);

                Console.WriteLine("=== Hash Algorithms ===");
                Random random = new Random(Guid.NewGuid().GetHashCode());
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
                Console.WriteLine("Press Enter to quit...");

                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    Task task = runner.Run(cts.Token);
                    Console.ReadLine();
                    cts.Cancel();
                    task.Wait();
                }
            }
        }
    }
}
