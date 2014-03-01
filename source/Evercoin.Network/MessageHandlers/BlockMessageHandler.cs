using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class BlockMessageHandler : MessageHandlerBase
    {
        private readonly IHashAlgorithmStore hashAlgorithmStore;
        private const string CoinbasePrevIn = "0000000000000000000000000000000000000000000000000000000000000000";
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("block");

        [ImportingConstructor]
        public BlockMessageHandler(INetwork network, IChainStore chainStore, IHashAlgorithmStore hashAlgorithmStore)
            : base(RecognizedCommand, network, chainStore)
        {
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            using (MemoryStream payloadStream = new MemoryStream(message.Payload.ToArray()))
            using (ProtocolStreamReader streamReader = new ProtocolStreamReader(payloadStream, leaveOpen: true))
            {
                uint version = await streamReader.ReadUInt32Async(token);
                BigInteger prevBlockId = await streamReader.ReadUInt256Async(token);
                BigInteger merkleRoot = await streamReader.ReadUInt256Async(token);
                uint timestamp = await streamReader.ReadUInt32Async(token);
                uint bits = await streamReader.ReadUInt32Async(token);
                uint nonce = await streamReader.ReadUInt32Async(token);
                ulong transactionCount = await streamReader.ReadCompactSizeAsync(token);
                ImmutableList<ProtocolTransaction> includedTransactions = ImmutableList<ProtocolTransaction>.Empty;

                while (transactionCount-- > 0)
                {
                    ProtocolTransaction nextTransaction = await streamReader.ReadTransactionAsync(token);
                    includedTransactions = includedTransactions.Add(nextTransaction);
                }

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
                BigInteger blockIdentifier = new BigInteger(blockHash.Reverse().ToArray());

                // For now, this will only ever be called for block #2 on the main chain.
                byte[] expectedBytes = ByteTwiddling.HexStringToByteArray("000000006A625F06636B8BB6AC7B960A8D03705D1ACE08B1A19DA3FDCC99DDBD");
                if (!expectedBytes.SequenceEqual(blockIdentifier.ToLittleEndianUInt256Array()))
                {
                    throw new InvalidOperationException("Got something other than block #2 on the main Bitcoin block chain!");
                }
            }

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
