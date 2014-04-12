using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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

        public async Task Run(int port, CancellationToken token)
        {
            List<FancyByteArray> blockIdentifiers = new List<FancyByteArray>();
            ulong highestKnownBlock = 0;
            while (true)
            {
                FancyByteArray? blockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(highestKnownBlock);
                if (!blockIdentifier.HasValue)
                {
                    break;
                }

                blockIdentifiers.Add(blockIdentifier.Value);
                highestKnownBlock++;
            }

            HashSet<ulong> heights = new HashSet<ulong>();
            long validTransactions = 0;

            ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector> knownInventory = new ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector>();
            List<IPEndPoint> endPoints = new List<IPEndPoint>
                                         {
                                             new IPEndPoint(IPAddress.Loopback, port),
                                         };

            this.network.ReceivedVersionPackets.Subscribe
            (
                async x =>
                {
                    x.Item1.HighestKnownBlock = (ulong)x.Item2.StartHeight;
                    await this.network.AcknowledgePeerVersionAsync(x.Item1, token).ConfigureAwait(false);
                }
            );
            this.network.ReceivedInventoryOffers.Subscribe(async x => await this.network.RequestInventoryAsync(x.Item2.Where(y => knownInventory.TryAdd(y, y)).GetArray(), token).ConfigureAwait(false));
            this.network.ReceivedBlocks.SubscribeOn(Scheduler.Immediate).ObserveOn(TaskPoolScheduler.Default).Subscribe(async x => await this.HandleBlock(x.Item2, token).ConfigureAwait(false));
            this.network.ReceivedTransactions.SubscribeOn(Scheduler.Immediate).ObserveOn(TaskPoolScheduler.Default).Subscribe(async x => await this.HandleTransaction(x.Item2, x.Item3, x.Item4, token).ConfigureAwait(false));

            this.network.ReceivedVersionAcknowledgements.Subscribe
            (
                async x =>
                {
                    ulong startingBlockHeight = highestKnownBlock;
                    ulong currentBlockHeight = startingBlockHeight;
                    bool started = false;
                    ulong bestHeight = x.HighestKnownBlock;

                    this.blockChain.RemoveBlocksAboveHeight(bestHeight);

                    Action updateBlockIdentifiers = () =>
                    {
                        lock (blockIdentifiers)
                        {
                            for (ulong i = 0; i < currentBlockHeight; i++)
                            {
                                if (heights.Add(i))
                                {
                                    blockIdentifiers.Add(this.blockChain.GetIdentifierOfBlockAtHeight(i).Value);
                                }
                            }
                        }
                    };

                    ulong lastHeadersRequestedHeight = currentBlockHeight;

                    // Start by pulling all the headers
                    while (!token.IsCancellationRequested &&
                            currentBlockHeight < bestHeight)
                    {
                        if (started)
                        {
                            await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested ||
                                                                            currentBlockHeight >= bestHeight ||
                                                                            (currentBlockHeight = this.blockChain.BlockCount) - lastHeadersRequestedHeight == 2000),
                                token).ConfigureAwait(false);

                            updateBlockIdentifiers();
                        }

                        lastHeadersRequestedHeight = currentBlockHeight;

                        started = true;
                        await this.network.RequestBlockOffersAsync(x, blockIdentifiers, BlockRequestType.HeadersOnly, token).ConfigureAwait(false);

                        if (currentBlockHeight >= bestHeight)
                        {
                            updateBlockIdentifiers();
                            break;
                        }

                        await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested || currentBlockHeight >= bestHeight || (currentBlockHeight = this.blockChain.BlockCount) != lastHeadersRequestedHeight), token).ConfigureAwait(false);
                    }

                    updateBlockIdentifiers();

                    ulong lastFullBlockRequestedHeight = 0;

                    // Now, validate all the transactions and blocks.
                    IHashAlgorithm txHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.currencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier);
                    SpinWait waiter = new SpinWait();
                    for (this.validationBlock = 1; this.validationBlock < currentBlockHeight; this.validationBlock++)
                    {
                        FancyByteArray? possibleBlockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(this.validationBlock);
                        if (!possibleBlockIdentifier.HasValue)
                        {
                            waiter.SpinOnce();
                            continue;
                        }

                        waiter.Reset();

                        FancyByteArray blockIdentifier = possibleBlockIdentifier.Value;

                        IBlock block = await this.chainStore.GetBlockAsync(blockIdentifier, token).ConfigureAwait(false);

                        FancyByteArray expectedMerkleRoot = block.TransactionIdentifiers.Data;
                        FancyByteArray actualMerkleRoot = new FancyByteArray();
                        HashSet<BigInteger> foundTransactions = new HashSet<BigInteger>();
                        while (true)
                        {
                            bool merkleRootUpdated = false;
                            List<FancyByteArray> transactionsForBlock = this.blockChain.GetTransactionsForBlock(blockIdentifier).ToList();
                            foreach (FancyByteArray transactionIdentifier in transactionsForBlock.Where(transactionIdentifier => foundTransactions.Add(transactionIdentifier) && !transactionIdentifier.NumericValue.IsZero))
                            {
                                ITransaction transaction = await this.chainStore.GetTransactionAsync(transactionIdentifier, token).ConfigureAwait(false);
                                ValidationResult transactionValidationResult = this.currencyParameters.ChainValidator.ValidateTransaction(transaction);
                                if (!transactionValidationResult)
                                {
                                    string exceptionMessage = "Transaction " + transactionIdentifier + " in block " + this.validationBlock + " is invalid: " + transactionValidationResult.Reason;
                                    Console.WriteLine();
                                    Console.WriteLine(exceptionMessage);
                                    throw new InvalidOperationException(exceptionMessage);
                                }

                                Interlocked.Increment(ref validTransactions);
                                merkleRootUpdated = true;
                            }

                            if (merkleRootUpdated)
                            {
                                actualMerkleRoot = transactionsForBlock.Select(transactionIdentifier => transactionIdentifier.Value)
                                                                       .ToMerkleTree(txHashAlgorithm)
                                                                       .Data;
                            }

                            if (expectedMerkleRoot == actualMerkleRoot)
                            {
                                break;
                            }

                            if (this.validationBlock > lastFullBlockRequestedHeight)
                            {
                                // Now, get all the transactions.
                                lastFullBlockRequestedHeight = this.validationBlock;
                                while (true)
                                {
                                    await this.network.RequestBlockOffersAsync(x, ((IReadOnlyList<FancyByteArray>)blockIdentifiers).GetRange(0, (int)lastFullBlockRequestedHeight), BlockRequestType.IncludeTransactions, token);
                                    if (lastFullBlockRequestedHeight >= currentBlockHeight)
                                    {
                                        break;
                                    }

                                    lastFullBlockRequestedHeight += 500;
                                    lastFullBlockRequestedHeight = Math.Min(lastFullBlockRequestedHeight, currentBlockHeight);
                                }
                            }

                            waiter.SpinOnce();
                        }

                        ValidationResult blockValidationResult = this.currencyParameters.ChainValidator.ValidateBlock(block);
                        if (!blockValidationResult)
                        {
                            string exceptionMessage = "Block " + this.validationBlock + " is invalid: " + blockValidationResult.Reason;
                            Console.WriteLine();
                            Console.WriteLine(exceptionMessage);
                            throw new InvalidOperationException(exceptionMessage);
                        }
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
                    ulong transactionCount = 0;
                    ulong highestBlockWithTransactions = 1;
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, token).ConfigureAwait(false);
                        while (true)
                        {
                            FancyByteArray? nextBlock = this.blockChain.GetIdentifierOfBlockAtHeight(highestBlockWithTransactions + 1);
                            if (!nextBlock.HasValue)
                            {
                                break;
                            }

                            if (!this.blockChain.GetTransactionsForBlock(nextBlock.Value).Any())
                            {
                                break;
                            }

                            FancyByteArray currentBlock = this.blockChain.GetIdentifierOfBlockAtHeight(highestBlockWithTransactions).Value;
                            transactionCount += (ulong)this.blockChain.GetTransactionsForBlock(currentBlock).Count();
                            highestBlockWithTransactions++;
                        }

                        Console.Write("\rBlocks: ({0,8}) // Found Transactions: ({1,8}) [on block {4, 8}] // Validated Transactions: ({2, 8}) [on block {3, 8}]", this.blockChain.BlockCount, transactionCount, validTransactions, this.validationBlock, highestBlockWithTransactions);
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

        private async Task HandleTransaction(ITransaction transaction, FancyByteArray containingBlockIdentifier, ulong indexInBlock, CancellationToken token)
        {
            byte[] dataToHash = this.chainSerializer.GetBytesForTransaction(transaction);
            Guid txHashAlgorithmIdentifier = this.currencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier;
            IHashAlgorithm txHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(txHashAlgorithmIdentifier);

            FancyByteArray transactionIdentifier = txHashAlgorithm.CalculateHash(dataToHash);

            await this.chainStore.PutTransactionAsync(transactionIdentifier, transaction, token).ConfigureAwait(false);

            if (!containingBlockIdentifier.NumericValue.IsZero)
            {
                this.blockChain.AddTransactionToBlock(transactionIdentifier, containingBlockIdentifier, indexInBlock);
            }
        }
    }
}
