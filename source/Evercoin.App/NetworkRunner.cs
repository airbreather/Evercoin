using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.App
{
    public sealed class NetworkRunner
    {
        private readonly IBlockChain blockChain;

        private readonly ICurrencyParameters currencyParameters;

        private readonly IChainSerializer chainSerializer;

        private readonly ICurrencyNetwork network;

        private readonly IChainStore chainStore;

        private readonly object syncLock = new object();

        private ulong dataBlock;

        private ulong validationBlock;

        public NetworkRunner(ICurrencyNetwork network, IChainStore chainStore, IChainSerializer chainSerializer, ICurrencyParameters currencyParameters, IBlockChain blockChain)
        {
            this.network = network;
            this.chainStore = chainStore;
            this.chainSerializer = chainSerializer;
            this.currencyParameters = currencyParameters;
            this.blockChain = blockChain;
        }

        public async Task Run(CancellationToken token)
        {
            IBlock genesisBlock = this.currencyParameters.ChainParameters.GenesisBlock;
            IHashAlgorithmStore hashAlgorithmStore = this.currencyParameters.HashAlgorithmStore;
            Guid blockHashAlgorithmIdentifier = this.currencyParameters.ChainParameters.BlockHashAlgorithmIdentifier;
            IHashAlgorithm blockHashAlgorithm = hashAlgorithmStore.GetHashAlgorithm(blockHashAlgorithmIdentifier);
            byte[] genesisBlockData = this.currencyParameters.ChainSerializer.GetBytesForBlock(genesisBlock);
            FancyByteArray genesisBlockIdentifier = blockHashAlgorithm.CalculateHash(genesisBlockData);
            List<BigInteger> blockIdentifiers = new List<BigInteger> { genesisBlockIdentifier };
            HashSet<ulong> heights = new HashSet<ulong>();
            long validTransactions = 0;

            ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector> knownInventory = new ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector>();
            List<IPEndPoint> endPoints = new List<IPEndPoint>
                                         {
                                             new IPEndPoint(IPAddress.Loopback, 8333),
                                         };

            this.network.ReceivedVersionPackets.Subscribe(async x => await this.network.AcknowledgePeerVersionAsync(x.Item1, token).ConfigureAwait(false));
            this.network.ReceivedInventoryOffers.Subscribe(async x => await this.network.RequestInventoryAsync(x.Item2.Where(y => knownInventory.TryAdd(y, y)), token).ConfigureAwait(false));
            this.network.ReceivedBlocks.SubscribeOn(Scheduler.Immediate).ObserveOn(TaskPoolScheduler.Default).Subscribe(async x => await this.HandleBlock(x.Item2, token).ConfigureAwait(false));
            this.network.ReceivedTransactions.SubscribeOn(Scheduler.Immediate).ObserveOn(TaskPoolScheduler.Default).Subscribe(async x => await this.HandleTransaction(x.Item2, x.Item3, x.Item4, token).ConfigureAwait(false));

            this.network.ReceivedVersionAcknowledgements.Subscribe
            (
                async x =>
                {
                    ulong startingBlockHeight = this.blockChain.Length;
                    ulong currentBlockHeight = startingBlockHeight;
                    bool started = false;
                    const int CurrentHighestBlockBecauseIAmCheating = 294361;

                    Action updateBlockIdentifiers = () =>
                    {
                        for (ulong ii = 0; ii < currentBlockHeight; ii++)
                        {
                            if (heights.Add(ii))
                            {
                                blockIdentifiers.Add(this.blockChain.GetIdentifierOfBlockAtHeight(ii).Value);
                            }
                        }
                    };

                    // Start by pulling all the headers
                    while (!token.IsCancellationRequested &&
                            currentBlockHeight < CurrentHighestBlockBecauseIAmCheating)
                    {
                        if (started)
                        {
                            await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested ||
                                                                            currentBlockHeight >= CurrentHighestBlockBecauseIAmCheating ||
                                                                            ((currentBlockHeight = this.blockChain.Length) - startingBlockHeight) % 2000 == 0),
                                token).ConfigureAwait(false);

                            updateBlockIdentifiers();
                        }

                        started = true;
                        await this.network.RequestBlockOffersAsync(x, blockIdentifiers, BlockRequestType.HeadersOnly, token).ConfigureAwait(false);

                        if (currentBlockHeight >= CurrentHighestBlockBecauseIAmCheating)
                        {
                            updateBlockIdentifiers();
                            break;
                        }

                        await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested || currentBlockHeight >= CurrentHighestBlockBecauseIAmCheating || currentBlockHeight != (currentBlockHeight = this.blockChain.Length)), token).ConfigureAwait(false);
                    }

                    updateBlockIdentifiers();

                    // Now, get all the transactions.
                    ulong i = 1;
                    while (true)
                    {
                        Task t = this.network.RequestBlockOffersAsync(x, ((IReadOnlyList<BigInteger>)blockIdentifiers).GetRange(0, (int)i), BlockRequestType.IncludeTransactions, token);
                        if (i >= currentBlockHeight - 1)
                        {
                            await t.ConfigureAwait(false);
                            break;
                        }

                        i += 500;
                        i = Math.Min(i, currentBlockHeight - 1);
                        await t.ConfigureAwait(false);
                    }

                    // Now, validate all the transactions and blocks.
                    ulong validatedBlockHeight = 1;
                    IHashAlgorithm txHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.currencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier);
                    while (validatedBlockHeight < currentBlockHeight)
                    {
                        this.validationBlock = validatedBlockHeight;
                        BigInteger blockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(validatedBlockHeight).Value;
                        IBlock block = await this.chainStore.GetBlockAsync(blockIdentifier, token).ConfigureAwait(false);
                        FancyByteArray expectedMerkleRoot = block.TransactionIdentifiers.Data;
                        await Task.Run(() => SpinWait.SpinUntil(() =>
                        {
                            List<FancyByteArray> transactionData = this.blockChain.GetTransactionsForBlock(blockIdentifier).Select(trid => FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(trid, 32, Endianness.LittleEndian)).ToList();
                            if (transactionData.Count == 0)
                            {
                                return false;
                            }

                            IMerkleTreeNode tree = transactionData.Select(tt => (byte[])tt).ToMerkleTree(txHashAlgorithm);
                            FancyByteArray actualMerkleRoot = FancyByteArray.CreateFromBytes(tree.Data);
                            return expectedMerkleRoot == actualMerkleRoot;
                        }), token).ConfigureAwait(false);

                        await Task.Run(() => Parallel.ForEach(this.blockChain.GetTransactionsForBlock(blockIdentifier), transasctionIdentifier =>
                        {
                            if (!this.chainStore.ContainsTransaction(transasctionIdentifier))
                            {
                                Console.WriteLine();
                                FancyByteArray trid = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(transasctionIdentifier, 32, Endianness.LittleEndian);
                                Console.WriteLine("A MISSING TRANSACTION: {0}", trid);
                                return;
                            }

                            ITransaction transaction = this.chainStore.GetTransaction(transasctionIdentifier);
                            if (!this.currencyParameters.ChainValidator.ValidateTransaction(transaction))
                            {
                                FancyByteArray trid = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(transasctionIdentifier, 32, Endianness.LittleEndian);
                                Console.WriteLine("A BAD TRANSACTION: {0}", trid);
                                return;
                            }

                            Interlocked.Increment(ref validTransactions);
                        }), token).ConfigureAwait(false);

                        validatedBlockHeight++;
                    }
                }
            );

            this.network.PeerConnections.Subscribe
            (
                async x =>
                {
                    switch (x.PeerConnectionDirection)
                    {
                        case ConnectionDirection.Outgoing:
                            await this.network.AnnounceVersionToPeerAsync(x, this.network.CurrencyParameters.NetworkParameters.ProtocolVersion, 1, Instant.FromDateTimeUtc(DateTime.UtcNow), 300, "/Evercoin/0.0.0/", 0, false, token).ConfigureAwait(false);
                            break;
                    }
                }
            );

            this.network.Start(token);
            await Task.WhenAll(endPoints.Select(endPoint => this.network.ConnectToPeerAsync(new ProtocolNetworkAddress(null, 1, endPoint.Address, (ushort)endPoint.Port), token))).ConfigureAwait(false);
            await Task.Run
            (
                async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(50, token).ConfigureAwait(false);
                        Console.Write("\rBlocks: ({0,8}) // Transactions: ({1,8}) [on block {3, 8}] // Valid Transactions: ({2, 8}) [on block {4, 8}]", blockIdentifiers.Count, Cheating.GetTransactionIdentifierCount(), validTransactions, this.dataBlock, this.validationBlock);
                    }
                },
                token
            );
        }

        private async Task HandleBlock(IBlock block, CancellationToken token)
        {
            Guid blockHashAlgorithmIdentifier = this.currencyParameters.ChainParameters.BlockHashAlgorithmIdentifier;
            IHashAlgorithm blockHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(blockHashAlgorithmIdentifier);

            FancyByteArray dataToHash = this.chainSerializer.GetBytesForBlock(block);
            FancyByteArray blockIdentifier = blockHashAlgorithm.CalculateHash(dataToHash.Value);

            if (this.chainStore.ContainsBlock(blockIdentifier))
            {
                return;
            }

            ulong? prevBlockHeight = null;
            var task = this.chainStore.PutBlockAsync(blockIdentifier, block, token).ConfigureAwait(false);
            SpinWait.SpinUntil(() => (prevBlockHeight = this.blockChain.GetHeightOfBlock(block.PreviousBlockIdentifier)).HasValue);
            this.blockChain.AddBlockAtHeight(blockIdentifier, prevBlockHeight.Value + 1);
            await task;
        }

        private async Task HandleTransaction(ITransaction transaction, BigInteger containingBlockIdentifier, ulong indexInBlock, CancellationToken token)
        {
            if (containingBlockIdentifier.IsZero)
            {
                // TODO: obviously, this will fail on transactions not yet in the blockchain.
                return;
            }

            if (!this.chainStore.ContainsBlock(containingBlockIdentifier))
            {
                return;
            }

            byte[] dataToHash = this.chainSerializer.GetBytesForTransaction(transaction);
            Guid txHashAlgorithmIdentifier = this.currencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier;
            IHashAlgorithm txHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(txHashAlgorithmIdentifier);

            FancyByteArray transactionIdentifier = txHashAlgorithm.CalculateHash(dataToHash);
            ulong? blockHeight = this.blockChain.GetHeightOfBlock(containingBlockIdentifier);
            if (blockHeight.HasValue)
            {
                lock (this.syncLock)
                {
                    this.dataBlock = Math.Max(this.dataBlock, blockHeight.Value);
                }
            }

            if (!this.chainStore.ContainsTransaction(transactionIdentifier))
            {
                var task = this.chainStore.PutTransactionAsync(transactionIdentifier, transaction, token).ConfigureAwait(false);
                Cheating.AddTransaction(transactionIdentifier);
                await task;
            }

            this.blockChain.AddTransactionToBlock(transactionIdentifier, containingBlockIdentifier, indexInBlock);
        }
    }
}
