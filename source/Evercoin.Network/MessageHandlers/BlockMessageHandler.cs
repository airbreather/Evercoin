using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class BlockMessageHandler : MessageHandlerBase
    {
        private readonly IHashAlgorithmStore hashAlgorithmStore;
        private readonly ISignatureCheckerFactory signatureCheckerFactory;

        private readonly ITransactionScriptRunner scriptRunner;
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("block");

        [ImportingConstructor]
        public BlockMessageHandler(INetwork network, IChainStore chainStore, IHashAlgorithmStore hashAlgorithmStore, ITransactionScriptRunner scriptRunner, ISignatureCheckerFactory signatureCheckerFactory)
            : base(RecognizedCommand, network, chainStore)
        {
            this.hashAlgorithmStore = hashAlgorithmStore;
            this.signatureCheckerFactory = signatureCheckerFactory;
            this.scriptRunner = scriptRunner;
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            using (MemoryStream payloadStream = new MemoryStream(message.Payload.ToArray()))
            using (ProtocolStreamReader streamReader = new ProtocolStreamReader(payloadStream, leaveOpen: true))
            {
                uint version = await streamReader.ReadUInt32Async(token);
                BigInteger prevBlockId = await streamReader.ReadUInt256Async(token);
                Task<IBlock> prevBlockGetter = this.ReadOnlyChainStore.GetBlockAsync(prevBlockId, token);
                BigInteger merkleRoot = await streamReader.ReadUInt256Async(token);
                uint timestamp = await streamReader.ReadUInt32Async(token);
                uint bits = await streamReader.ReadUInt32Async(token);
                uint nonce = await streamReader.ReadUInt32Async(token);
                ulong transactionCount = await streamReader.ReadCompactSizeAsync(token);
                ImmutableList<ProtocolTransaction> includedTransactions = ImmutableList<ProtocolTransaction>.Empty;
                HashSet<BigInteger> neededTransactions = new HashSet<BigInteger>();

                while (transactionCount-- > 0)
                {
                    ProtocolTransaction nextTransaction = await streamReader.ReadTransactionAsync(token);
                    includedTransactions = includedTransactions.Add(nextTransaction);
                    foreach (BigInteger prevTxId in nextTransaction.Inputs.Select(x => x.PrevOutTxId).Where(x => !x.IsZero))
                    {
                        neededTransactions.Add(prevTxId);
                    }
                }

                // This is important to do now -- for some reason, the Satoshi client gives us recent blocks even when we're building from the genesis block!
                // So, one thing we REALLY don't want to do is to fetch all the old transactions!
                ProtocolTxIn cb = includedTransactions[0].Inputs[0];
                if (version > 1 && cb.ScriptSig[0] == 3)
                {
                    byte[] serializedHeight = cb.ScriptSig.Skip(1).Take(3).ToArray();
                    Array.Resize(ref serializedHeight, 4);

                    uint probableHeight = BitConverter.ToUInt32(serializedHeight.LittleEndianToOrFromBitConverterEndianness(), 0);
                    if (probableHeight - Cheating.GetBlockIdentifiers().Count > 1000)
                    {
                        // So we've got a block message from a recent block.
                        // Don't waste any more time on it than we have.
                        return HandledNetworkMessageResult.ContextuallyInvalid;
                    }
                }

                Dictionary<BigInteger, Task<ITransaction>> neededTransactionFetchers = neededTransactions.ToDictionary(x => x, x => this.ReadOnlyChainStore.GetTransactionAsync(x, token));

                ImmutableList<byte> dataToHash = ImmutableList.CreateRange(BitConverter.GetBytes(version).LittleEndianToOrFromBitConverterEndianness())
                                                              .AddRange(prevBlockId.ToLittleEndianUInt256Array())
                                                              .AddRange(merkleRoot.ToLittleEndianUInt256Array())
                                                              .AddRange(BitConverter.GetBytes(timestamp).LittleEndianToOrFromBitConverterEndianness())
                                                              .AddRange(BitConverter.GetBytes(bits).LittleEndianToOrFromBitConverterEndianness())
                                                              .AddRange(BitConverter.GetBytes(nonce).LittleEndianToOrFromBitConverterEndianness());

                // TODO: This should definitely be done somewhere else, because the POW algorithm is chain-specific,
                // TODO: which is part of why I'm so hesitant to have the ID on IBlock.
                IHashAlgorithm hashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(HashAlgorithmIdentifiers.DoubleSHA256);
                ImmutableList<byte> blockHash = hashAlgorithm.CalculateHash(dataToHash);
                BigInteger blockIdentifier = new BigInteger(blockHash.ToArray());
                BigInteger target = TargetFromBits(bits);
                if (blockIdentifier >= target)
                {
                    return HandledNetworkMessageResult.ContextuallyInvalid;
                }

                foreach (ProtocolTransaction includedTransaction in includedTransactions)
                {
                    includedTransaction.CalculateTxId(hashAlgorithm);
                }

                Dictionary<BigInteger, ProtocolTransaction> ownTransactions = includedTransactions.ToDictionary(x => x.TxId);
                Dictionary<BigInteger, ITransaction> foundTransactions = new Dictionary<BigInteger, ITransaction>(neededTransactionFetchers.Count);

                NetworkBlock newBlock = new NetworkBlock
                                        {
                                            DifficultyTarget = target,
                                            Identifier = blockIdentifier,
                                            Nonce = nonce,
                                            PreviousBlockIdentifier = prevBlockId,
                                            Timestamp = Instant.FromSecondsSinceUnixEpoch(timestamp),
                                            Version = version,
                                            TransactionIdentifiers = includedTransactions.Select(x => x.TxId.ToLittleEndianUInt256Array().ToImmutableList()).ToMerkleTree(hashAlgorithm),
                                        };
                if (!newBlock.TransactionIdentifiers.Data.SequenceEqual(merkleRoot.ToLittleEndianUInt256Array()))
                {
                    return HandledNetworkMessageResult.ContextuallyInvalid;
                }

                ICoinbaseValueSource coinbase = new NetworkCoinbaseValueSource
                                                {
                                                    // For now, assume that the coinbase outputs are valid.
                                                    AvailableValue = includedTransactions[0].Outputs.Sum(x => x.ValueInSatoshis),
                                                    OriginatingBlockIdentifier = blockIdentifier
                                                };
                newBlock.Coinbase = coinbase;
                Dictionary<ITransaction, List<Task<bool>>> tScriptTasks = new Dictionary<ITransaction, List<Task<bool>>>();
                foreach (ProtocolTransaction newProtoTransaction in includedTransactions)
                {
                    NetworkTransaction newTransaction = new NetworkTransaction
                                                        {
                                                            Identifier = newProtoTransaction.TxId,
                                                            ContainingBlockIdentifier = blockIdentifier,
                                                            Inputs = ImmutableList<IValueSpender>.Empty,
                                                            Outputs = ImmutableList<ITransactionValueSource>.Empty,
                                                            Version = version,
                                                            LockTime = newProtoTransaction.LockTime
                                                        };

                    foreach (ProtocolTxIn txIn in newProtoTransaction.Inputs)
                    {
                        IValueSource source;
                        if (txIn.PrevOutTxId.IsZero)
                        {
                            source = coinbase;
                        }
                        else
                        {
                            ProtocolTransaction ownPrevTransaction;
                            if (ownTransactions.TryGetValue(txIn.PrevOutTxId, out ownPrevTransaction))
                            {
                                ProtocolTxOut prevOut = ownPrevTransaction.Outputs[(int)txIn.PrevOutN];
                                source = new NetworkTransactionValueSource
                                         {
                                             AvailableValue = prevOut.ValueInSatoshis,
                                             ScriptPublicKey = prevOut.ScriptPubKey
                                         };
                            }
                            else
                            {
                                ITransaction t;
                                if (!foundTransactions.TryGetValue(txIn.PrevOutTxId, out t))
                                {
                                    t = await neededTransactionFetchers[txIn.PrevOutTxId];
                                    foundTransactions[txIn.PrevOutTxId] = t;
                                }

                                source = t.Outputs[(int)txIn.PrevOutN];
                            }
                        }

                        NetworkValueSpender spender = new NetworkValueSpender
                                                      {
                                                          ScriptSignature = txIn.ScriptSig,
                                                          SpendingTransactionIdentifier = txIn.PrevOutTxId,
                                                          SpendingTransactionInputIndex = txIn.PrevOutN,
                                                          SpendingValueSource = source,
                                                          SequenceNumber = txIn.Sequence
                                                      };

                        newTransaction.Inputs = newTransaction.Inputs.Add(spender);
                    }

                    for (int i = 0; i < newProtoTransaction.Outputs.Count; i++)
                    {
                        ProtocolTxOut txOut = newProtoTransaction.Outputs[i];
                        NetworkTransactionValueSource source = new NetworkTransactionValueSource
                                                               {
                                                                   AvailableValue = txOut.ValueInSatoshis,
                                                                   OriginatingTransactionIdentifier = newTransaction.Identifier,
                                                                   OriginatingTransactionOutputIndex = (uint)i,
                                                                   ScriptPublicKey = txOut.ScriptPubKey
                                                               };

                        newTransaction.Outputs = newTransaction.Outputs.Add(source);
                    }

                    List<Task<bool>> scriptTasks = new List<Task<bool>>();
                    for (int i = 0; i < newTransaction.Inputs.Count; i++)
                    {
                        IValueSpender input = newTransaction.Inputs[i];
                        if (input.SpendingTransactionIdentifier.IsZero)
                        {
                            continue;
                        }

                        ImmutableList<byte> scriptSig = input.ScriptSignature;
                        ISignatureChecker signatureChecker = this.signatureCheckerFactory.CreateSignatureChecker(newTransaction, i);
                        Task<bool> scriptTask = Task.Run(() =>
                        {
                            var result = this.scriptRunner.EvaluateScript(scriptSig, signatureChecker);
                            if (!result)
                            {
                                return false;
                            }

                            ImmutableList<byte> scriptPubKey;
                            ProtocolTransaction prevTransaction;
                            if (ownTransactions.TryGetValue(input.SpendingTransactionIdentifier, out prevTransaction))
                            {
                                scriptPubKey = prevTransaction.Outputs[(int)input.SpendingTransactionInputIndex].ScriptPubKey;
                            }
                            else
                            {
                                scriptPubKey = foundTransactions[input.SpendingTransactionIdentifier].Outputs[(int)input.SpendingTransactionInputIndex].ScriptPublicKey;
                            }

                            return (bool)this.scriptRunner.EvaluateScript(scriptPubKey, signatureChecker, result.MainStack, result.AlternateStack);
                        }, token);

                        scriptTasks.Add(scriptTask);
                    }

                    tScriptTasks[newTransaction] = scriptTasks;
                }

                IBlock prevBlock = await prevBlockGetter;
                newBlock.Height = prevBlock.Height + 1;
                Cheating.Add((int)newBlock.Height, blockIdentifier);
                Task putBlockTask = this.ChainStore.PutBlockAsync(newBlock, token);

                foreach (var kvp in tScriptTasks)
                {
                    ITransaction newTransaction = kvp.Key;
                    List<Task<bool>> scriptTasks = kvp.Value;

                    bool[] results = await Task.WhenAll(scriptTasks);
                    if (results.Contains(false))
                    {
                        return HandledNetworkMessageResult.ContextuallyInvalid;
                    }

                    await this.ChainStore.PutTransactionAsync(newTransaction, token);
                }

                await putBlockTask;
            }

            return HandledNetworkMessageResult.Okay;
        }

        private static BigInteger TargetFromBits(uint bits)
        {
            uint mantissa = bits & 0x007fffff;
            bool negative = (bits & 0x00800000) != 0;
            byte exponent = (byte)(bits >> 24);
            BigInteger result;

            if (exponent <= 3)
            {
                mantissa >>= 8 * (3 - exponent);
                result = mantissa;
            }
            else
            {
                result = mantissa;
                result <<= 8 * (exponent - 3);
            }

            if ((result.Sign < 0) != negative)
            {
                result = -result;
            }

            return result;
        }
    }
}
