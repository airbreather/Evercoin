using System;
using System.Collections.Generic;
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
        private readonly IChainStore chainStore;

        private readonly ITransactionScriptRunner scriptRunner;
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("block");

        public BlockMessageHandler(INetwork network, IChainStore chainStore, IHashAlgorithmStore hashAlgorithmStore, ITransactionScriptRunner scriptRunner, ISignatureCheckerFactory signatureCheckerFactory)
            : base(RecognizedCommand, network)
        {
            this.chainStore = chainStore;
            this.hashAlgorithmStore = hashAlgorithmStore;
            this.signatureCheckerFactory = signatureCheckerFactory;
            this.scriptRunner = scriptRunner;
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            uint version;
            BigInteger prevBlockId;
            Task<IBlock> prevBlockGetter;
            BigInteger merkleRoot;
            uint timestamp;
            uint bits;
            uint nonce;
            List<ProtocolTransaction> includedTransactions = new List<ProtocolTransaction>();
            HashSet<BigInteger> neededTransactions = new HashSet<BigInteger>();

            using (MemoryStream payloadStream = new MemoryStream(message.Payload))
            using (ProtocolStreamReader streamReader = new ProtocolStreamReader(payloadStream, true, this.hashAlgorithmStore))
            {
                version = await streamReader.ReadUInt32Async(token);
                prevBlockId = await streamReader.ReadUInt256Async(token);
                prevBlockGetter = this.chainStore.GetBlockAsync(prevBlockId, token);
                merkleRoot = await streamReader.ReadUInt256Async(token);
                timestamp = await streamReader.ReadUInt32Async(token);
                bits = await streamReader.ReadUInt32Async(token);
                nonce = await streamReader.ReadUInt32Async(token);

                bool checkedCoinbase = false;
                ulong transactionCount = await streamReader.ReadCompactSizeAsync(token);
                while (transactionCount-- > 0)
                {
                    ProtocolTransaction nextTransaction = await streamReader.ReadTransactionAsync(token);
                    if (!checkedCoinbase)
                    {
                        checkedCoinbase = true;

                        // This is important to do now -- for some reason, the Satoshi client gives us recent blocks even when we're building from the genesis block!
                        // So, one thing we REALLY don't want to do is to fetch all the old transactions!
                        ProtocolTxIn cb = nextTransaction.Inputs[0];
                        if (version > 1 && cb.ScriptSig[0] == 3)
                        {
                            byte[] serializedHeight = cb.ScriptSig.Skip(1).Take(3).GetArray();
                            Array.Resize(ref serializedHeight, 4);

                            uint probableHeight = BitConverter.ToUInt32(serializedHeight.LittleEndianToOrFromBitConverterEndianness(), 0);
                            if (probableHeight - Cheating.GetBlockIdentifierCount() > 1000)
                            {
                                // So we've got a block message from a recent block.
                                // Don't waste any more time on it than we have.
                                return HandledNetworkMessageResult.ContextuallyInvalid;
                            }
                        }
                    }

                    includedTransactions.Add(nextTransaction);
                    foreach (BigInteger prevTxId in nextTransaction.Inputs.Select(x => x.PrevOutTxId).Where(x => !x.IsZero))
                    {
                        neededTransactions.Add(prevTxId);
                    }
                }
            }

            byte[] versionBytes = BitConverter.GetBytes(version).LittleEndianToOrFromBitConverterEndianness();
            byte[] prevBlockIdBytes = prevBlockId.ToLittleEndianUInt256Array();
            byte[] merkleRootBytes = merkleRoot.ToLittleEndianUInt256Array();
            byte[] timestampBytes = BitConverter.GetBytes(timestamp).LittleEndianToOrFromBitConverterEndianness();
            byte[] packedTargetBytes = BitConverter.GetBytes(bits).LittleEndianToOrFromBitConverterEndianness();
            byte[] nonceBytes = BitConverter.GetBytes(nonce).LittleEndianToOrFromBitConverterEndianness();

            byte[] dataToHash = ByteTwiddling.ConcatenateData(versionBytes, prevBlockIdBytes, merkleRootBytes, timestampBytes, packedTargetBytes, nonceBytes);

            // TODO: This should definitely be done somewhere else, because the POW algorithm is chain-specific,
            // TODO: which is part of why I'm so hesitant to have the ID on IBlock.
            IHashAlgorithm hashAlgorithm = this.hashAlgorithmStore.GetHashAlgorithm(HashAlgorithmIdentifiers.DoubleSHA256);
            byte[] blockHash = hashAlgorithm.CalculateHash(dataToHash);
            BigInteger blockIdentifier = new BigInteger(blockHash);

            Dictionary<BigInteger, Task<ITransaction>> neededTransactionFetchers = neededTransactions.ToDictionary(x => x, x => this.chainStore.GetTransactionAsync(x, token));

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
                                        TransactionIdentifiers = includedTransactions.Select(x => x.TxId.ToLittleEndianUInt256Array()).ToMerkleTree(hashAlgorithm),
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
            foreach (ProtocolTransaction newProtoTransaction in includedTransactions)
            {
                NetworkTransaction newTransaction = new NetworkTransaction
                                                    {
                                                        Identifier = newProtoTransaction.TxId,
                                                        ContainingBlockIdentifier = blockIdentifier,
                                                        Version = version,
                                                        Inputs = new IValueSpender[newProtoTransaction.Inputs.Length],
                                                        Outputs = new ITransactionValueSource[newProtoTransaction.Outputs.Length],
                                                        LockTime = newProtoTransaction.LockTime
                                                    };

                int inputIndex = 0;
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

                    newTransaction.Inputs[inputIndex++] = spender;
                }


                for (int outputIndex = 0; outputIndex < newProtoTransaction.Outputs.Length; outputIndex++)
                {
                    ProtocolTxOut txOut = newProtoTransaction.Outputs[outputIndex];
                    NetworkTransactionValueSource source = new NetworkTransactionValueSource
                                                           {
                                                               AvailableValue = txOut.ValueInSatoshis,
                                                               OriginatingTransactionIdentifier = newTransaction.Identifier,
                                                               OriginatingTransactionOutputIndex = (uint)outputIndex,
                                                               ScriptPublicKey = txOut.ScriptPubKey
                                                           };

                    newTransaction.Outputs[outputIndex] = source;
                }

                bool badScriptFound = false;
                ParallelOptions options = new ParallelOptions { CancellationToken = token };
                Parallel.For(0, newTransaction.Inputs.Length, options, (i, loopState) =>
                {
                    IValueSpender input = newTransaction.Inputs[i];
                    if (input.SpendingTransactionIdentifier.IsZero)
                    {
                        return;
                    }

                    if (loopState.IsStopped)
                    {
                        return;
                    }

                    byte[] scriptSig = input.ScriptSignature;
                    ISignatureChecker signatureChecker = this.signatureCheckerFactory.CreateSignatureChecker(newTransaction, i);
                    var result = this.scriptRunner.EvaluateScript(scriptSig, signatureChecker);
                    if (!result)
                    {
                        badScriptFound = true;
                        loopState.Stop();
                        return;
                    }

                    if (loopState.IsStopped)
                    {
                        return;
                    }

                    byte[] scriptPubKey;
                    ProtocolTransaction prevTransaction;
                    if (ownTransactions.TryGetValue(input.SpendingTransactionIdentifier, out prevTransaction))
                    {
                        scriptPubKey = prevTransaction.Outputs[(int)input.SpendingTransactionInputIndex].ScriptPubKey;
                    }
                    else
                    {
                        scriptPubKey = foundTransactions[input.SpendingTransactionIdentifier].Outputs[(int)input.SpendingTransactionInputIndex].ScriptPublicKey;
                    }

                    if (loopState.IsStopped)
                    {
                        return;
                    }

                    if (!this.scriptRunner.EvaluateScript(scriptPubKey, signatureChecker, result.MainStack, result.AlternateStack))
                    {
                        badScriptFound = true;
                        loopState.Stop();
                    }
                });

                if (badScriptFound)
                {
                    return HandledNetworkMessageResult.ContextuallyInvalid;
                }

                await this.chainStore.PutTransactionAsync(newTransaction, token);
            }

            IBlock prevBlock = await prevBlockGetter;
            newBlock.Height = prevBlock.Height + 1;
            Cheating.Add((int)newBlock.Height, blockIdentifier);
            await this.chainStore.PutBlockAsync(newBlock, token);

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
