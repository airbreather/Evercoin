using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Util;

using Ionic.Zip;
using Ionic.Zlib;

namespace Evercoin.Storage
{
    // HINT: Don't use this.  It sucks.
    ////[Export(typeof(IChainStore))]
    ////[Export(typeof(IReadableChainStore))]
    public sealed class DotNetZipFileChainStore : ReadWriteChainStoreBase
    {
        private const string ZipFileName = @"C:\Freedom\evercoin.zip";

        private const string BlockDir = "Blocks";
        private const string TxDir = "Transactions";

        private const string EntrySep = "/";

        private readonly object zipLock = new object();

        private readonly ZipFile archive;

        private readonly Waiter<BigInteger> blockWaiter = new Waiter<BigInteger>();
        private readonly Waiter<BigInteger> txWaiter = new Waiter<BigInteger>();

        private IChainSerializer chainSerializer;

        public DotNetZipFileChainStore()
        {
            try
            {
                this.archive = new ZipFile(ZipFileName)
                               {
                                   UseZip64WhenSaving = Zip64Option.AsNecessary
                               };

                this.archive.CompressionLevel = CompressionLevel.BestCompression;

                if (!this.archive.ContainsEntry(BlockDir + EntrySep))
                {
                    this.archive.AddDirectoryByName(BlockDir);
                }

                if (!this.archive.ContainsEntry(TxDir + EntrySep))
                {
                    this.archive.AddDirectoryByName(TxDir);
                }
            }
            catch
            {
                if (this.archive != null)
                {
                    this.archive.Dispose();
                }

                throw;
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

        protected override bool ContainsBlockCore(BigInteger blockIdentifier)
        {
            lock (this.zipLock)
            return this.archive.ContainsEntry(GetBlockEntryName(blockIdentifier));
        }

        protected override bool ContainsTransactionCore(BigInteger transactionIdentifier)
        {
            lock (this.zipLock)
            return this.archive.ContainsEntry(GetTransactionEntryName(transactionIdentifier));
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
            this.blockWaiter.WaitFor(blockIdentifier);
            byte[] serializedBlock;
            lock (this.zipLock)
            {
                ZipEntry data = this.archive[GetBlockEntryName(blockIdentifier)];

                try
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var stream = data.OpenReader())
                        {
                            stream.CopyTo(ms);
                        }

                        serializedBlock = ms.ToArray();
                    }
                }
                catch (BadStateException)
                {
                    this.Save();
                    using (var ms = new MemoryStream())
                    {
                        using (var stream = data.OpenReader())
                        {
                            stream.CopyTo(ms);
                        }

                        serializedBlock = ms.ToArray();
                    }
                }
            }

            return this.chainSerializer.GetBlockForBytes(serializedBlock);
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            this.txWaiter.WaitFor(transactionIdentifier);
            byte[] serializedTransaction;
            lock (this.zipLock)
            {
                ZipEntry data = this.archive[GetTransactionEntryName(transactionIdentifier)];

                try
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var stream = data.OpenReader())
                        {
                            stream.CopyTo(ms);
                        }

                        serializedTransaction = ms.ToArray();
                    }
                }
                catch (BadStateException)
                {
                    this.Save();
                    using (var ms = new MemoryStream())
                    {
                        using (var stream = data.OpenReader())
                        {
                            stream.CopyTo(ms);
                        }

                        serializedTransaction = ms.ToArray();
                    }
                }
            }

            return this.chainSerializer.GetTransactionForBytes(serializedTransaction);
        }

        protected override void PutBlockCore(BigInteger blockIdentifier, IBlock block)
        {
            byte[] serializedBlock = this.chainSerializer.GetBytesForBlock(block);
            lock (this.zipLock)
            {
                this.archive.AddEntry(GetBlockEntryName(blockIdentifier), serializedBlock);
            }

            this.blockWaiter.SetEventFor(blockIdentifier);
        }

        protected override void PutTransactionCore(BigInteger transactionIdentifier, ITransaction transaction)
        {
            byte[] serializedTransaction = this.chainSerializer.GetBytesForTransaction(transaction);
            lock (this.zipLock)
            {
                this.archive.AddEntry(GetTransactionEntryName(transactionIdentifier), serializedTransaction);
            }

            this.txWaiter.SetEventFor(transactionIdentifier);
        }

        protected override void DisposeManagedResources()
        {
            this.Save();
            this.archive.Dispose();
        }

        private static string GetBlockEntryName(BigInteger blockIdentifier)
        {
            FancyByteArray bytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(blockIdentifier, 32, Endianness.LittleEndian);
            return String.Join(EntrySep, BlockDir, bytes);
        }

        private static string GetTransactionEntryName(BigInteger transactionIdentifier)
        {
            FancyByteArray bytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(transactionIdentifier, 32, Endianness.LittleEndian);
            return String.Join(EntrySep, TxDir, bytes);
        }

        private void Save()
        {
            this.archive.Save();
        }

        private void OnChainSerializerSet()
        {
            // OOH, CHEATING
            SHA256 hasher = SHA256.Create();
            Dictionary<BigInteger, BigInteger> blockIdToNextBlockIdMapping = new Dictionary<BigInteger, BigInteger>();
            foreach (var entry in this.archive.SelectEntries("*", BlockDir).Skip(1))
            {
                byte[] serializedBlock;
                using (var ms = new MemoryStream())
                {
                    using (var stream = entry.OpenReader())
                    {
                        stream.CopyTo(ms);
                    }

                    serializedBlock = ms.ToArray();
                }

                IBlock block = this.chainSerializer.GetBlockForBytes(serializedBlock);

                // OOH, CHEATING
                FancyByteArray hash = hasher.ComputeHash(hasher.ComputeHash(serializedBlock));

                blockIdToNextBlockIdMapping[block.PreviousBlockIdentifier] = hash;
            }

            BigInteger prevBlockId = BigInteger.Zero;
            for (int i = 0; i < blockIdToNextBlockIdMapping.Count; i++)
            {
                BigInteger blockId;
                if (!blockIdToNextBlockIdMapping.TryGetValue(prevBlockId, out blockId))
                {
                    break;
                }

                Cheating.AddBlock(i, blockId);
                prevBlockId = blockId;
                this.blockWaiter.SetEventFor(blockId);
            }
        }
    }
}
