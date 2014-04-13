#if X64
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Evercoin.BaseImplementations;
using Evercoin.Util;
using LevelDb;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    [Export(typeof(IReadableChainStore))]
    public sealed class LevelDBChainStore : ReadWriteChainStoreBase
    {
        private const string BlockFileName = @"C:\Freedom\blocks.leveldb";
        private const string TxFileName = @"C:\Freedom\transactions.leveldb";

        private readonly Database blockDB;
        private readonly Database txDB;

        private readonly Waiter<FancyByteArray> blockWaiter = new Waiter<FancyByteArray>();
        private readonly Waiter<FancyByteArray> txWaiter = new Waiter<FancyByteArray>();

        private readonly ConcurrentDictionary<FancyByteArray, bool> transactions = new ConcurrentDictionary<FancyByteArray, bool>();

        public LevelDBChainStore()
        {
            Cheating.CopyLevelDbDll();

            LevelDbFactory factory = new LevelDbFactory();

            DatabaseOptions blockOptions = factory.CreateDatabaseOptions();
            blockOptions.CompressionOption = CompressionOption.SnappyCompression;
            blockOptions.CreateIfMissing = true;

            DatabaseOptions txOptions = factory.CreateDatabaseOptions();
            txOptions.CompressionOption = CompressionOption.SnappyCompression;
            txOptions.CreateIfMissing = true;

            this.blockDB = factory.OpenDatabase(BlockFileName, blockOptions);
            this.txDB = factory.OpenDatabase(TxFileName, txOptions);
        }

        [Import]
        public IChainSerializer ChainSerializer { get; set; }

        protected override IBlock FindBlockCore(FancyByteArray blockIdentifier)
        {
            IBlock block;
            while (!this.TryGetBlock(blockIdentifier, out block))
            {
                this.blockWaiter.WaitFor(blockIdentifier);
            }

            return block;
        }

        protected override ITransaction FindTransactionCore(FancyByteArray transactionIdentifier)
        {
            ITransaction transaction;
            while (!this.TryGetTransaction(transactionIdentifier, out transaction))
            {
                this.txWaiter.WaitFor(transactionIdentifier);
            }

            return transaction;
        }

        protected override void PutBlockCore(FancyByteArray blockIdentifier, IBlock block)
        {
            byte[] serializedBlock = this.ChainSerializer.GetBytesForBlock(block);

            this.blockDB.Put(blockIdentifier, serializedBlock);

            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            byte[] serializedTransaction = this.ChainSerializer.GetBytesForTransaction(transaction);

            this.txDB.Put(transactionIdentifier, serializedTransaction);

            this.txWaiter.SetEventFor(transactionIdentifier);
            this.transactions[transactionIdentifier] = true;
        }

        protected override bool ContainsBlockCore(FancyByteArray blockIdentifier)
        {
            return this.blockDB.Get(blockIdentifier) != null;
        }

        protected override bool ContainsTransactionCore(FancyByteArray transactionIdentifier)
        {
            return this.txDB.Get(transactionIdentifier) != null;
        }

        protected override bool TryGetBlockCore(FancyByteArray blockIdentifier, out IBlock block)
        {
            byte[] serializedBlock = this.blockDB.Get(blockIdentifier);
            if (serializedBlock == null)
            {
                block = null;
                return false;
            }

            block = this.ChainSerializer.GetBlockForBytes(serializedBlock);
            return true;
        }

        protected override bool TryGetTransactionCore(FancyByteArray transactionIdentifier, out ITransaction transaction)
        {
            byte[] serializedTransaction = this.txDB.Get(transactionIdentifier);
            if (serializedTransaction == null)
            {
                transaction = null;
                return false;
            }

            transaction = this.ChainSerializer.GetTransactionForBytes(serializedTransaction);
            return true;
        }

        protected override void DisposeManagedResources()
        {
            this.blockDB.Dispose();
            this.txDB.Dispose();
            this.blockWaiter.Dispose();
            this.txWaiter.Dispose();
            base.DisposeManagedResources();
        }
    }
}
#endif