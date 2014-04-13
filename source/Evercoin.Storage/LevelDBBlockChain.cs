#if X64
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Evercoin.Util;

using LevelDb;

namespace Evercoin.Storage
{
    public sealed class LevelDBBlockChain : DisposableObject, IBlockChain
    {
        private const byte HeightToBlock = 0;

        private const byte BlockToHeight = 1;

        private const byte BlockToTransaction = 2;

        private const byte TransactionToBlock = 3;

        private const byte Metadata = 4;

        private readonly Database db;

        private readonly object metadataLock = new object();

        private readonly ConcurrentDictionary<FancyByteArray, object> blockLocks = new ConcurrentDictionary<FancyByteArray, object>();

        private readonly Waiter<ulong> blockHeightWaiter = new Waiter<ulong>();

        private readonly Waiter<FancyByteArray> blockIdWaiter = new Waiter<FancyByteArray>();

        private readonly Waiter<FancyByteArray> txIdWaiter = new Waiter<FancyByteArray>();

        private ulong? blockCount;

        public LevelDBBlockChain()
        {
            Cheating.CopyLevelDbDll();

            const string Name = @"C:\Freedom\chain.leveldb";
            LevelDbFactory factory = new LevelDbFactory();
            IDatabaseOptions databaseOptions = factory.CreateDatabaseOptions();
            databaseOptions.CreateIfMissing = true;
            databaseOptions.CompressionOption = CompressionOption.SnappyCompression;

            // Use a 256 MB cache here.
            databaseOptions.OverriddenLruCache = factory.CreateLRUCache(2 << 27);

            this.db = factory.OpenDatabase(Name, databaseOptions);
        }

        public ulong BlockCount
        {
            get
            {
                ulong? currentBlockCount = this.blockCount;
                if (currentBlockCount.HasValue)
                {
                    return currentBlockCount.Value;
                }

                lock (metadataLock)
                {
                    currentBlockCount = this.blockCount;
                    if (currentBlockCount.HasValue)
                    {
                        return currentBlockCount.Value;
                    }

                    byte[] metadataKey = { Metadata };
                    byte[] metadata = this.db.Get(metadataKey) ?? new byte[16];
                    this.blockCount = BitConverter.ToUInt64(metadata, 0);
                    return this.blockCount.Value;
                }
            }
        }

        public FancyByteArray GetIdentifierOfBlockAtHeight(ulong height)
        {
            FancyByteArray result;
            while (!this.TryGetIdentifierOfBlockAtHeight(height, out result))
            {
                this.blockHeightWaiter.WaitFor(height);
            }

            return result;
        }

        public FancyByteArray GetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier)
        {
            FancyByteArray result;
            while (!this.TryGetIdentifierOfBlockWithTransaction(transactionIdentifier, out result))
            {
                this.txIdWaiter.WaitFor(transactionIdentifier);
            }

            return result;
        }

        public ulong GetHeightOfBlock(FancyByteArray blockIdentifier)
        {
            ulong result;
            while (!this.TryGetHeightOfBlock(blockIdentifier, out result))
            {
                this.blockIdWaiter.WaitFor(blockIdentifier);
            }

            return result;
        }

        public IEnumerable<FancyByteArray> GetTransactionsForBlock(FancyByteArray blockIdentifier)
        {
            IEnumerable<FancyByteArray> result;
            while (!this.TryGetTransactionsForBlock(blockIdentifier, out result))
            {
                this.blockIdWaiter.WaitFor(blockIdentifier);
            }

            return result;
        }

        public bool TryGetIdentifierOfBlockAtHeight(ulong height, out FancyByteArray blockIdentifier)
        {
            byte[] serializedHeight = BitConverter.GetBytes(height);
            byte[] key = new byte[serializedHeight.Length + 1];
            key[0] = HeightToBlock;
            Buffer.BlockCopy(serializedHeight, 0, key, 1, serializedHeight.Length);

            byte[] data = this.db.Get(key);
            if (data == null)
            {
                blockIdentifier = default(FancyByteArray);
                return false;
            }

            blockIdentifier = data;
            return true;
        }

        public bool TryGetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier, out FancyByteArray blockIdentifier)
        {
            byte[] serializedTransactionId = transactionIdentifier;
            byte[] key = new byte[serializedTransactionId.Length + 1];
            key[0] = TransactionToBlock;
            Buffer.BlockCopy(serializedTransactionId, 0, key, 1, serializedTransactionId.Length);

            byte[] data = this.db.Get(key);
            if (data == null)
            {
                blockIdentifier = default(FancyByteArray);
                return false;
            }

            blockIdentifier = this.db.Get(key);
            return true;
        }

        public bool TryGetHeightOfBlock(FancyByteArray blockIdentifier, out ulong height)
        {
            byte[] serializedBlockId = blockIdentifier;
            byte[] key = new byte[serializedBlockId.Length + 1];
            key[0] = BlockToHeight;
            Buffer.BlockCopy(serializedBlockId, 0, key, 1, serializedBlockId.Length);

            byte[] data = this.db.Get(key);
            if (data == null)
            {
                height = 0;
                return false;
            }

            height = BitConverter.ToUInt64(data, 0);
            return true;
        }

        public bool TryGetTransactionsForBlock(FancyByteArray blockIdentifier, out IEnumerable<FancyByteArray> transactionIdentifiers)
        {
            byte[] serializedBlockId = blockIdentifier;
            byte[] key = new byte[serializedBlockId.Length + 1];
            key[0] = BlockToTransaction;
            Buffer.BlockCopy(serializedBlockId, 0, key, 1, serializedBlockId.Length);

            byte[] data = this.db.Get(key);

            if (data == null)
            {
                transactionIdentifiers = null;
                return false;
            }

            transactionIdentifiers = GetTransactionIdentifiers(data);
            return true;
        }

        public void RemoveBlocksAboveHeight(ulong height)
        {
            if (height >= this.BlockCount)
            {
                return;
            }

            this.SetBlockCount(++height, false);

            for (; height < this.BlockCount; height++)
            {
                byte[] serializedHeight = BitConverter.GetBytes(height);
                byte[] key = new byte[serializedHeight.Length + 1];
                key[0] = HeightToBlock;
                Buffer.BlockCopy(serializedHeight, 0, key, 1, serializedHeight.Length);

                byte[] serializedBlockId = this.db.Get(key);
                if (serializedBlockId == null)
                {
                    continue;
                }

                this.db.Delete(key);
                key = new byte[serializedBlockId.Length + 1];
                Buffer.BlockCopy(serializedBlockId, 0, key, 1, serializedBlockId.Length);

                key[0] = BlockToHeight;
                this.db.Delete(key);

                key[0] = BlockToTransaction;
                byte[] serializedTransactions = this.db.Get(key);
                if (serializedTransactions != null)
                {
                    this.db.Delete(key);
                }
            }
        }

        private static IEnumerable<FancyByteArray> GetTransactionIdentifiers(byte[] data)
        {
            ulong transactionCount = BitConverter.ToUInt64(data, 0);

            int offset = 8;
            for (ulong i = 0; i < transactionCount; i++)
            {
                int l = (int)BitConverter.ToUInt64(data, offset);
                offset += 8;
                IReadOnlyList<byte> serializedTransaction = data.GetRange(offset, l);
                offset += l;
                yield return FancyByteArray.CreateFromBytes(serializedTransaction);
            }
        }

        public void AddBlockAtHeight(FancyByteArray block, ulong height)
        {
            this.SetBlockCount(height + 1, true);

            byte[] serializedBlockId = block;
            byte[] serializedHeight = BitConverter.GetBytes(height);

            byte[] blockToHeightKey = new byte[serializedBlockId.Length + 1];
            byte[] heightToBlockKey = new byte[serializedHeight.Length + 1];
            blockToHeightKey[0] = BlockToHeight;
            heightToBlockKey[0] = HeightToBlock;

            Buffer.BlockCopy(serializedBlockId, 0, blockToHeightKey, 1, serializedBlockId.Length);
            Buffer.BlockCopy(serializedHeight, 0, heightToBlockKey, 1, serializedHeight.Length);

            this.db.Put(blockToHeightKey, serializedHeight);
            this.blockHeightWaiter.SetEventFor(height);

            this.db.Put(heightToBlockKey, serializedBlockId);
            this.blockIdWaiter.SetEventFor(block);
        }

        public void AddTransactionToBlock(FancyByteArray transactionIdentifier, FancyByteArray blockIdentifier, ulong index)
        {
            lock (this.blockLocks.GetOrAdd(blockIdentifier, _ => new object()))
            {
                byte[] serializedBlockId = blockIdentifier;

                IEnumerable<FancyByteArray> transactionsForBlock;
                if (!this.TryGetTransactionsForBlock(blockIdentifier, out transactionsForBlock))
                {
                    transactionsForBlock = Enumerable.Empty<FancyByteArray>();
                }

                FancyByteArray[] currentTransactions = transactionsForBlock.GetArray();

                if (currentTransactions.Length <= (int)index)
                {
                    Array.Resize(ref currentTransactions, (int)index + 1);
                }

                currentTransactions[index] = transactionIdentifier;

                byte[] serializedTransactionId = transactionIdentifier;

                byte[] transactionToBlockKey = new byte[serializedTransactionId.Length + 1];
                byte[] blockToTransactionKey = new byte[serializedBlockId.Length + 1];

                transactionToBlockKey[0] = TransactionToBlock;
                blockToTransactionKey[0] = BlockToTransaction;

                Buffer.BlockCopy(serializedTransactionId, 0, transactionToBlockKey, 1, serializedTransactionId.Length);
                Buffer.BlockCopy(serializedBlockId, 0, blockToTransactionKey, 1, serializedBlockId.Length);

                int count = currentTransactions.Length;
                byte[] serializedTransactionCount = BitConverter.GetBytes((ulong)count);
                List<byte[]> serializedTransactions = currentTransactions.Select(x => x.Value).ToList();
                byte[] serializedTransactionData = ByteTwiddling.ConcatenateData(serializedTransactions.Select(x => ByteTwiddling.ConcatenateData(BitConverter.GetBytes((ulong)x.Length), x)));

                byte[] blockToTransactionValue = ByteTwiddling.ConcatenateData(serializedTransactionCount, serializedTransactionData);
                this.db.Put(transactionToBlockKey, blockIdentifier);
                this.db.Put(blockToTransactionKey, blockToTransactionValue);
            }

            this.txIdWaiter.SetEventFor(transactionIdentifier);
        }

        protected override void DisposeManagedResources()
        {
            this.db.Dispose();
            this.blockHeightWaiter.Dispose();
            this.blockIdWaiter.Dispose();
            this.txIdWaiter.Dispose();
            base.DisposeManagedResources();
        }

        private void SetBlockCount(ulong newBlockCount, bool max)
        {
            lock (this.metadataLock)
            {
                byte[] metadataKey = { Metadata };
                byte[] metadata = this.db.Get(metadataKey) ?? new byte[16];

                if (max && BitConverter.ToUInt64(metadata, 0) > newBlockCount)
                {
                    return;
                }

                Buffer.BlockCopy(BitConverter.GetBytes(newBlockCount), 0, metadata, 0, 8);
                this.db.Put(metadataKey, metadata);
                this.blockCount = newBlockCount;
            }
        }
    }
}
#endif