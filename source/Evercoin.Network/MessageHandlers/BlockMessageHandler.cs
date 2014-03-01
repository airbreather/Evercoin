using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

using NodaTime;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class BlockMessageHandler : MessageHandlerBase
    {
        private const string CoinbasePrevIn = "0000000000000000000000000000000000000000000000000000000000000000";
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("block");

        [ImportingConstructor]
        public BlockMessageHandler(INetwork network, IChainStore chainStore)
            : base(RecognizedCommand, network, chainStore)
        {
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            /*
            NetworkBlock block = new NetworkBlock();
            List<Task> putThingsTasks = new List<Task>();
            using (MemoryStream payloadStream = new MemoryStream(message.Payload.ToArray()))
            {
                ImmutableList<byte> versionBytes = await payloadStream.ReadBytesAsyncWithIntParam(4, CancellationToken.None);
                block.Version = BitConverter.ToUInt32(versionBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

                ImmutableList<byte> prevBlockBytes = await payloadStream.ReadBytesAsyncWithIntParam(32, CancellationToken.None);
                block.PreviousBlockIdentifier = ByteTwiddling.ByteArrayToHexString(prevBlockBytes);

                ImmutableList<byte> unused = await payloadStream.ReadBytesAsyncWithIntParam(32, CancellationToken.None);

                ImmutableList<byte> timestampBytes = await payloadStream.ReadBytesAsyncWithIntParam(4, CancellationToken.None);
                uint timestamp = BitConverter.ToUInt32(timestampBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);
                block.Timestamp = Instant.FromSecondsSinceUnixEpoch(timestamp);

                ImmutableList<byte> difficultyTargetBytes = await payloadStream.ReadBytesAsyncWithIntParam(4, CancellationToken.None);
                block.DifficultyTarget = BitConverter.ToUInt32(difficultyTargetBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

                ImmutableList<byte> nonceBytes = await payloadStream.ReadBytesAsyncWithIntParam(4, CancellationToken.None);
                block.Nonce = BitConverter.ToUInt32(nonceBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

                ImmutableList<byte> dataToHash = ImmutableList.CreateRange(versionBytes)
                                                              .AddRange(prevBlockBytes)
                                                              .AddRange(unused)
                                                              .AddRange(timestampBytes)
                                                              .AddRange(difficultyTargetBytes)
                                                              .AddRange(nonceBytes);

                // Hey look -- I'm cheating!
                byte[] dataToHashInAnArray = dataToHash.ToArray();
                SHA256 theWrongWayToHash = SHA256.Create();
                byte[] blockHash = theWrongWayToHash.ComputeHash(theWrongWayToHash.ComputeHash(dataToHashInAnArray));
                block.Identifier = ByteTwiddling.ByteArrayToHexString(blockHash);

                ProtocolCompactSize transactionCount = new ProtocolCompactSize();
                await transactionCount.LoadFromStreamAsync(payloadStream, CancellationToken.None);

                decimal coinbaseValue = 0;

                while ((ulong)block.Transactions.Count < transactionCount.Value)
                {
                    bool isCoinbase = false;
                    NetworkTransaction transaction = new NetworkTransaction();
                    ProtocolTransaction protoTransaction = new ProtocolTransaction();
                    await protoTransaction.LoadFromStreamAsync(payloadStream, CancellationToken.None);

                    byte[] transactionHash = theWrongWayToHash.ComputeHash(theWrongWayToHash.ComputeHash(protoTransaction.ByteRepresentation.ToArray()));
                    transaction.Identifier = ByteTwiddling.ByteArrayToHexString(transactionHash);
                    transaction.Version = protoTransaction.Version;

                    foreach (ProtocolTxIn txIn in protoTransaction.Inputs)
                    {
                        if (txIn.PrevOutTxId == CoinbasePrevIn)
                        {
                            isCoinbase = true;
                            continue;
                        }

                        NetworkTransactionValueSource valueSource = new NetworkTransactionValueSource();
                        if (!await this.ReadOnlyChainStore.ContainsTransactionAsync(txIn.PrevOutTxId))
                        {
                            return HandledNetworkMessageResult.ContextuallyInvalid;
                        }

                        ITransaction prevTransaction = await this.ReadOnlyChainStore.GetTransactionAsync(txIn.PrevOutTxId);
                        IValueSource prevOut = prevTransaction.Outputs.ElementAtOrDefault((int)txIn.PrevOutN);
                        if (prevOut == null)
                        {
                            return HandledNetworkMessageResult.ContextuallyInvalid;
                        }

                        valueSource.Transaction = new NetworkTransaction(prevTransaction);
                        valueSource.AvailableValue = prevOut.AvailableValue;
                        foreach (byte scriptByte in prevOut.ScriptPubKey)
                        {
                            valueSource.ScriptPubKey.Add(scriptByte);
                        }

                        transaction.Inputs.Add(valueSource);
                    }

                    foreach (ProtocolTxOut txOut in protoTransaction.Outputs)
                    {
                        NetworkTransactionValueSource valueSource = new NetworkTransactionValueSource
                                                                    {
                                                                        AvailableValue = txOut.ValueInSatoshis,
                                                                        Transaction = transaction
                                                                    };

                        if (isCoinbase)
                        {
                            coinbaseValue += valueSource.AvailableValue;
                        }

                        foreach (byte scriptByte in txOut.ScriptPubKey)
                        {
                            valueSource.ScriptPubKey.Add(scriptByte);
                        }

                        transaction.Outputs.Add(valueSource);
                    }

                    putThingsTasks.Add(this.ChainStore.PutTransactionAsync(transaction));
                }

                block.Coinbase = new NetworkValueSource
                                 {
                                     AvailableValue = coinbaseValue
                                 };

                putThingsTasks.Add(this.ChainStore.PutBlockAsync(block));
            }

            await Task.WhenAll(putThingsTasks);*/
            return HandledNetworkMessageResult.Okay;
        }
    }
}
