using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

using Ionic.Zip;
using Ionic.Zlib;

namespace Evercoin.Storage
{
    // HINT: Don't use this.  It sucks.
    ////[Export(typeof(IChainStore))]
    ////[Export(typeof(IReadableChainStore))]
    public sealed class DotNetZipFileChainStore : ReadWriteChainStoreBase
    {
        private const string ZipFileName = @"C:\Freedom\evercoin.zip";

        private const string BlockDir = "Blocks";
        private const string TxDir = "Transactions";

        private const string EntrySep = "/";

        private readonly object zipLock = new object();

        private readonly ZipFile archive;

        public DotNetZipFileChainStore()
        {
            try
            {
                this.archive = new ZipFile(ZipFileName)
                               {
                                   UseZip64WhenSaving = Zip64Option.AsNecessary
                               };

                this.archive.CompressionLevel = CompressionLevel.BestCompression;

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
                                             TransactionIdentifiers = new MerkleTreeNode { Data = ByteTwiddling.HexStringToByteArray("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B").Reverse().GetArray() }
                                         };
                    this.PutBlock(genesisBlockIdentifier, genesisBlock);
                    this.Save();
                    Cheating.AddBlock(0, genesisBlockIdentifier);
                }
                else
                {
                    DataContractSerializer blockSerializer = new DataContractSerializer(typeof(Block));
                    Dictionary<BigInteger, BigInteger> blockIdToNextBlockIdMapping = new Dictionary<BigInteger, BigInteger>();
                    foreach (var entry in this.archive.SelectEntries(BlockDir + EntrySep + "*").Skip(1))
                    {
                        Block block;
                        using (var stream = entry.OpenReader())
                        {
                            block = (Block)blockSerializer.ReadObject(stream);
                        }

                        blockIdToNextBlockIdMapping[block.PreviousBlockIdentifier] = block.Identifier;
                    }

                    BigInteger prevBlockId = BigInteger.Zero;
                    for (int i = 0; i < blockIdToNextBlockIdMapping.Count; i++)
                    {
                        BigInteger blockId;
                        if (!blockIdToNextBlockIdMapping.TryGetValue(prevBlockId, out blockId))
                        {
                            break;
                        }

                        Cheating.AddBlock(i, blockId);
                        prevBlockId = blockId;
                    }
                }
            }
            catch
            {
                if (this.archive != null)
                {
                    this.archive.Dispose();
                }

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
            SpinWait spinner = new SpinWait();
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
                    spinner.SpinOnce();
                    Monitor.Enter(this.zipLock);
                }
                while (true);

                DataContractSerializer blockSerializer = new DataContractSerializer(typeof(Block));
                try
                {
                    using (var stream = data.OpenReader())
                    {
                        return (Block)blockSerializer.ReadObject(stream);
                    }
                }
                catch (BadStateException)
                {
                    this.Save();
                    using (var stream = data.OpenReader())
                    {
                        return (Block)blockSerializer.ReadObject(stream);
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
            SpinWait spinner = new SpinWait();
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
                    spinner.SpinOnce();
                    Monitor.Enter(this.zipLock);
                }
                while (true);

                DataContractSerializer txSerializer = new DataContractSerializer(typeof(Transaction));
                try
                {
                    using (var stream = data.OpenReader())
                    {
                        return (Transaction)txSerializer.ReadObject(stream);
                    }
                }
                catch (BadStateException)
                {
                    this.Save();
                    using (var stream = data.OpenReader())
                    {
                        return (Transaction)txSerializer.ReadObject(stream);
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
            DataContractSerializer blockSerializer = new DataContractSerializer(typeof(Block));

            lock (this.zipLock)
            {
                this.archive.AddEntry(GetBlockEntryName(blockIdentifier), (_, entryStream) => blockSerializer.WriteObject(entryStream, typedBlock));
            }
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            Transaction typedTransaction = new Transaction(transactionIdentifier, transaction);
            DataContractSerializer txSerializer = new DataContractSerializer(typeof(Transaction));

            lock (this.zipLock)
            {
                this.archive.AddEntry(GetTransactionEntryName(transactionIdentifier), (_, entryStream) => txSerializer.WriteObject(entryStream, typedTransaction));
            }
        }

        protected override void DisposeManagedResources()
        {
            this.Save();
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

        private void Save()
        {
            this.archive.Save();
        }
    }
}
