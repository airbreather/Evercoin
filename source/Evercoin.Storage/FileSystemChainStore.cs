using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Security.Cryptography;

using Evercoin.BaseImplementations;
using Evercoin.Util;

namespace Evercoin.Storage
{
    ////[Export(typeof(IChainStore))]
    ////[Export(typeof(IReadableChainStore))]
    public sealed class FileSystemChainStore : ReadWriteChainStoreBase
    {
        private const string BlockDirName = @"C:\Freedom\blocks";
        private const string TxDirName = @"C:\Freedom\transactions";

        private readonly Waiter<FancyByteArray> blockWaiter = new Waiter<FancyByteArray>();
        private readonly Waiter<FancyByteArray> txWaiter = new Waiter<FancyByteArray>();

        private IChainSerializer chainSerializer;

        public FileSystemChainStore()
        {
            if (!Directory.Exists(BlockDirName))
            {
                Directory.CreateDirectory(BlockDirName);
            }

            if (!Directory.Exists(TxDirName))
            {
                Directory.CreateDirectory(TxDirName);
            }
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
            string filePath = GetBlockFileName(blockIdentifier);
            byte[] serializedBlock = File.ReadAllBytes(filePath);
            return this.chainSerializer.GetBlockForBytes(serializedBlock);
        }

        protected override ITransaction FindTransactionCore(FancyByteArray transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);
            string filePath = GetTransactionFileName(transactionIdentifier);
            byte[] serializedTransaction = File.ReadAllBytes(filePath);
            return this.chainSerializer.GetTransactionForBytes(serializedTransaction);
        }

        protected override void PutBlockCore(FancyByteArray blockIdentifier, IBlock block)
        {
            string filePath = GetBlockFileName(blockIdentifier);
            byte[] serializedBlock = this.chainSerializer.GetBytesForBlock(block);
            File.WriteAllBytes(filePath, serializedBlock);
            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(FancyByteArray transactionIdentifier, ITransaction transaction)
        {
            string filePath = GetTransactionFileName(transactionIdentifier);
            byte[] serializedTransaction = this.chainSerializer.GetBytesForTransaction(transaction);
            File.WriteAllBytes(filePath, serializedTransaction);
            this.txWaiter.SetEventFor(transactionIdentifier);
        }

        protected override bool ContainsBlockCore(FancyByteArray blockIdentifier)
        {
            return File.Exists(GetBlockFileName(blockIdentifier));
        }

        protected override bool ContainsTransactionCore(FancyByteArray transactionIdentifier)
        {
            return File.Exists(GetTransactionFileName(transactionIdentifier));
        }

        private static string GetBlockFileName(FancyByteArray blockIdentifier)
        {
            return Path.Combine(BlockDirName, blockIdentifier.ToString());
        }

        private static string GetTransactionFileName(FancyByteArray transactionIdentifier)
        {
            return Path.Combine(TxDirName, transactionIdentifier.ToString());
        }

        private void OnChainSerializerSet()
        {
            ConcurrentDictionary<FancyByteArray, FancyByteArray> blockIdToNextBlockIdMapping = new ConcurrentDictionary<FancyByteArray, FancyByteArray>();

            // OOH, CHEATING
            SHA256 hasher = SHA256.Create();
            foreach (string fileName in Directory.EnumerateFiles(BlockDirName))
            {
                byte[] serializedBlock = File.ReadAllBytes(fileName);

                // OOH, CHEATING
                FancyByteArray hash = hasher.ComputeHash(hasher.ComputeHash(serializedBlock));

                this.blockWaiter.SetEventFor(hash);
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
            foreach (string fileName in Directory.EnumerateFiles(BlockDirName))
            {
                FancyByteArray blockId = FancyByteArray.CreateLittleEndianFromHexString(Path.GetFileName(fileName), Endianness.BigEndian);
                if (!goodBlockIds.Contains(blockId) &&
                    blockId != genesisBlockIdentifier)
                {
                    File.Delete(fileName);
                }
            }

            foreach (string fileName in Directory.EnumerateFiles(TxDirName))
            {
                // TODO: Cheating.AddBlock on the containing block once we've found all its transactions.
                byte[] serializedTransaction = File.ReadAllBytes(fileName);

                FancyByteArray hash = hasher.ComputeHash(hasher.ComputeHash(serializedTransaction));

                this.txWaiter.SetEventFor(hash);
            }
        }
    }
}
