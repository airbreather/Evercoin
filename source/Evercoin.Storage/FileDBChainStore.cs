using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Threading;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

using Numeria.IO;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    [Export(typeof(IReadableChainStore))]
    public sealed class FileDBChainStore : ReadWriteChainStoreBase
    {
        private const string BlockFileName = @"C:\Freedom\blocks.filedb";
        private const string TxFileName = @"C:\Freedom\transactions.filedb";

        private readonly FileDB blockDb;
        private readonly FileDB txDb;

        private readonly object blockLock = new object();
        private readonly object txLock = new object();

        private readonly ConcurrentDictionary<Guid, BigInteger> fileIdToBlockIdIndex = new ConcurrentDictionary<Guid, BigInteger>();
        private readonly ConcurrentDictionary<BigInteger, Guid> blockIdToFileIdIndex = new ConcurrentDictionary<BigInteger, Guid>();
        private readonly ConcurrentDictionary<Guid, BigInteger> fileIdToTxIdIndex = new ConcurrentDictionary<Guid, BigInteger>();
        private readonly ConcurrentDictionary<BigInteger, Guid> txIdToFileIdIndex = new ConcurrentDictionary<BigInteger, Guid>();

        public FileDBChainStore()
        {
            FileDB.CreateEmptyFile(BlockFileName, ignoreIfExists: true);
            FileDB.CreateEmptyFile(TxFileName, ignoreIfExists: true);

            try
            {
                ConcurrentDictionary<BigInteger, BigInteger> blockIdToNextBlockIdMapping = new ConcurrentDictionary<BigInteger, BigInteger>();
                this.blockDb = new FileDB(BlockFileName, FileAccess.ReadWrite);
                this.txDb = new FileDB(TxFileName, FileAccess.ReadWrite);

                foreach (EntryInfo entry in this.blockDb.ListFiles())
                {
                    try
                    {
                        using (var ms = new MemoryStream())
                        {
                            lock (this.blockLock)
                            {
                                this.blockDb.Read(entry.ID, ms);
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            var serializer = new DataContractSerializer(typeof(Block));
                            Block block = (Block)serializer.ReadObject(ms);
                            this.fileIdToBlockIdIndex[entry.ID] = blockIdToNextBlockIdMapping[block.PreviousBlockIdentifier] = block.Identifier;
                            this.blockIdToFileIdIndex[block.Identifier] = entry.ID;
                        }
                    }
                    catch
                    {
                        lock (this.blockLock)
                        {
                            this.blockDb.Delete(entry.ID);
                        }
                    }
                }

                BigInteger genesisBlockIdentifier = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").AsEnumerable().Reverse().GetArray());
                if (!this.ContainsBlockCore(genesisBlockIdentifier))
                {
                    Block genesisBlock = new Block
                                         {
                                             Identifier = genesisBlockIdentifier,
                                             TypedCoinbase = new ValueSource
                                                             {
                                                                 AvailableValue = 50
                                                             },
                                             TransactionIdentifiers = new MerkleTreeNode { Data = ByteTwiddling.HexStringToByteArray("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B").AsEnumerable().Reverse().GetArray() }
                                         };
                    this.PutBlockCore(genesisBlockIdentifier, genesisBlock);
                    Cheating.Add(0, genesisBlockIdentifier);
                }

                BigInteger prevBlockId = BigInteger.Zero;
                for (int i = 0; i < blockIdToNextBlockIdMapping.Count; i++)
                {
                    BigInteger blockId;
                    if (!blockIdToNextBlockIdMapping.TryGetValue(prevBlockId, out blockId))
                    {
                        break;
                    }

                    Cheating.Add(i, blockId);
                    prevBlockId = blockId;
                }

                HashSet<BigInteger> goodBlockIds = new HashSet<BigInteger>(blockIdToNextBlockIdMapping.Values);
                foreach (EntryInfo entry in this.blockDb.ListFiles())
                {
                    BigInteger blockId = this.fileIdToBlockIdIndex[entry.ID];
                    if (!goodBlockIds.Contains(blockId) &&
                        blockId != genesisBlockIdentifier)
                    {
                        lock (this.blockLock)
                        {
                            this.blockDb.Delete(entry.ID);
                        }
                    }
                }

                foreach (EntryInfo entry in this.txDb.ListFiles())
                {
                    try
                    {
                        using (var ms = new MemoryStream())
                        {
                            lock (this.txLock)
                            {
                                this.txDb.Read(entry.ID, ms);
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            var serializer = new DataContractSerializer(typeof(Transaction));
                            Transaction transaction = (Transaction)serializer.ReadObject(ms);
                            this.fileIdToTxIdIndex[entry.ID] = transaction.Identifier;
                            this.txIdToFileIdIndex[transaction.Identifier] = entry.ID;
                        }
                    }
                    catch
                    {
                        lock (this.txLock)
                        {
                            this.txDb.Delete(entry.ID);
                        }
                    }
                }
            }
            catch
            {
                if (this.txDb != null)
                {
                    this.txDb.Dispose();
                }

                if (this.blockDb != null)
                {
                    this.blockDb.Dispose();
                }
            }
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            SpinWait spinner = new SpinWait();
            Guid fileId;
            while (!this.blockIdToFileIdIndex.TryGetValue(blockIdentifier, out fileId))
            {
                spinner.SpinOnce();
            }

            using (var ms = new MemoryStream())
            {
                lock (this.blockLock)
                {
                    this.blockDb.Read(fileId, ms);
                }

                ms.Seek(0, SeekOrigin.Begin);
                var serializer = new DataContractSerializer(typeof(Block));
                return (Block)serializer.ReadObject(ms);
            }
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            SpinWait spinner = new SpinWait();
            Guid fileId;
            while (!this.txIdToFileIdIndex.TryGetValue(transactionIdentifier, out fileId))
            {
                spinner.SpinOnce();
            }

            using (var ms = new MemoryStream())
            {
                lock (this.txLock)
                {
                    this.txDb.Read(fileId, ms);
                }

                ms.Seek(0, SeekOrigin.Begin);
                var serializer = new DataContractSerializer(typeof(Transaction));
                return (Transaction)serializer.ReadObject(ms);
            }
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            Block typedBlock = new Block(blockIdentifier, block);
            Guid fileId;

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(Block));
                serializer.WriteObject(ms, typedBlock);
                ms.Seek(0, SeekOrigin.Begin);
                lock (this.blockLock)
                {
                    fileId = this.blockDb.Store(Guid.NewGuid().ToString(), ms).ID;
                }
            }

            this.blockIdToFileIdIndex[blockIdentifier] = fileId;
            this.fileIdToBlockIdIndex[fileId] = blockIdentifier;
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            Transaction typedTransaction = new Transaction(transactionIdentifier, transaction);
            Guid fileId;

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(Transaction));
                serializer.WriteObject(ms, typedTransaction);
                ms.Seek(0, SeekOrigin.Begin);
                lock (this.txLock)
                {
                    fileId = this.txDb.Store(Guid.NewGuid().ToString(), ms).ID;
                }
            }

            this.txIdToFileIdIndex[transactionIdentifier] = fileId;
            this.fileIdToTxIdIndex[fileId] = transactionIdentifier;
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.blockIdToFileIdIndex.ContainsKey(blockIdentifier);
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.txIdToFileIdIndex.ContainsKey(transactionIdentifier);
        }

        protected override void DisposeManagedResources()
        {
            this.blockDb.Dispose();
            this.txDb.Dispose();
        }
    }
}
