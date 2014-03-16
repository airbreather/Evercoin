using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

namespace Evercoin.Storage
{
    ////[Export(typeof(IChainStore))]
    ////[Export(typeof(IReadOnlyChainStore))]
    public sealed class FileSystemChainStore : ReadWriteChainStoreBase
    {
        private const string BlockDirName = @"C:\Freedom\blocks";
        private const string TxDirName = @"C:\Freedom\transactions";

        public FileSystemChainStore()
        {
            BigInteger genesisBlockIdentifier = new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").AsEnumerable().Reverse().GetArray());
            if (!this.ContainsBlock(genesisBlockIdentifier))
            {
                Block genesisBlock = new Block
                {
                    Identifier = genesisBlockIdentifier,
                    TypedCoinbase = new CoinbaseValueSource
                    {
                        AvailableValue = 50,
                        OriginatingBlockIdentifier = genesisBlockIdentifier
                    },
                    TransactionIdentifiers = new MerkleTreeNode { Data = ByteTwiddling.HexStringToByteArray("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B").AsEnumerable().Reverse().GetArray() }
                };
                this.PutBlock(genesisBlockIdentifier, genesisBlock);
            }
            else
            {
                Dictionary<BigInteger, BigInteger> blockIdToNextBlockIdMapping = new Dictionary<BigInteger, BigInteger>();
                foreach (string filePath in Directory.EnumerateFiles(BlockDirName))
                {
                    string name = Path.GetFileName(filePath);
                    byte[] hexBytes = ByteTwiddling.HexStringToByteArray(name);
                    Array.Reverse(hexBytes);
                    BigInteger blockId = new BigInteger(hexBytes);
                    IBlock block = this.FindBlockCore(blockId);
                    blockIdToNextBlockIdMapping[block.PreviousBlockIdentifier] = blockId;
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
            }
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            SpinWait spinner = new SpinWait();
            string filePath = GetBlockFileName(blockIdentifier);
            FileStream stream;
            do
            {
                if (!File.Exists(filePath))
                {
                    spinner.SpinOnce();
                    continue;
                }

                try
                {
                    stream = File.OpenRead(filePath);
                    break;
                }
                catch
                {
                    spinner.SpinOnce();
                }
            }
            while (true);

            using (stream)
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Block)binaryFormatter.Deserialize(stream);
            }
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            SpinWait spinner = new SpinWait();
            string filePath = GetTransactionFileName(transactionIdentifier);
            FileStream stream;
            do
            {
                if (!File.Exists(filePath))
                {
                    spinner.SpinOnce();
                    continue;
                }

                try
                {
                    stream = File.OpenRead(filePath);
                    break;
                }
                catch
                {
                    spinner.SpinOnce();
                }
            }
            while (true);

            using (stream)
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Transaction)binaryFormatter.Deserialize(stream);
            }
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            string filePath = GetBlockFileName(blockIdentifier);
            Block typedBlock = new Block(blockIdentifier, block);

            using (FileStream stream = File.OpenWrite(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, typedBlock);
            }
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            string filePath = GetTransactionFileName(transactionIdentifier);
            Transaction typedTransaction = new Transaction(transactionIdentifier, transaction);

            using (FileStream stream = File.OpenWrite(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, typedTransaction);
            }
        }

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            return File.Exists(GetBlockFileName(blockIdentifier));
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            return File.Exists(GetTransactionFileName(transactionIdentifier));
        }

        protected override async Task<bool> ContainsBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            return await Task.Run(() => File.Exists(GetBlockFileName(blockIdentifier)), token);
        }

        protected override async Task<bool> ContainsTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            return await Task.Run(() => File.Exists(GetTransactionFileName(transactionIdentifier)), token);
        }

        private static string GetBlockFileName(BigInteger blockIdentifier)
        {
            byte[] idBytes = blockIdentifier.ToLittleEndianUInt256Array();
            Array.Reverse(idBytes);
            return Path.Combine(BlockDirName, ByteTwiddling.ByteArrayToHexString(idBytes));
        }

        private static string GetTransactionFileName(BigInteger transactionIdentifier)
        {
            byte[] idBytes = transactionIdentifier.ToLittleEndianUInt256Array();
            Array.Reverse(idBytes);
            return Path.Combine(TxDirName, ByteTwiddling.ByteArrayToHexString(idBytes));
        }
    }
}
