using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

using Ionic.Zip;

namespace Evercoin.Storage
{
    ////[Export(typeof(IChainStore))]
    ////[Export(typeof(IReadOnlyChainStore))]
    public sealed class ZipFileChainStore : ReadWriteChainStoreBase
    {
        private const string ZipFileName = @"C:\Freedom\evercoin.zip";

        private const string BlockDir = "Blocks";
        private const string TxDir = "Transactions";

        private const string EntrySep = "/";

        private readonly object zipLock = new object();

        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> blockWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();
        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> txWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();

        private readonly ZipFile archive;

        public ZipFileChainStore()
        {
            this.archive = new ZipFile(ZipFileName) { UseZip64WhenSaving = Zip64Option.Always };
            try
            {
                if (!this.archive.ContainsEntry(BlockDir + EntrySep))
                {
                    this.archive.AddDirectoryByName(BlockDir);
                }

                if (!this.archive.ContainsEntry(TxDir + EntrySep))
                {
                    this.archive.AddDirectoryByName(TxDir);
                }

                BigInteger genesisBlockIdentifier = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").Reverse().GetArray());
                if (!this.ContainsBlock(genesisBlockIdentifier))
                {
                    Block genesisBlock = new Block
                                         {
                                             Identifier = genesisBlockIdentifier,
                                             TypedCoinbase = new CoinbaseValueSource
                                                             {
                                                                 AvailableValue = 50,
                                                                 OriginatingBlockIdentifier = genesisBlockIdentifier
                                                             },
                                             TransactionIdentifiers = new MerkleTreeNode { Data = ByteTwiddling.HexStringToByteArray("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B").Reverse().GetArray() }
                                         };
                    this.PutBlock(genesisBlockIdentifier, genesisBlock);
                    this.archive.Save();
                }
            }
            catch
            {
                this.archive.Dispose();
                throw;
            }
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            lock (this.zipLock)
            return this.archive.ContainsEntry(GetBlockEntryName(blockIdentifier));
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            lock (this.zipLock)
            return this.archive.ContainsEntry(GetTransactionEntryName(transactionIdentifier));
        }

        protected override async Task<bool> ContainsBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return await Task.Run(() => this.ContainsBlockCore(blockIdentifier), token);
        }

        protected override async Task<bool> ContainsTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return await Task.Run(() => this.ContainsTransactionCore(transactionIdentifier), token);
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            Monitor.Enter(this.zipLock);
            try
            {
                ZipEntry data;
                do
                {
                    data = this.archive[GetBlockEntryName(blockIdentifier)];
                    if (data != null)
                    {
                        break;
                    }

                    Monitor.Exit(this.zipLock);
                    ManualResetEventSlim mres = this.blockWaiters.GetOrAdd(blockIdentifier, _ => new ManualResetEventSlim());
                    if (mres.Wait(10000) &&
                        this.blockWaiters.TryRemove(blockIdentifier, out mres))
                    {
                        mres.Dispose();
                    }

                    Monitor.Enter(this.zipLock);
                }
                while (true);

                try
                {
                    using (var stream = data.OpenReader())
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        return (Block)binaryFormatter.Deserialize(stream);
                    }
                }
                catch (BadStateException)
                {
                    this.archive.Save();
                    using (var stream = data.OpenReader())
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        return (Block)binaryFormatter.Deserialize(stream);
                    }
                }
            }
            finally
            {
                Monitor.Exit(this.zipLock);
            }
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            Monitor.Enter(this.zipLock);
            try
            {
                ZipEntry data;
                do
                {
                    data = this.archive[GetTransactionEntryName(transactionIdentifier)];
                    if (data != null)
                    {
                        break;
                    }

                    Monitor.Exit(this.zipLock);
                    ManualResetEventSlim mres = this.txWaiters.GetOrAdd(transactionIdentifier, _ => new ManualResetEventSlim());
                    if (mres.Wait(10000) &&
                        this.txWaiters.TryRemove(transactionIdentifier, out mres))
                    {
                        mres.Dispose();
                    }

                    Monitor.Enter(this.zipLock);
                }
                while (true);

                try
                {
                    using (var stream = data.OpenReader())
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        return (Transaction)binaryFormatter.Deserialize(stream);
                    }
                }
                catch (BadStateException)
                {
                    this.archive.Save();
                    using (var stream = data.OpenReader())
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        return (Transaction)binaryFormatter.Deserialize(stream);
                    }
                }
            }
            finally
            {
                Monitor.Exit(this.zipLock);
            }
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            Block typedBlock = new Block(blockIdentifier, block);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            lock (this.zipLock)
            {
                this.archive.AddEntry(GetBlockEntryName(blockIdentifier), (_, entryStream) => binaryFormatter.Serialize(entryStream, typedBlock));
                this.archive.Save();
            }

            ManualResetEventSlim mres;
            if (this.blockWaiters.TryGetValue(blockIdentifier, out mres))
            {
                mres.Set();
            }
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            Transaction typedTransaction = new Transaction(transactionIdentifier, transaction);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            lock (this.zipLock)
            {
                this.archive.AddEntry(GetTransactionEntryName(transactionIdentifier), (_, entryStream) => binaryFormatter.Serialize(entryStream, typedTransaction));
                this.archive.Save();
            }

            ManualResetEventSlim mres;
            if (this.txWaiters.TryGetValue(transactionIdentifier, out mres))
            {
                mres.Set();
            }
        }

        protected override void DisposeManagedResources()
        {
            this.archive.Save();
            this.archive.Dispose();
        }

        private static string GetBlockEntryName(BigInteger blockIdentifier)
        {
            byte[] blockIdentifierBytes = blockIdentifier.ToLittleEndianUInt256Array();
            Array.Reverse(blockIdentifierBytes);
            string id = ByteTwiddling.ByteArrayToHexString(blockIdentifierBytes);
            return String.Join(EntrySep, BlockDir, id);
        }

        private static string GetTransactionEntryName(BigInteger transactionIdentifier)
        {
            byte[] transactionIdentifierBytes = transactionIdentifier.ToLittleEndianUInt256Array();
            Array.Reverse(transactionIdentifierBytes);
            string id = ByteTwiddling.ByteArrayToHexString(transactionIdentifierBytes);
            return String.Join(EntrySep, TxDir, id);
        }
    }
}
