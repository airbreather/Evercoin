using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
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
            if (!Directory.Exists(BlockDirName))
            {
                Directory.CreateDirectory(BlockDirName);
            }

            if (!Directory.Exists(TxDirName))
            {
                Directory.CreateDirectory(TxDirName);
            }

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
                Cheating.Add(0, genesisBlockIdentifier);
            }

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            ConcurrentDictionary<BigInteger, BigInteger> blockIdToNextBlockIdMapping = new ConcurrentDictionary<BigInteger, BigInteger>();
            Parallel.ForEach(Directory.EnumerateFiles(BlockDirName), options, filePath =>
            {
                string name = Path.GetFileName(filePath);
                byte[] hexBytes = ByteTwiddling.HexStringToByteArray(name);
                Array.Reverse(hexBytes);
                BigInteger blockId = new BigInteger(hexBytes);
                try
                {
                    IBlock block = this.FindBlockCore(blockId);
                    blockIdToNextBlockIdMapping[block.PreviousBlockIdentifier] = blockId;
                }
                catch
                {
                }
            });

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

            HashSet<BigInteger> goodBlockIds = new HashSet<BigInteger>(blockIdToNextBlockIdMapping.Values);
            Parallel.ForEach(Directory.EnumerateFiles(BlockDirName), options, filePath =>
            {
                string name = Path.GetFileName(filePath);
                byte[] hexBytes = ByteTwiddling.HexStringToByteArray(name);
                Array.Reverse(hexBytes);
                BigInteger blockId = new BigInteger(hexBytes);
                if (!goodBlockIds.Contains(blockId) &&
                    blockId != genesisBlockIdentifier)
                {
                    File.Delete(filePath);
                }
            });

            Parallel.ForEach(Directory.EnumerateFiles(TxDirName), options, filePath =>
            {
                string name = Path.GetFileName(filePath);
                byte[] hexBytes = ByteTwiddling.HexStringToByteArray(name);
                Array.Reverse(hexBytes);
                BigInteger transactionIdentifier = new BigInteger(hexBytes);
                try
                {
                    ITransaction tx = this.FindTransactionCore(transactionIdentifier);
                }
                catch
                {
                    File.Delete(filePath);
                }
            });
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
                var serializer = new DataContractSerializer(typeof(Block));
                return (Block)serializer.ReadObject(stream);
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
                var serializer = new DataContractSerializer(typeof(Transaction));
                return (Transaction)serializer.ReadObject(stream);
            }
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            string filePath = GetBlockFileName(blockIdentifier);
            Block typedBlock = new Block(blockIdentifier, block);

            using (FileStream stream = File.OpenWrite(filePath))
            {
                var serializer = new DataContractSerializer(typeof(Block));
                serializer.WriteObject(stream, typedBlock);
            }
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            string filePath = GetTransactionFileName(transactionIdentifier);
            Transaction typedTransaction = new Transaction(transactionIdentifier, transaction);

            using (FileStream stream = File.OpenWrite(filePath))
            {
                var serializer = new DataContractSerializer(typeof(Transaction));
                serializer.WriteObject(stream, typedTransaction);
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
