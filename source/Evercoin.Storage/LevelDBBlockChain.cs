#if X64
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Evercoin.Util;

using LevelDb;

namespace Evercoin.Storage
{
    internal enum StaticDataPrefix : byte
    {
        HeightToBlockId,
        BlockIdToHeight,
        BlockToTransaction,
        TransactionToBlock
    }

    public sealed class LevelDBBlockChain : IBlockChain
    {
        private const byte HeightToBlock = 0;

        private const byte BlockToHeight = 1;

        private const byte BlockToTransaction = 2;

        private const byte TransactionToBlock = 3;

        private const byte Metadata = 4;

        private readonly Database db;

        private readonly object metadataLock = new object();

        private readonly ConcurrentDictionary<FancyByteArray, object> blockLocks = new ConcurrentDictionary<FancyByteArray, object>();

        public LevelDBBlockChain()
        {
            Cheating.CopyLevelDbDll();

            const string Name = @"C:\Freedom\chain.leveldb";
            LevelDbFactory factory = new LevelDbFactory();
            IDatabaseOptions databaseOptions = factory.CreateDatabaseOptions();
            databaseOptions.CreateIfMissing = true;
            databaseOptions.CompressionOption = CompressionOption.SnappyCompression;

            // Use a 256 MB cache here.
            databaseOptions.OverriddenLruCache = factory.CreateLRUCache(2 << 28);

            this.db = factory.OpenDatabase(Name, databaseOptions);
        }

        public ulong BlockCount
        {
            get
            {
                byte[] metadataKey = { Metadata };
                byte[] metadata = this.db.Get(metadataKey) ?? new byte[16];
                return BitConverter.ToUInt64(metadata, 0);
            }
        }

        public FancyByteArray? GetIdentifierOfBlockAtHeight(ulong height)
        {
            byte[] serializedHeight = BitConverter.GetBytes(height);
            byte[] key = new byte[serializedHeight.Length + 1];
            key[0] = HeightToBlock;
            Buffer.BlockCopy(serializedHeight, 0, key, 1, serializedHeight.Length);

            byte[] value = this.db.Get(key);

            if (value == null)
            {
                return default(FancyByteArray?);
            }

            return value;
        }

        public FancyByteArray? GetIdentifierOfBlockWithTransaction(FancyByteArray transactionIdentifier)
        {
            byte[] serializedTransactionId = transactionIdentifier;
            byte[] key = new byte[serializedTransactionId.Length + 1];
            key[0] = TransactionToBlock;
            Buffer.BlockCopy(serializedTransactionId, 0, key, 1, serializedTransactionId.Length);

            byte[] value = this.db.Get(key);

            if (value == null)
            {
                return default(FancyByteArray?);
            }

            return value;
        }

        public ulong? GetHeightOfBlock(FancyByteArray blockIdentifier)
        {
            byte[] serializedBlockId = blockIdentifier;
            byte[] key = new byte[serializedBlockId.Length + 1];
            key[0] = BlockToHeight;
            Buffer.BlockCopy(serializedBlockId, 0, key, 1, serializedBlockId.Length);

            byte[] value = this.db.Get(key);

            if (value == null)
            {
                return default(ulong?);
            }

            return BitConverter.ToUInt64(value, 0);
        }

        public IEnumerable<FancyByteArray> GetTransactionsForBlock(FancyByteArray blockIdentifier)
        {
            byte[] serializedBlockId = blockIdentifier;
            byte[] key = new byte[serializedBlockId.Length + 1];
            key[0] = BlockToTransaction;
            Buffer.BlockCopy(serializedBlockId, 0, key, 1, serializedBlockId.Length);

            byte[] value = this.db.Get(key);

            if (value == null)
            {
                yield break;
            }

            ulong transactionCount = BitConverter.ToUInt64(value, 0);

            int offset = 8;
            for (ulong i = 0; i < transactionCount; i++)
            {
                int l = (int)BitConverter.ToUInt64(value, offset);
                offset += 8;
                IReadOnlyList<byte> serializedTransaction = value.GetRange(offset, l);
                offset += l;
                yield return FancyByteArray.CreateFromBytes(serializedTransaction);
            }
        }

        public void RemoveBlocksAboveHeight(ulong height)
        {
            ulong blockCount = this.BlockCount;
            if (height > blockCount)
            {
                return;
            }

            this.IncrementBlockCount(height++);

            for (; height < blockCount; height++)
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

        public void AddBlockAtHeight(FancyByteArray block, ulong height)
        {
            if (height > this.BlockCount)
            {
                this.IncrementBlockCount(height);
            }

            byte[] serializedBlockId = block;
            byte[] serializedHeight = BitConverter.GetBytes(height);

            byte[] blockToHeightKey = new byte[serializedBlockId.Length + 1];
            byte[] heightToBlockKey = new byte[serializedHeight.Length + 1];
            blockToHeightKey[0] = BlockToHeight;
            heightToBlockKey[0] = HeightToBlock;

            Buffer.BlockCopy(serializedBlockId, 0, blockToHeightKey, 1, serializedBlockId.Length);
            Buffer.BlockCopy(serializedHeight, 0, heightToBlockKey, 1, serializedHeight.Length);
            this.db.Put(blockToHeightKey, serializedHeight);
            this.db.Put(heightToBlockKey, serializedBlockId);
        }

        public void AddTransactionToBlock(FancyByteArray transactionIdentifier, FancyByteArray blockIdentifier, ulong index)
        {
            lock (this.blockLocks.GetOrAdd(blockIdentifier, _ => new object()))
            {
                FancyByteArray[] currentTransactions = this.GetTransactionsForBlock(blockIdentifier).ToArray();

                if (currentTransactions.Length <= (int)index)
                {
                    Array.Resize(ref currentTransactions, (int)index + 1);
                }

                currentTransactions[index] = transactionIdentifier;

                byte[] serializedTransactionId = transactionIdentifier;
                byte[] serializedBlockId = blockIdentifier;

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
        }

        private void IncrementBlockCount(ulong? newBlockCount = null)
        {
            lock (this.metadataLock)
            {
                byte[] metadataKey = { Metadata };
                byte[] metadata = this.db.Get(metadataKey) ?? new byte[16];
                ulong blockCount = newBlockCount ?? BitConverter.ToUInt64(metadata, 0) + 1;
                Buffer.BlockCopy(BitConverter.GetBytes(blockCount), 0, metadata, 0, 8);
                this.db.Put(metadataKey, metadata);
            }
        }
    }
}
#endif