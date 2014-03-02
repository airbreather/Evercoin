﻿using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

using LevelDB;

namespace Evercoin.Storage
{
    [Export("UncachedChainStore", typeof(IChainStore))]
    public sealed class SimpleChainStore : ReadWriteChainStoreBase
    {
        private static readonly Options DbOptions = new Options
                                                    {
                                                        ////WriteBufferSize = 100 * 1024 * 1024,
                                                        CreateIfMissing = true,
                                                        ////BlockSize = 500 * 1024 * 1024
                                                    };

        private const string BlockFileName = @"C:\Freedom\blocks.leveldb";

        private const string TxFileName = @"C:\Freedom\transactions.leveldb";

        private readonly object blockLock = new object();
        private readonly object txLock = new object();

        private readonly DB blockDb = new DB(DbOptions, BlockFileName);
        private readonly DB txDb = new DB(DbOptions, TxFileName);

        public SimpleChainStore()
        {
            BigInteger genesisBlockIdentifier = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").Reverse().ToArray());
            if (!this.ContainsBlock(genesisBlockIdentifier))
            {
                Block genesisBlock = new Block
                                     {
                                         Identifier = genesisBlockIdentifier,
                                         TypedCoinbase = new CoinbaseValueSource
                                                         {
                                                             AvailableValue = 50,
                                                             OriginatingBlockIdentifier = genesisBlockIdentifier
                                                         }
                                     };
                this.PutBlock(genesisBlock);
            }
            else
            {
                using (var iterator = this.blockDb.CreateIterator())
                {
                    iterator.SeekToFirst();
                    while (iterator.IsValid())
                    {
                        Block currentBlock;
                        byte[] blockBytes = iterator.Value();
                        using (var ms = new MemoryStream(blockBytes))
                        {
                            BinaryFormatter binaryFormatter = new BinaryFormatter();
                            currentBlock = (Block)binaryFormatter.Deserialize(ms);
                        }

                        Cheating.Add((int)currentBlock.Height, currentBlock.Identifier);
                        iterator.Next();
                    }
                }
            }
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            lock (this.blockLock)
            return this.blockDb.Get(blockIdentifier.ToLittleEndianUInt256Array()) != null;
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            lock (this.txLock)
            return this.txDb.Get(transactionIdentifier.ToLittleEndianUInt256Array()) != null;
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
            byte[] data = null;
            while (data == null)
            {
                lock (this.blockLock)
                data = this.blockDb.Get(blockIdentifier.ToLittleEndianUInt256Array());
                if (data == null)
                {
                    Thread.Sleep(100);
                }

                this.ThrowIfDisposed();
            }

            using (var ms = new MemoryStream(data))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Block)binaryFormatter.Deserialize(ms);
            }
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            byte[] data = null;
            while (data == null)
            {
                lock (this.txLock)
                data = this.txDb.Get(transactionIdentifier.ToLittleEndianUInt256Array());
                if (data == null)
                {
                    Thread.Sleep(100);
                }

                this.ThrowIfDisposed();
            }

            using (var ms = new MemoryStream(data))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Transaction)binaryFormatter.Deserialize(ms);
            }
        }

        protected override void PutBlockCore(IBlock block)
        {
            Block typedBlock = new Block(block);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(ms, typedBlock);
                data = ms.ToArray();
            }

            lock (this.blockLock)
            this.blockDb.Put(block.Identifier.ToLittleEndianUInt256Array(), data);
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
            Transaction typedTransaction = new Transaction(transaction);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(ms, typedTransaction);
                data = ms.ToArray();
            }

            lock (this.txLock)
            this.txDb.Put(transaction.Identifier.ToLittleEndianUInt256Array(), data);
        }

        protected override void DisposeManagedResources()
        {
            this.blockDb.Dispose();
            this.txDb.Dispose();
            ////DB.Destroy(new Options { CreateIfMissing = true }, BlockFileName);
            ////DB.Destroy(new Options { CreateIfMissing = true }, TxFileName);
        }
    }
}