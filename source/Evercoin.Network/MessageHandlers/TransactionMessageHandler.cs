using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Network.MessageHandlers
{
    public sealed class TransactionMessageHandler : MessageHandlerBase
    {
        private const string CoinbasePrevIn = "0000000000000000000000000000000000000000000000000000000000000000";
        private static readonly byte[] RecognizedCommand = Encoding.ASCII.GetBytes("tx");

        private readonly GetDataMessageBuilder builder;

        [ImportingConstructor]
        public TransactionMessageHandler(INetwork network, IChainStore chainStore)
            : base(RecognizedCommand, network, chainStore)
        {
            this.builder = new GetDataMessageBuilder(network);
        }

        protected override async Task<HandledNetworkMessageResult> HandleMessageAsyncCore(INetworkMessage message, CancellationToken token)
        {
            /*
            NetworkTransaction transaction = new NetworkTransaction();
            SHA256 theWrongWayToHash = SHA256.Create();
            using (MemoryStream payloadStream = new MemoryStream(message.Payload.ToArray()))
            {
                ProtocolTransaction protoTransaction = new ProtocolTransaction();
                await protoTransaction.LoadFromStreamAsync(payloadStream, CancellationToken.None);

                byte[] transactionHash = theWrongWayToHash.ComputeHash(theWrongWayToHash.ComputeHash(protoTransaction.ByteRepresentation.ToArray()));
                transaction.Identifier = ByteTwiddling.ByteArrayToHexString(transactionHash);
                transaction.Version = protoTransaction.Version;

                foreach (ProtocolTxIn txIn in protoTransaction.Inputs)
                {
                    if (txIn.PrevOutTxId == CoinbasePrevIn)
                    {
                        continue;
                    }

                    NetworkTransactionValueSource valueSource = new NetworkTransactionValueSource();
                    if (!await this.ReadOnlyChainStore.ContainsTransactionAsync(txIn.PrevOutTxId))
                    {
                        INetworkMessage newMessage = this.builder.BuildGetDataMessage(message.RemoteClient, ImmutableList.Create(ImmutableList.Create(ByteTwiddling.HexStringToByteArray(txIn.PrevOutTxId))));
                        await this.Network.SendMessageToClientAsync(message.RemoteClient, newMessage);
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

                    foreach (byte scriptByte in txOut.ScriptPubKey)
                    {
                        valueSource.ScriptPubKey.Add(scriptByte);
                    }

                    transaction.Outputs.Add(valueSource);
                }

                await this.ChainStore.PutTransactionAsync(transaction);
            } */

            return HandledNetworkMessageResult.Okay;
        }
    }
}
