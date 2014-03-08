﻿using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.BaseImplementations;
using Evercoin.Storage.Model;
using Evercoin.Util;

using Ionic.Zip;

using NodaTime;

namespace Evercoin.Storage
{
    ////[Export("UncachedChainStore", typeof(IChainStore))]
    public sealed class ZipFileChainStore : ReadWriteChainStoreBase
    {
        private const string ZipFileName = @"C:\Freedom\evercoin.zip";

        private const string BlockDir = "Blocks";
        private const string TxDir = "Transactions";

        private const string EntrySep = "/";

        private readonly object zipLock = new object();

        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> blockWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();
        private readonly ConcurrentDictionary<BigInteger, ManualResetEventSlim> txWaiters = new ConcurrentDictionary<BigInteger, ManualResetEventSlim>();

        private readonly ZipFile archive;

        private Instant lastSave = Instant.FromDateTimeUtc(DateTime.UtcNow);

        public ZipFileChainStore()
        {
            this.archive = new ZipFile(ZipFileName) { UseZip64WhenSaving = Zip64Option.Always };
            try
            {
                if (!this.archive.ContainsEntry(BlockDir + EntrySep))
                {
                    this.archive.AddDirectoryByName(BlockDir);
                }

                if (!this.archive.ContainsEntry(TxDir + EntrySep))
                {
                    this.archive.AddDirectoryByName(TxDir);
                }

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
                                                             },
                                             TransactionIdentifiers = new MerkleTreeNode { Data = ByteTwiddling.HexStringToByteArray("4A5E1E4BAAB89F3A32518A88C31BC87F618F76673E2CC77AB2127B7AFDEDA33B").Reverse().ToImmutableList() }
                                         };
                    this.PutBlock(genesisBlock);
                    this.archive.Save();
                }
                else
                {
                    foreach (var entry in this.archive.SelectEntries(BlockDir + EntrySep + "*"))
                    {
                        string fn = entry.FileName.Replace(BlockDir + EntrySep, String.Empty);
                        if (fn.Length == 0)
                        {
                            continue;
                        }

                        byte[] blockIdentifierBytes = ByteTwiddling.HexStringToByteArray(fn);
                        Array.Reverse(blockIdentifierBytes);
                        BigInteger blockId = new BigInteger(blockIdentifierBytes);
                        Block block;
                        using (var stream = entry.OpenReader())
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            block = (Block)formatter.Deserialize(stream);
                        }

                        Cheating.Add((int)block.Height, blockId);
                    }
                }
            }
            catch
            {
                this.archive.Dispose();
                throw;
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
            do
            {
                lock (this.zipLock)
                {
                    var data = this.archive[GetBlockEntryName(blockIdentifier)];
                    if (data != null)
                    {
                        using (var stream = data.OpenReader())
                        {
                            BinaryFormatter binaryFormatter = new BinaryFormatter();
                            return (Block)binaryFormatter.Deserialize(stream);
                        }
                    }
                }

                ManualResetEventSlim mres = this.blockWaiters.GetOrAdd(blockIdentifier, _ => new ManualResetEventSlim());
                if (mres.Wait(10000) &&
                    this.blockWaiters.TryRemove(blockIdentifier, out mres))
                {
                    mres.Dispose();
                }
            }
            while (true);
        }

        protected override ITransaction FindTransactionCore(BigInteger transactionIdentifier)
        {
            do
            {
                lock (this.zipLock)
                {
                    var data = this.archive[GetTransactionEntryName(transactionIdentifier)];
                    if (data != null)
                    {
                        using (var stream = data.OpenReader())
                        {
                            BinaryFormatter binaryFormatter = new BinaryFormatter();
                            return (Transaction)binaryFormatter.Deserialize(stream);
                        }
                    }
                }

                ManualResetEventSlim mres = this.txWaiters.GetOrAdd(transactionIdentifier, _ => new ManualResetEventSlim());
                if (mres.Wait(10000) &&
                    this.txWaiters.TryRemove(transactionIdentifier, out mres))
                {
                    mres.Dispose();
                }
            }
            while (true);
        }

        protected override void PutBlockCore(IBlock block)
        {
            Block typedBlock = new Block(block);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            lock (this.zipLock)
            {
                this.archive.AddEntry(GetBlockEntryName(typedBlock), (_, entryStream) => binaryFormatter.Serialize(entryStream, typedBlock));
                this.Save();
            }

            ManualResetEventSlim mres;
            if (this.blockWaiters.TryRemove(block.Identifier, out mres))
            {
                mres.Set();
            }
        }

        protected override void PutTransactionCore(ITransaction transaction)
        {
            Transaction typedTransaction = new Transaction(transaction);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            lock (this.zipLock)
            {
                this.archive.AddEntry(GetTransactionEntryName(typedTransaction), (_, entryStream) => binaryFormatter.Serialize(entryStream, typedTransaction));
                this.Save();
            }

            ManualResetEventSlim mres;
            if (this.txWaiters.TryRemove(transaction.Identifier, out mres))
            {
                mres.Set();
            }
        }

        protected override void DisposeManagedResources()
        {
            this.archive.Save();
            this.archive.Dispose();
        }

        private static string GetBlockEntryName(IBlock block)
        {
            return GetBlockEntryName(block.Identifier);
        }

        private static string GetBlockEntryName(BigInteger blockIdentifier)
        {
            byte[] blockIdentifierBytes = blockIdentifier.ToLittleEndianUInt256Array();
            Array.Reverse(blockIdentifierBytes);
            string id = ByteTwiddling.ByteArrayToHexString(blockIdentifierBytes);
            return String.Join(EntrySep, BlockDir, id);
        }

        private static string GetTransactionEntryName(ITransaction transaction)
        {
            return GetTransactionEntryName(transaction.Identifier);
        }

        private static string GetTransactionEntryName(BigInteger transactionIdentifier)
        {
            byte[] transactionIdentifierBytes = transactionIdentifier.ToLittleEndianUInt256Array();
            Array.Reverse(transactionIdentifierBytes);
            string id = ByteTwiddling.ByteArrayToHexString(transactionIdentifierBytes);
            return String.Join(EntrySep, TxDir, id);
        }

        private void Save()
        {
            Instant now = Instant.FromDateTimeUtc(DateTime.UtcNow);
            if (now - this.lastSave > Duration.FromSeconds(1))
            {
                this.archive.Save();
                this.lastSave = Instant.FromDateTimeUtc(DateTime.UtcNow);
            }
        }
    }
}