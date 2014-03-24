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
        private readonly ICurrencyNetwork network;

        private readonly IChainStore chainStore;

        private readonly ISignatureCheckerFactory signatureCheckerFactory;

        private readonly ITransactionScriptParser scriptParser;

        private readonly ITransactionScriptRunner scriptRunner;

        public NetworkRunner(ICurrencyNetwork network, IChainStore chainStore, ISignatureCheckerFactory signatureCheckerFactory, ITransactionScriptParser scriptParser, ITransactionScriptRunner scriptRunner)
        {
            this.network = network;
            this.chainStore = chainStore;
            this.signatureCheckerFactory = signatureCheckerFactory;
            this.scriptParser = scriptParser;
            this.scriptRunner = scriptRunner;
        }

        public async Task Run(CancellationToken token)
        {
            token.Register(Cheating.DisposeThings);
            ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector> knownInventory = new ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector>(Cheating.GetBlockIdentifiers().ToDictionary(x => new ProtocolInventoryVector(ProtocolInventoryVector.InventoryType.Block, x), x => new ProtocolInventoryVector(ProtocolInventoryVector.InventoryType.Block, x)));
            List<IPEndPoint> endPoints = new List<IPEndPoint>
                                         {
                                             new IPEndPoint(IPAddress.Loopback, 8333),
                                         };

            this.network.ReceivedVersionPackets.Subscribe(async x => await this.network.AcknowledgePeerVersionAsync(x.Item1, token));
            this.network.ReceivedInventoryOffers.Subscribe(async x => await this.network.RequestInventoryAsync(x.Item2.Where(y => knownInventory.TryAdd(y, y)), token));
            this.network.ReceivedBlocks.ObserveOn(TaskPoolScheduler.Default).Subscribe(async x => await this.HandleBlock(x.Item2, token));
            this.network.ReceivedTransactions.ObserveOn(TaskPoolScheduler.Default).Subscribe(async x => await this.HandleTransaction(x.Item2, token));

            this.network.ReceivedVersionAcknowledgements.Subscribe
            (
                async x =>
                {
                    int startingBlockHeight = Cheating.GetHighestBlock();
                    int currentBlockHeight = startingBlockHeight;
                    bool started = false;
                    const int CurrentHighestBlockBecauseIAmCheating = 291890;

                    // Start by pulling all the headers
                    while (!token.IsCancellationRequested &&
                           currentBlockHeight < CurrentHighestBlockBecauseIAmCheating)
                    {
                        if (started)
                        {
                            await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested ||
                                                                          currentBlockHeight >= CurrentHighestBlockBecauseIAmCheating ||
                                                                          (startingBlockHeight - (currentBlockHeight = Cheating.GetHighestBlock())) % 2000 == 0),
                                token);
                        }

                        started = true;
                        await this.network.RequestBlockOffersAsync(x, Cheating.GetBlockIdentifiers(), BlockRequestType.HeadersOnly, token);

                        if (currentBlockHeight >= CurrentHighestBlockBecauseIAmCheating)
                        {
                            break;
                        }

                        await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested || currentBlockHeight >= CurrentHighestBlockBecauseIAmCheating || currentBlockHeight != (currentBlockHeight = Cheating.GetHighestBlock())), token);
                    }

                    // Now, get all the transactions.
                    int i = 1;
                    while (true)
                    {
                        await this.network.RequestBlockOffersAsync(x, Cheating.GetBlockIdentifiers().GetRange(0, i), BlockRequestType.IncludeTransactions, token);
                        if (i == Cheating.GetHighestBlock())
                        {
                            break;
                        }

                        i += 500;
                        i = Math.Min(i, Cheating.GetHighestBlock());
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
                            await this.network.AnnounceVersionToPeerAsync(x, this.network.CurrencyParameters.NetworkParameters.ProtocolVersion, 1, Instant.FromDateTimeUtc(DateTime.UtcNow), 300, "/Evercoin/0.0.0/", 0, false, token);
                            break;
                    }
                }
            );

            try
            {
                this.network.Start(token);
                await Task.WhenAll(endPoints.Select(endPoint => this.network.ConnectToPeerAsync(new ProtocolNetworkAddress(null, 1, endPoint.Address, (ushort)endPoint.Port), token)));
                await Task.Run
                (
                    async () =>
                    {
                        int prevBlockIdCount = -1;
                        int prevTransactionIdCount = -1;
                        while (!token.IsCancellationRequested)
                        {
                            Console.Write("\rBlocks: ({0,8}) // Transactions: ({1,8})", Cheating.GetHighestBlock(), Cheating.GetTransactionIdentifierCount());
                            await Task.Run(() => SpinWait.SpinUntil(() => token.IsCancellationRequested || prevTransactionIdCount != (prevTransactionIdCount = Cheating.GetTransactionIdentifierCount()) || prevBlockIdCount != (prevBlockIdCount = Cheating.GetBlockIdentifierCount())), token);
                        }
                    },
                    token
                );
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task HandleBlock(ProtocolBlock block, CancellationToken token)
        {
            Task<int> prevBlockHeightGetter = Cheating.GetBlockHeightAsync(block.PrevBlockId, token);
            byte[] dataToHash = block.HeaderData;
            IHashAlgorithm blockHashAlgorithm = this.network.CurrencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.network.CurrencyParameters.ChainParameters.BlockHashAlgorithmIdentifier);
            byte[] blockHash = blockHashAlgorithm.CalculateHash(dataToHash);
            BigInteger blockIdentifier = new BigInteger(blockHash);

            if (this.chainStore.ContainsBlock(blockIdentifier))
            {
                return;
            }

            IHashAlgorithm txHashAlgorithm = this.network.CurrencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.network.CurrencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier);
            IBlock typedBlock = block.ToBlock(txHashAlgorithm);
            if (blockIdentifier >= typedBlock.DifficultyTarget)
            {
                return;
            }

            await this.chainStore.PutBlockAsync(blockIdentifier, typedBlock, token);
            int prevBlockHeight = await prevBlockHeightGetter;

            Cheating.AddBlock(prevBlockHeight + 1, blockIdentifier);
        }

        private async Task HandleTransaction(ProtocolTransaction transaction, CancellationToken token)
        {
            if (transaction.ContainingBlockIdentifier.IsZero)
            {
                // TODO: obviously, this will fail on transactions not yet in the blockchain.
                return;
            }

            // I don't think this will actually happen with the above TODO check.
            ////if (!this.chainStore.ContainsBlock(transaction.ContainingBlockIdentifier) ||
            ////    !Cheating.GetBlockIdentifiers().Contains(transaction.ContainingBlockIdentifier))
            ////{
            ////    return;
            ////}

            Dictionary<BigInteger, ITransaction> allValidInputTransactions = new Dictionary<BigInteger, ITransaction>();
            foreach (BigInteger neededTxId in transaction.Inputs.Select(x => x.PrevOutTxId).Where(x => !x.IsZero).Distinct())
            {
                Task<ITransaction> prevTransaction = this.chainStore.GetTransactionAsync(neededTxId, token);
                allValidInputTransactions[neededTxId] = await prevTransaction;
            }

            IHashAlgorithm txHashAlgorithm = this.network.CurrencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.network.CurrencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier);
            transaction.CalculateTxId(txHashAlgorithm);
            BigInteger transactionIdentifier = transaction.TxId;

            if (this.chainStore.ContainsTransaction(transactionIdentifier))
            {
                return;
            }

            ITransaction tx = transaction.ToTransaction(allValidInputTransactions);

            int valid = 1;
            ParallelOptions options = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.For(0, tx.Inputs.Length, options, i =>
            {
                var input = tx.Inputs[i];

                if (input.SpendingValueSource.IsCoinbase)
                {
                    return;
                }

                byte[] scriptSig = input.ScriptSignature;
                IEnumerable<TransactionScriptOperation> parsedScriptSig = this.scriptParser.Parse(scriptSig);
                ISignatureChecker signatureChecker = this.signatureCheckerFactory.CreateSignatureChecker(tx, i);
                var result = this.scriptRunner.EvaluateScript(parsedScriptSig, signatureChecker);
                if (!result)
                {
                    Interlocked.CompareExchange(ref valid, 0, 1);
                    return;
                }

                byte[] scriptPubKey = allValidInputTransactions[input.SpendingValueSource.OriginatingTransactionIdentifier].Outputs[(int)input.SpendingValueSource.OriginatingTransactionOutputIndex].ScriptPublicKey;
                IEnumerable<TransactionScriptOperation> parsedScriptPubKey = this.scriptParser.Parse(scriptPubKey);

                if (!this.scriptRunner.EvaluateScript(parsedScriptPubKey, signatureChecker, result.MainStack, result.AlternateStack))
                {
                    Interlocked.CompareExchange(ref valid, 0, 1);
                }
            });

            if (valid == 0)
            {
                Console.WriteLine("Invalid transaction in the blockchain!  Alert!  Alert!");
                return;
            }

            await this.chainStore.PutTransactionAsync(transactionIdentifier, tx, token);
            Cheating.AddTransaction(transactionIdentifier);
        }
    }
}
;