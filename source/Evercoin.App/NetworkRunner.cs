using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            HashSet<ulong> heights = new HashSet<ulong>();
            List<FancyByteArray> blockIdentifiers = new List<FancyByteArray>();

            for (ulong highestKnownBlock = 0; highestKnownBlock < this.blockChain.BlockCount; highestKnownBlock++)
            {
                FancyByteArray blockIdentifier;
                if (!this.blockChain.TryGetIdentifierOfBlockAtHeight(highestKnownBlock, out blockIdentifier))
                {
                    throw new Exception("Block chain is discontinuous!");
                }

                blockIdentifiers.Add(blockIdentifier.Value);
                heights.Add(highestKnownBlock);
            }

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
            this.network.ReceivedBlocks.SubscribeOn(Scheduler.Immediate).ObserveOn(TaskPoolScheduler.Default).Subscribe(x => this.HandleBlock(x.Item2));
            this.network.ReceivedTransactions.SubscribeOn(Scheduler.Immediate).ObserveOn(TaskPoolScheduler.Default).Subscribe(x => this.HandleTransaction(x.Item2, x.Item3, x.Item4));

            this.network.ReceivedVersionAcknowledgements.Subscribe
            (
                async x =>
                {
                    bool started = false;
                    ulong bestHeight = x.HighestKnownBlock;
                    ulong currentBlockCount = this.blockChain.BlockCount;

                    SpinLock locker = new SpinLock();
                    Action updateBlockIdentifiers = () =>
                    {
                        bool _ = false;
                        locker.Enter(ref _);
                        try
                        {
                            ulong i = 0;
                            while (true)
                            {
                                if (heights.Contains(i))
                                {
                                    i++;
                                    continue;
                                }

                                FancyByteArray blockIdentifier;
                                if (this.blockChain.TryGetIdentifierOfBlockAtHeight(i, out blockIdentifier))
                                {
                                    blockIdentifiers.Add(blockIdentifier);
                                    heights.Add(i);
                                }
                                else
                                {
                                    break;
                                }

                                i++;
                            }
                        }
                        finally
                        {
                            locker.Exit();
                        }
                    };

                    ulong headersLastRequestedAtCount = currentBlockCount;

                    // Start by pulling all the headers
                    while (!token.IsCancellationRequested &&
                            bestHeight > this.blockChain.BlockCount)
                    {
                        if (started)
                        {
                            await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested ||
                                                                            this.blockChain.BlockCount >= bestHeight ||
                                                                            this.blockChain.BlockCount - headersLastRequestedAtCount == 2000),
                                token).ConfigureAwait(false);

                            updateBlockIdentifiers();
                        }

                        headersLastRequestedAtCount = this.blockChain.BlockCount;

                        started = true;
                        await this.network.RequestBlockOffersAsync(x, blockIdentifiers, BlockRequestType.HeadersOnly, token).ConfigureAwait(false);
                    }

                    updateBlockIdentifiers();

                    ulong lastFullBlockRequestedHeight = 0;

                    // Now, validate all the transactions and blocks.
                    IHashAlgorithm txHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.currencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier);
                    for (this.validationBlock = 1; this.validationBlock < this.blockChain.BlockCount;)
                    {
                        FancyByteArray blockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(this.validationBlock);
                        IBlock block = this.chainStore.GetBlock(blockIdentifier);

                        FancyByteArray expectedMerkleRoot = block.TransactionIdentifiers.Data;
                        FancyByteArray actualMerkleRoot = new FancyByteArray();
                        HashSet<FancyByteArray> checkedTransactions = new HashSet<FancyByteArray>();
                        SpinWait waiter = new SpinWait();
                        while (true)
                        {
                            bool merkleRootUpdated = false;
                            IEnumerable<FancyByteArray> transactionsForBlock;
                            bool foundTransactions = this.blockChain.TryGetTransactionsForBlock(blockIdentifier, out transactionsForBlock);
                            if (foundTransactions)
                            {
                                transactionsForBlock = transactionsForBlock.ToList();
                            }

                            bool knownMissing = !foundTransactions || checkedTransactions.IsSupersetOf(transactionsForBlock);
                            if (knownMissing)
                            {
                                if (this.validationBlock >= lastFullBlockRequestedHeight)
                                {
                                    const int PacketsToRequest = 10;
                                    int packetsRequested = 0;

                                    // Now, get all the transactions.
                                    lastFullBlockRequestedHeight = this.validationBlock;
                                    while (packetsRequested < PacketsToRequest)
                                    {
                                        await this.network.RequestBlockOffersAsync(x, blockIdentifiers.GetRange(0, (int)lastFullBlockRequestedHeight), BlockRequestType.IncludeTransactions, token).ConfigureAwait(false);

                                        lastFullBlockRequestedHeight += 500;
                                        if (lastFullBlockRequestedHeight >= this.blockChain.BlockCount)
                                        {
                                            await this.network.RequestBlockOffersAsync(x, blockIdentifiers.GetRange(0, (int)this.blockChain.BlockCount), BlockRequestType.IncludeTransactions, token).ConfigureAwait(false);
                                            break;
                                        }

                                        packetsRequested++;
                                    }
                                }

                                waiter.SpinOnce();
                                continue;
                            }

                            transactionsForBlock = transactionsForBlock.ToList();
                            Parallel.ForEach(transactionsForBlock.Where(checkedTransactions.Add), transactionIdentifier =>
                            {
                                ITransaction transaction = this.chainStore.GetTransaction(transactionIdentifier);
                                ValidationResult transactionValidationResult = this.currencyParameters.ChainValidator.ValidateTransaction(transaction);
                                if (!transactionValidationResult)
                                {
                                    string exceptionMessage = "Transaction " + transactionIdentifier + " in block " + this.validationBlock + " is invalid: " + transactionValidationResult.Reason;
                                    Console.WriteLine();
                                    Console.WriteLine(exceptionMessage);
                                    throw new InvalidOperationException(exceptionMessage);
                                }

                                merkleRootUpdated = true;
                            });

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

                        validTransactions += checkedTransactions.Count;
                        this.validationBlock++;
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
        ////await Task.Run
        ////(
        ////    async () =>
        ////    {
                    ulong transactionCount = 0;
                    ulong highestBlockWithTransactions = 1;
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(50, token).ConfigureAwait(false);
                        while (true)
                        {
                            FancyByteArray nextBlock;
                            if (!this.blockChain.TryGetIdentifierOfBlockAtHeight(highestBlockWithTransactions + 1, out nextBlock))
                            {
                                break;
                            }

                            IEnumerable<FancyByteArray> transactionsForNextBlock;
                            if (!this.blockChain.TryGetTransactionsForBlock(nextBlock, out transactionsForNextBlock))
                            {
                                break;
                            }

                            transactionCount += (ulong)transactionsForNextBlock.Count();
                            highestBlockWithTransactions++;
                        }

                        Console.Write("\rBlocks: ({0,8}) // Found Transactions: ({1,8}) [on block {4, 8}] // Validated Transactions: ({2, 8}) [on block {3, 8}]", this.blockChain.BlockCount, transactionCount, validTransactions, this.validationBlock, highestBlockWithTransactions);
                    }
        ////    },
        ////    token
        ////);
        }

        private void HandleBlock(IBlock block)
        {
            Guid blockHashAlgorithmIdentifier = this.currencyParameters.ChainParameters.BlockHashAlgorithmIdentifier;
            IHashAlgorithm blockHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(blockHashAlgorithmIdentifier);

            FancyByteArray dataToHash = this.chainSerializer.GetBytesForBlock(block);
            FancyByteArray blockIdentifier = blockHashAlgorithm.CalculateHash(dataToHash.Value);

            if (!this.chainStore.ContainsBlock(blockIdentifier))
            {
                this.chainStore.PutBlock(blockIdentifier, block);
            }

            ulong blockHeight;
            if (!this.blockChain.TryGetHeightOfBlock(blockIdentifier, out blockHeight))
            {
                ulong prevBlockHeight = this.blockChain.GetHeightOfBlock(block.PreviousBlockIdentifier);
                this.blockChain.AddBlockAtHeight(blockIdentifier, prevBlockHeight + 1);
            }
        }

        private void HandleTransaction(ITransaction transaction, FancyByteArray containingBlockIdentifier, ulong indexInBlock)
        {
            byte[] dataToHash = this.chainSerializer.GetBytesForTransaction(transaction);
            Guid txHashAlgorithmIdentifier = this.currencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier;
            IHashAlgorithm txHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(txHashAlgorithmIdentifier);

            FancyByteArray transactionIdentifier = txHashAlgorithm.CalculateHash(dataToHash);

            if (!this.chainStore.ContainsTransaction(transactionIdentifier))
            {
                this.chainStore.PutTransaction(transactionIdentifier, transaction);
            }

            if (containingBlockIdentifier)
            {
                this.blockChain.AddTransactionToBlock(transactionIdentifier, containingBlockIdentifier, indexInBlock);
            }
        }
    }
}
