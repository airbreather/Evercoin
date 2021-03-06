﻿#if !X64
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Security.Cryptography;

using Evercoin.BaseImplementations;
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

        private readonly ConcurrentDictionary<Guid, FancyByteArray> fileIdToBlockIdIndex = new ConcurrentDictionary<Guid, FancyByteArray>();
        private readonly ConcurrentDictionary<FancyByteArray, Guid> blockIdToFileIdIndex = new ConcurrentDictionary<FancyByteArray, Guid>();
        private readonly ConcurrentDictionary<Guid, FancyByteArray> fileIdToTxIdIndex = new ConcurrentDictionary<Guid, FancyByteArray>();
        private readonly ConcurrentDictionary<FancyByteArray, Guid> txIdToFileIdIndex = new ConcurrentDictionary<FancyByteArray, Guid>();

        private readonly Waiter<FancyByteArray> blockWaiter = new Waiter<FancyByteArray>();
        private readonly Waiter<FancyByteArray> txWaiter = new Waiter<FancyByteArray>();

        private IChainSerializer chainSerializer;

        public FileDBChainStore()
        {
            FileDB.CreateEmptyFile(BlockFileName, ignoreIfExists: true);
            FileDB.CreateEmptyFile(TxFileName, ignoreIfExists: true);
            this.blockDb = new FileDB(BlockFileName, FileAccess.ReadWrite);
            this.txDb = new FileDB(TxFileName, FileAccess.ReadWrite);
        }

        [Import]
        public IChainSerializer ChainSerializer
        {
            get
            {
                return this.chainSerializer;
            }

            set
            {
                this.chainSerializer = value;
                this.OnChainSerializerSet();
            }
        }

        protected override IBlock FindBlockCore(FancyByteArray blockIdentifier)
        {
            this.blockWaiter.WaitFor(blockIdentifier);
            Guid fileId = this.blockIdToFileIdIndex[blockIdentifier];

            byte[] serializedBlock;
            using (var ms = new MemoryStream())
            {
                lock (this.blockLock)
                {
                    this.blockDb.Read(fileId, ms);
                }

                serializedBlock = ms.ToArray();
            }

            return this.chainSerializer.GetBlockForBytes(serializedBlock);
        }

        protected override ITransaction FindTransactionCore(FancyByteArray transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);
            Guid fileId = this.txIdToFileIdIndex[transactionIdentifier];

            byte[] serializedTransaction;
            using (var ms = new MemoryStream())
            {
                lock (this.txLock)
                {
                    this.txDb.Read(fileId, ms);
                }

                serializedTransaction = ms.ToArray();
            }

            return this.chainSerializer.GetTransactionForBytes(serializedTransaction);
        }

        protected override void PutBlockCore(FancyByteArray blockIdentifier, IBlock block)
        {
            Guid fileId;

            using (var ms = new MemoryStream())
            {
                byte[] serializedBlock = this.chainSerializer.GetBytesForBlock(block);
                ms.Write(serializedBlock, 0, serializedBlock.Length);

                ms.Seek(0, SeekOrigin.Begin);
                lock (this.blockLock)
                {
                    fileId = this.blockDb.Store(Guid.NewGuid().ToString(), ms).ID;
                }
            }

            this.blockIdToFileIdIndex[blockIdentifier] = fileId;
            this.fileIdToBlockIdIndex[fileId] = blockIdentifier;
            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            Guid fileId;

            using (var ms = new MemoryStream())
            {
                byte[] serializedTransaction = this.chainSerializer.GetBytesForTransaction(transaction);
                ms.Write(serializedTransaction, 0, serializedTransaction.Length);

                ms.Seek(0, SeekOrigin.Begin);
                lock (this.txLock)
                {
                    fileId = this.txDb.Store(Guid.NewGuid().ToString(), ms).ID;
                }
            }

            this.txIdToFileIdIndex[transactionIdentifier] = fileId;
            this.fileIdToTxIdIndex[fileId] = transactionIdentifier;
            this.txWaiter.SetEventFor(transactionIdentifier);
        }

        protected override bool ContainsBlockCore(FancyByteArray blockIdentifier)
        {
            return this.blockIdToFileIdIndex.ContainsKey(blockIdentifier);
        }

        protected override bool ContainsTransactionCore(FancyByteArray transactionIdentifier)
        {
            return this.txIdToFileIdIndex.ContainsKey(transactionIdentifier);
        }

        protected override void DisposeManagedResources()
        {
            this.blockDb.Dispose();
            this.txDb.Dispose();
            this.blockWaiter.Dispose();
            this.txWaiter.Dispose();
            base.DisposeManagedResources();
        }

        private void OnChainSerializerSet()
        {
            try
            {
                ConcurrentDictionary<FancyByteArray, FancyByteArray> blockIdToNextBlockIdMapping = new ConcurrentDictionary<FancyByteArray, FancyByteArray>();

                // OOH, CHEATING
                SHA256 hasher = SHA256.Create();
                foreach (EntryInfo entry in this.blockDb.ListFiles())
                {
                    try
                    {
                        byte[] serializedBlock;
                        using (var ms = new MemoryStream())
                        {
                            lock (this.blockLock)
                            {
                                this.blockDb.Read(entry.ID, ms);
                            }

                            serializedBlock = ms.ToArray();
                        }

                        IBlock block = this.chainSerializer.GetBlockForBytes(serializedBlock);

                        // OOH, CHEATING
                        FancyByteArray hash = hasher.ComputeHash(hasher.ComputeHash(serializedBlock));

                        this.fileIdToBlockIdIndex[entry.ID] = blockIdToNextBlockIdMapping[block.PreviousBlockIdentifier] = hash;
                        this.blockIdToFileIdIndex[hash] = entry.ID;
                        this.blockWaiter.SetEventFor(hash);
                    }
                    catch
                    {
                        lock (this.blockLock)
                        {
                            this.blockDb.Delete(entry.ID);
                        }
                    }
                }

                FancyByteArray genesisBlockIdentifier = FancyByteArray.CreateLittleEndianFromHexString("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F", Endianness.BigEndian);

                FancyByteArray prevBlockId = new FancyByteArray();
                for (int i = 0; i < blockIdToNextBlockIdMapping.Count; i++)
                {
                    FancyByteArray blockId;
                    if (!blockIdToNextBlockIdMapping.TryGetValue(prevBlockId, out blockId))
                    {
                        break;
                    }

                    prevBlockId = blockId;
                }

                HashSet<FancyByteArray> goodBlockIds = new HashSet<FancyByteArray>(blockIdToNextBlockIdMapping.Values);
                foreach (EntryInfo entry in this.blockDb.ListFiles())
                {
                    FancyByteArray blockId = this.fileIdToBlockIdIndex[entry.ID];
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
                    // TODO: Cheating.AddBlock on the containing block once we've found all its transactions.
                    try
                    {
                        byte[] serializedTransaction;
                        using (var ms = new MemoryStream())
                        {
                            lock (this.txLock)
                            {
                                this.txDb.Read(entry.ID, ms);
                            }

                            serializedTransaction = ms.ToArray();
                        }

                        FancyByteArray hash = hasher.ComputeHash(hasher.ComputeHash(serializedTransaction));

                        this.fileIdToTxIdIndex[entry.ID] = hash;
                        this.txIdToFileIdIndex[hash] = entry.ID;
                        this.txWaiter.SetEventFor(hash);
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
    }
}
#endif