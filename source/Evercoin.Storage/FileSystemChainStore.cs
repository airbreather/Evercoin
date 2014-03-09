using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> blockWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();
        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> txWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();

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
                this.PutBlock(genesisBlock);
            }
            else
            {
                foreach (var entry in Directory.EnumerateFiles(BlockDirName))
                {
                    byte[] blockIdentifierBytes = ByteTwiddling.HexStringToByteArray(Path.GetFileName(entry));
                    Array.Reverse(blockIdentifierBytes);
                    BigInteger blockId = new BigInteger(blockIdentifierBytes);
                    Block block;
                    using (var stream = File.OpenRead(entry))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        block = (Block)formatter.Deserialize(stream);
                    }

                    Cheating.Add((int)block.Height, blockId);
                }
            }
        }

        protected override IBlock FindBlockCore(BigInteger blockIdentifier)
        {
            string filePath = GetBlockFileName(blockIdentifier);
            do
            {
                if (File.Exists(filePath))
                {
                    break;
                }

                ManualResetEventSlim mres = this.blockWaiters.GetOrAdd(blockIdentifier, _ => new ManualResetEventSlim());
                if (mres.Wait(10000) &&
                    this.blockWaiters.TryRemove(blockIdentifier, out mres))
                {
                    mres.Dispose();
                }
            }
            while (true);

            using (FileStream stream = File.OpenRead(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Block)binaryFormatter.Deserialize(stream);
            }
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            string filePath = GetTransactionFileName(transactionIdentifier);
            do
            {
                if (File.Exists(filePath))
                {
                    break;
                }

                ManualResetEventSlim mres = this.txWaiters.GetOrAdd(transactionIdentifier, _ => new ManualResetEventSlim());
                if (mres.Wait(10000) &&
                    this.txWaiters.TryRemove(transactionIdentifier, out mres))
                {
                    mres.Dispose();
                }
            }
            while (true);

            using (FileStream stream = File.OpenRead(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Transaction)binaryFormatter.Deserialize(stream);
            }
        }

        protected override void PutBlockCore(IBlock block)
        {
            string filePath = GetBlockFileName(block.Identifier);
            Block typedBlock = new Block(block);

            using (FileStream stream = File.OpenWrite(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, typedBlock);
            }

            ManualResetEventSlim mres;
            if (this.blockWaiters.TryGetValue(block.Identifier, out mres))
            {
                mres.Set();
            }
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
            string filePath = GetTransactionFileName(transaction.Identifier);
            Transaction typedTransaction = new Transaction(transaction);

            using (FileStream stream = File.OpenWrite(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, typedTransaction);
            }

            ManualResetEventSlim mres;
            if (this.txWaiters.TryGetValue(transaction.Identifier, out mres))
            {
                mres.Set();
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

        protected override async Task<IBlock> FindBlockAsyncCore(BigInteger blockIdentifier, CancellationToken token)
        {
            string filePath = GetBlockFileName(blockIdentifier);
            do
            {
                if (File.Exists(filePath))
                {
                    break;
                }

                ManualResetEventSlim mres = this.blockWaiters.GetOrAdd(blockIdentifier, _ => new ManualResetEventSlim());
                bool success = await Task.Run(() => mres.Wait(10000, token), token);
                if (success &&
                    this.blockWaiters.TryRemove(blockIdentifier, out mres))
                {
                    mres.Dispose();
                }
            }
            while (true);

            using (FileStream stream = File.OpenRead(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Block)binaryFormatter.Deserialize(stream);
            }
        }

        protected override async Task<ITransaction> FindTransactionAsyncCore(BigInteger transactionIdentifier, CancellationToken token)
        {
            string filePath = GetTransactionFileName(transactionIdentifier);
            do
            {
                if (File.Exists(filePath))
                {
                    break;
                }

                ManualResetEventSlim mres = this.txWaiters.GetOrAdd(transactionIdentifier, _ => new ManualResetEventSlim());
                bool success = await Task.Run(() => mres.Wait(10000, token), token);
                if (success &&
                    this.txWaiters.TryRemove(transactionIdentifier, out mres))
                {
                    mres.Dispose();
                }
            }
            while (true);

            using (FileStream stream = File.OpenRead(filePath))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (Transaction)binaryFormatter.Deserialize(stream);
            }
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
