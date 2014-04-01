using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
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

        private readonly Waiter<BigInteger> blockWaiter = new Waiter<BigInteger>();
        private readonly Waiter<BigInteger> txWaiter = new Waiter<BigInteger>();

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

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            this.blockWaiter.WaitFor(blockIdentifier);
            string filePath = GetBlockFileName(blockIdentifier);
            byte[] serializedBlock = File.ReadAllBytes(filePath);
            return this.chainSerializer.GetBlockForBytes(serializedBlock);
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);
            string filePath = GetTransactionFileName(transactionIdentifier);
            byte[] serializedTransaction = File.ReadAllBytes(filePath);
            return this.chainSerializer.GetTransactionForBytes(serializedTransaction);
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            string filePath = GetBlockFileName(blockIdentifier);
            byte[] serializedBlock = this.chainSerializer.GetBytesForBlock(block);
            File.WriteAllBytes(filePath, serializedBlock);
            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            string filePath = GetTransactionFileName(transactionIdentifier);
            byte[] serializedTransaction = this.chainSerializer.GetBytesForTransaction(transaction);
            File.WriteAllBytes(filePath, serializedTransaction);
            this.txWaiter.SetEventFor(transactionIdentifier);
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return File.Exists(GetBlockFileName(blockIdentifier));
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return File.Exists(GetTransactionFileName(transactionIdentifier));
        }

        private static string GetBlockFileName(BigInteger blockIdentifier)
        {
            FancyByteArray bytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(blockIdentifier, 32, Endianness.LittleEndian);
            return Path.Combine(BlockDirName, bytes.ToString());
        }

        private static string GetTransactionFileName(BigInteger transactionIdentifier)
        {
            FancyByteArray bytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(transactionIdentifier, 32, Endianness.LittleEndian);
            return Path.Combine(TxDirName, bytes.ToString());
        }

        private void OnChainSerializerSet()
        {
            ConcurrentDictionary<BigInteger, BigInteger> blockIdToNextBlockIdMapping = new ConcurrentDictionary<BigInteger, BigInteger>();

            // OOH, CHEATING
            SHA256 hasher = SHA256.Create();
            foreach (string fileName in Directory.EnumerateFiles(BlockDirName))
            {
                byte[] serializedBlock = File.ReadAllBytes(fileName);

                // OOH, CHEATING
                FancyByteArray hash = hasher.ComputeHash(hasher.ComputeHash(serializedBlock));

                this.blockWaiter.SetEventFor(hash);
            }

            BigInteger genesisBlockIdentifier = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").AsEnumerable().Reverse().GetArray());

            BigInteger prevBlockId = BigInteger.Zero;
            for (int i = 0; i < blockIdToNextBlockIdMapping.Count; i++)
            {
                BigInteger blockId;
                if (!blockIdToNextBlockIdMapping.TryGetValue(prevBlockId, out blockId))
                {
                    break;
                }

                prevBlockId = blockId;
            }

            HashSet<BigInteger> goodBlockIds = new HashSet<BigInteger>(blockIdToNextBlockIdMapping.Values);
            foreach (string fileName in Directory.EnumerateFiles(BlockDirName))
            {
                BigInteger blockId = BigInteger.Parse(Path.GetFileName(fileName), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
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
