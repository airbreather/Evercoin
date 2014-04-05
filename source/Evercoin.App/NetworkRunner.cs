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
                        if (i >= currentBlockHeight)
                        {
                            await t.ConfigureAwait(false);
                            break;
                        }

                        i += 500;
                        i = Math.Min(i, currentBlockHeight);
                        await t.ConfigureAwait(false);
                    }

                    // Now, validate all the transactions and blocks.
                    this.validationBlock = 1;
                    IHashAlgorithm txHashAlgorithm = this.currencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.currencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier);
                    while (this.validationBlock < currentBlockHeight)
                    {
                        BigInteger blockIdentifier = this.blockChain.GetIdentifierOfBlockAtHeight(this.validationBlock).Value;
                        IBlock block = await this.chainStore.GetBlockAsync(blockIdentifier, token).ConfigureAwait(false);

                        ValidationResult blockValidationResult = this.currencyParameters.ChainValidator.ValidateBlock(block);
                        if (!blockValidationResult)
                        {
                            string exceptionMessage = "Block " + this.validationBlock + " is invalid: " + blockValidationResult.Reason;
                            Console.WriteLine();
                            Console.WriteLine(exceptionMessage);
                            throw new InvalidOperationException(exceptionMessage);
                        }

                        FancyByteArray expectedMerkleRoot = block.TransactionIdentifiers.Data;
                        FancyByteArray actualMerkleRoot = new FancyByteArray();
                        HashSet<BigInteger> foundTransactions = new HashSet<BigInteger>();
                        SpinWait waiter = new SpinWait();
                        while (true)
                        {
                            bool merkleRootUpdated = false;
                            List<BigInteger> transactionsForBlock = this.blockChain.GetTransactionsForBlock(blockIdentifier).ToList();
                            foreach (BigInteger transactionIdentifier in transactionsForBlock.Where(transactionIdentifier => foundTransactions.Add(transactionIdentifier) && !transactionIdentifier.IsZero))
                            {
                                ITransaction transaction = await this.chainStore.GetTransactionAsync(transactionIdentifier, token).ConfigureAwait(false);
                                ValidationResult transactionValidationResult = this.currencyParameters.ChainValidator.ValidateTransaction(transaction);
                                if (!transactionValidationResult)
                                {
                                    string exceptionMessage = "Transaction " + FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(transactionIdentifier, 32, Endianness.LittleEndian) + " in block " + this.validationBlock + " is invalid: " + transactionValidationResult.Reason;
                                    Console.WriteLine();
                                    Console.WriteLine(exceptionMessage);
                                    throw new InvalidOperationException(exceptionMessage);
                                }

                                Interlocked.Increment(ref validTransactions);
                                merkleRootUpdated = true;
                            }

                            if (merkleRootUpdated)
                            {
                                actualMerkleRoot = transactionsForBlock.Select(transactionIdentifier => FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(transactionIdentifier, 32, Endianness.LittleEndian).Value)
                                                                       .ToMerkleTree(txHashAlgorithm)
                                                                       .Data;
                            }

                            if (expectedMerkleRoot == actualMerkleRoot)
                            {
                                break;
                            }

                            waiter.SpinOnce();
                        }

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
