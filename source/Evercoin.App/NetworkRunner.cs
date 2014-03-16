using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
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

        private readonly ITransactionScriptRunner scriptRunner;

        public NetworkRunner(ICurrencyNetwork network, IChainStore chainStore, ISignatureCheckerFactory signatureCheckerFactory, ITransactionScriptRunner scriptRunner)
        {
            this.network = network;
            this.chainStore = chainStore;
            this.signatureCheckerFactory = signatureCheckerFactory;
            this.scriptRunner = scriptRunner;
        }

        public async Task Run(CancellationToken token)
        {
            ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector> knownInventory = new ConcurrentDictionary<ProtocolInventoryVector, ProtocolInventoryVector>(Cheating.GetBlockIdentifiers().ToDictionary(x => new ProtocolInventoryVector(ProtocolInventoryVector.InventoryType.Block, x), x => new ProtocolInventoryVector(ProtocolInventoryVector.InventoryType.Block, x)));
            List<IPEndPoint> endPoints = new List<IPEndPoint>
                                         {
                                             new IPEndPoint(IPAddress.Loopback, 8333),
                                         };

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

            this.network.ReceivedVersionPackets.Subscribe
            (
                async x =>
                {
                    INetworkPeer peer = x.Item1;
                    ////ProtocolVersionPacket versionPacket = x.Item2;

                    await this.network.AcknowledgePeerVersionAsync(peer, token);
                }
            );

            this.network.ReceivedVersionAcknowledgements.Subscribe
            (
                async x =>
                {
                    SpinWait spinner = new SpinWait();
                    int startingCount = Cheating.GetBlockIdentifierCount();
                    int prev = -1;
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            var blidCount = Cheating.GetBlockIdentifierCount();
                            if ((blidCount - startingCount) % 500 == 0 &&
                                blidCount != prev)
                            {
                                prev = blidCount;
                                await this.network.RequestBlockOffersAsync(x, Cheating.GetBlockIdentifiers(), token);
                                spinner.Reset();
                            }

                            spinner.SpinOnce();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            );

            this.network.ReceivedInventoryOffers.Subscribe(async x => await this.network.RequestInventoryAsync(x.Item2.Where(y => knownInventory.TryAdd(y, y)), token));

            this.network.ReceivedBlocks.Subscribe(async x =>
            {
                bool isHandled = await this.HandleBlock(x.Item2, token);
                if (isHandled)
                {
                    Console.Write("\r({0})", Cheating.GetBlockIdentifierCount());
                }
            });

            try
            {
                this.network.Start(token);
                await Task.WhenAll(endPoints.Select(endPoint => this.network.ConnectToPeerAsync(new ProtocolNetworkAddress(null, 1, endPoint.Address, (ushort)endPoint.Port), token)));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task<bool> HandleBlock(ProtocolBlock block, CancellationToken token)
        {
            // This is important to do now -- for some reason, the Satoshi client gives us recent blocks even when we're building from the genesis block!
            // So, one thing we REALLY don't want to do is to fetch all the old transactions!
            ProtocolTxIn cb = block.IncludedTransactions[0].Inputs[0];
            if (block.Version > 1 && cb.ScriptSig[0] == 3)
            {
                byte[] serializedHeight = cb.ScriptSig.Skip(1).Take(3).GetArray();
                Array.Resize(ref serializedHeight, 4);

                uint probableHeight = BitConverter.ToUInt32(serializedHeight.LittleEndianToOrFromBitConverterEndianness(), 0);
                if (probableHeight - Cheating.GetBlockIdentifierCount() > 1000)
                {
                    // So we've got a block message from a recent block.
                    // Don't waste any more time on it than we have.
                    return false;
                }
            }

            Task<int> prevBlockHeightGetter = Cheating.GetBlockHeightAsync(block.PrevBlockId, token);
            byte[] dataToHash = block.HeaderData;

            IHashAlgorithm blockHashAlgorithm = this.network.CurrencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.network.CurrencyParameters.ChainParameters.BlockAlgorithmIdentifier);
            IHashAlgorithm txHashAlgorithm = this.network.CurrencyParameters.HashAlgorithmStore.GetHashAlgorithm(this.network.CurrencyParameters.ChainParameters.TransactionHashAlgorithmIdentifier);
            byte[] blockHash = blockHashAlgorithm.CalculateHash(dataToHash);
            BigInteger blockIdentifier = new BigInteger(blockHash);

            HashSet<BigInteger> includedTxIds = new HashSet<BigInteger>();
            foreach (ProtocolTransaction includedTransaction in block.IncludedTransactions)
            {
                includedTransaction.CalculateTxId(txHashAlgorithm);
                includedTxIds.Add(includedTransaction.TxId);
            }

            List<BigInteger> neededTxIds = block.IncludedTransactions.SelectMany(x => x.Inputs).Select(x => x.PrevOutTxId).Where(x => !x.IsZero && !includedTxIds.Contains(x)).Distinct().ToList();
            Dictionary<BigInteger, Task<ITransaction>> neededTransactionFetchers = neededTxIds.ToDictionary(x => x, x => this.chainStore.GetTransactionAsync(x, token));

            IBlock typedBlock = block.ToBlock(blockIdentifier, blockHashAlgorithm);
            if (blockIdentifier >= typedBlock.DifficultyTarget)
            {
                return false;
            }

            if (!typedBlock.TransactionIdentifiers.Data.SequenceEqual(block.MerkleRoot.ToLittleEndianUInt256Array()))
            {
                return false;
            }

            Dictionary<BigInteger, ITransaction> allValidInputTransactions = neededTransactionFetchers.ToDictionary(x => x.Key, x => x.Value.Result);
            foreach (ProtocolTransaction protoTransaction in block.IncludedTransactions)
            {
                ITransaction tx = protoTransaction.ToTransaction(allValidInputTransactions, typedBlock);
                allValidInputTransactions[protoTransaction.TxId] = tx;
            }

            ParallelOptions options = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Environment.ProcessorCount };
            ParallelLoopResult validationResult = Parallel.ForEach(block.IncludedTransactions, options, (protoTransaction, loopStateOuter) =>
            {
                ITransaction tx = allValidInputTransactions[protoTransaction.TxId];

                Parallel.For(0, tx.Inputs.Length, options, (i, loopStateInner) =>
                {
                    if (loopStateOuter.IsStopped)
                    {
                        return;
                    }

                    var input = tx.Inputs[i];

                    ITransactionValueSource inputValueSource = input.SpendingValueSource as ITransactionValueSource;
                    if (inputValueSource == null)
                    {
                        // probably coinbase -- no script to validate.
                        return;
                    }

                    byte[] scriptSig = input.ScriptSignature;
                    ISignatureChecker signatureChecker = this.signatureCheckerFactory.CreateSignatureChecker(tx, i);
                    var result = this.scriptRunner.EvaluateScript(scriptSig, signatureChecker);
                    if (!result)
                    {
                        loopStateOuter.Stop();
                        loopStateInner.Stop();
                        return;
                    }

                    if (loopStateOuter.IsStopped)
                    {
                        return;
                    }

                    byte[] scriptPubKey = allValidInputTransactions[inputValueSource.OriginatingTransactionIdentifier].Outputs[(int)inputValueSource.OriginatingTransactionOutputIndex].ScriptPublicKey;

                    if (!this.scriptRunner.EvaluateScript(scriptPubKey, signatureChecker, result.MainStack, result.AlternateStack))
                    {
                        loopStateOuter.Stop();
                        loopStateInner.Stop();
                    }
                });

                if (loopStateOuter.IsStopped)
                {
                    return;
                }

                this.chainStore.PutTransactionAsync(protoTransaction.TxId, tx, token);
            });

            if (!validationResult.IsCompleted)
            {
                return false;
            }

            int prevBlockHeight = await prevBlockHeightGetter;
            Cheating.Add(prevBlockHeight + 1, blockIdentifier);

            await this.chainStore.PutBlockAsync(blockIdentifier, typedBlock, token);

            return true;
        }
    }
}
