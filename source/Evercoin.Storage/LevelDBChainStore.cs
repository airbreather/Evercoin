﻿#if X64
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Evercoin.BaseImplementations;
using Evercoin.Util;

using LevelDB;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    [Export(typeof(IReadableChainStore))]
    public sealed class LevelDBChainStore : ReadWriteChainStoreBase
    {
        private const string BlockFileName = @"C:\Freedom\blocks.leveldb";
        private const string TxFileName = @"C:\Freedom\transactions.leveldb";

        private readonly DB blockDB;
        private readonly DB txDB;

        private readonly Waiter<BigInteger> blockWaiter = new Waiter<BigInteger>();
        private readonly Waiter<BigInteger> txWaiter = new Waiter<BigInteger>();

        private IChainSerializer chainSerializer;

        public LevelDBChainStore()
        {
            CopyToFile("leveldb.dll");

            Options blockOptions = new Options();
            blockOptions.Compression = CompressionType.SnappyCompression;
            blockOptions.CreateIfMissing = true;

            Options txOptions = new Options();
            txOptions.Compression = CompressionType.SnappyCompression;
            txOptions.CreateIfMissing = true;

            this.blockDB = new DB(blockOptions, BlockFileName);
            this.txDB = new DB(txOptions, TxFileName);
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

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            this.blockWaiter.WaitFor(blockIdentifier);

            string serializedBlockString = this.blockDB.Get(GetBlockKey(blockIdentifier));
            byte[] serializedBlock = ByteTwiddling.HexStringToByteArray(serializedBlockString);

            return this.chainSerializer.GetBlockForBytes(serializedBlock);
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);

            string serializedTransactionString = this.txDB.Get(GetTxKey(transactionIdentifier));
            byte[] serializedTransaction = ByteTwiddling.HexStringToByteArray(serializedTransactionString);

            return this.chainSerializer.GetTransactionForBytes(serializedTransaction);
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            byte[] serializedBlock = this.ChainSerializer.GetBytesForBlock(block);
            string serializedBlockString = ByteTwiddling.ByteArrayToHexString(serializedBlock);
            this.blockDB.Put(GetBlockKey(blockIdentifier), serializedBlockString);

            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            byte[] serializedTransaction = this.ChainSerializer.GetBytesForTransaction(transaction);
            string serializedTransactionString = ByteTwiddling.ByteArrayToHexString(serializedTransaction);
            this.txDB.Put(GetTxKey(transactionIdentifier), serializedTransactionString);

            this.txWaiter.SetEventFor(transactionIdentifier);
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return this.blockDB.Get(GetBlockKey(blockIdentifier)) != null;
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return this.txDB.Get(GetTxKey(transactionIdentifier)) != null;
        }

        protected override void DisposeManagedResources()
        {
            this.blockDB.Dispose();
            this.txDB.Dispose();
            this.blockWaiter.Dispose();
            this.txWaiter.Dispose();
            base.DisposeManagedResources();
        }

        private static string GetBlockKey(BigInteger blockIdentifier)
        {
            return FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(blockIdentifier, 32, Endianness.LittleEndian).ToString();
        }

        private static string GetTxKey(BigInteger transactionIdentifier)
        {
            return FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(transactionIdentifier, 32, Endianness.LittleEndian).ToString();
        }

        private static void CopyToFile(string resourceTag)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            string thisAssemblyFolderPath = Path.GetDirectoryName(thisAssembly.Location);
            string targetFilePath = Path.Combine(thisAssemblyFolderPath, resourceTag);

            string fullResourceTag = String.Join(".", thisAssembly.GetName().Name, resourceTag);

            byte[] resourceData;
            using (var ms = new MemoryStream())
            {
                using (var resourceStream = thisAssembly.GetManifestResourceStream(fullResourceTag))
                {
                    resourceStream.CopyTo(ms);
                }

                resourceData = ms.ToArray();
            }

            if (!File.Exists(targetFilePath) ||
                !File.ReadAllBytes(targetFilePath).SequenceEqual(resourceData))
            {
                File.WriteAllBytes(targetFilePath, resourceData);
            }
        }

        private void OnChainSerializerSet()
        {
        }
    }
}
#endif