using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;

using Evercoin.Util;

namespace Evercoin.App
{
    class Program
    {
        static void Main(string[] args)
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

            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();
        }
    }
}
