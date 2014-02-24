using System.ComponentModel.Composition;
using System.IO;

using Evercoin.BaseImplementations;

using ProtoBuf;

namespace Evercoin.Storage
{
    [PartNotDiscoverable]
    public sealed class DiskChainStorage : ChainStoreBase
    {
        private readonly string blockStoragePath;
        private readonly string transactionStoragePath;

        private readonly object blockStorageLock = new object();
        private readonly object transactionStorageLock = new object();

        public DiskChainStorage(string blockStoragePath, string transactionStoragePath)
        {
            this.blockStoragePath = blockStoragePath;
            this.transactionStoragePath = transactionStoragePath;
            Directory.CreateDirectory(blockStoragePath);
            Directory.CreateDirectory(transactionStoragePath);
        }

        public override bool TryGetBlock(string blockIdentifier, out IBlock block)
        {
            block = null;
            string blockPath = this.GetBlockPath(blockIdentifier);

            lock (this.blockStorageLock)
            {
                if (!File.Exists(blockPath))
                {
                    return false;
                }

                using (FileStream file = File.Open(blockPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    block = Serializer.Deserialize<SerializableBlock>(file);
                    return true;
                }
            }
        }

        public override bool TryGetTransaction(string transactionIdentifier, out ITransaction transaction)
        {
            transaction = null;
            string transactionPath = this.GetTransactionPath(transactionIdentifier);

            lock (this.transactionStorageLock)
            {
                if (!File.Exists(transactionPath))
                {
                    return false;
                }

                using (FileStream file = File.Open(transactionPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    transaction = Serializer.Deserialize<SerializableTransaction>(file);
                    return true;
                }
            }
        }

        public override void PutBlock(IBlock block)
        {
            string blockPath = this.GetBlockPath(block.Identifier);

            SerializableBlock blockToSerialize = block as SerializableBlock ?? new SerializableBlock(block);
            lock (this.blockStorageLock)
            using (FileStream file = File.Open(blockPath, FileMode.Append, FileAccess.Write))
            {
                Serializer.Serialize(file, blockToSerialize);
            }
        }

        public override void PutTransaction(ITransaction transaction)
        {
            string transactionPath = this.GetTransactionPath(transaction.Identifier);

            SerializableTransaction transactionToSerialize = transaction as SerializableTransaction ?? new SerializableTransaction(transaction);
            lock (this.transactionStorageLock)
            using (FileStream file = File.Open(transactionPath, FileMode.Append, FileAccess.Write))
            {
                Serializer.Serialize(file, transactionToSerialize);
            }
        }

        private string GetBlockPath(string blockIdentifier)
        {
            return Path.Combine(this.blockStoragePath, blockIdentifier);
        }
        private string GetTransactionPath(string transactionIdentifier)
        {
            return Path.Combine(this.transactionStoragePath, transactionIdentifier);
        }
    }
}
