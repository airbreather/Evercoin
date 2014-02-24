using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Evercoin.BaseImplementations;
using Evercoin.Util;

using Json;

using NodaTime;

namespace Evercoin.Storage
{
    [Export(typeof(IChainStore))]
    public sealed class BCInfoChainStorage : ChainStoreBase
    {
        private const string GetBlockUrlFormat = "http://blockchain.info/rawblock/{0}";

        private const string GetTransactionUrlFormat = "http://blockchain.info/rawtx/{0}?scripts=true";

        private const string GetTransactionFromIndexUrlFormat = "https://blockchain.info/tx-index/{0}?format=json&scripts=true";

        private readonly DiskChainStorage dcs;

        [ImportingConstructor]
        public BCInfoChainStorage([Import("Disk.BlockStorageFolderPath")] string blockStoragePath,
            [Import("Disk.TransactionStorageFolderPath")] string transactionStoragePath)
        {
            this.dcs = new DiskChainStorage(blockStoragePath, transactionStoragePath);
        }

        private readonly object reqLock = new object();

        public override bool TryGetBlock(string blockIdentifier, out IBlock block)
        {
            if (this.dcs.TryGetBlock(blockIdentifier, out block))
            {
                return true;
            }

            try
            {
                string blockJSON = this.GetJSON(GetBlockUrlFormat, blockIdentifier);
                dynamic json = JsonParser.Deserialize(blockJSON);
                SerializableBlock sblock = new SerializableBlock();
                sblock.Identifier = blockIdentifier;
                sblock.DifficultyTarget = (ulong)json.bits;
                sblock.Version = json.ver;
                sblock.Timestamp = Instant.FromSecondsSinceUnixEpoch(json.time);
                sblock.Nonce = json.nonce;
                sblock.Height = json.height;
                foreach (dynamic jsonTx in json.tx)
                {
                    sblock.Transactions.Add(this.ParseTxJSON(this.GetJSON(GetTransactionUrlFormat, jsonTx.hash)));
                }

                sblock.Coinbase = sblock.Transactions[0].Inputs[0];

                block = sblock;
                return true;
            }
            catch (WebException)
            {
                block = null;
                return false;
            }
        }

        public override bool TryGetTransaction(string transactionIdentifier, out ITransaction transaction)
        {
            if (this.dcs.TryGetTransaction(transactionIdentifier, out transaction))
            {
                return true;
            }

            try
            {
                string transactionJSON = this.GetJSON(GetTransactionUrlFormat, transactionIdentifier);
                transaction = this.ParseTxJSON(transactionJSON);
                return true;
            }
            catch (WebException)
            {
                transaction = null;
                return false;
            }
        }

        public override bool ContainsTransaction(string transactionIdentifier)
        {
            return true;
        }

        public override bool ContainsBlock(string blockIdentifier)
        {
            return true;
        }

        private string GetJSON(string urlFormat, string urlParameter)
        {
            lock (reqLock)
            {
                Thread.Sleep(200);
                HttpWebRequest req = HttpWebRequest.CreateHttp(String.Format(CultureInfo.InvariantCulture, urlFormat, urlParameter));
                using (StreamReader sr = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private SerializableTransaction ParseTxJSON(string text)
        {
            SerializableTransaction sTrans = new SerializableTransaction();
            dynamic json = JsonParser.Deserialize(text);
            sTrans.Identifier = json.hash;
            sTrans.Version = (uint)json.ver;

            foreach (dynamic jsonTxOut in json.@out)
            {
                SerializableTransactionValueSource svs = new SerializableTransactionValueSource
                {
                    ChainStore = this,
                    AvailableValue = (int)jsonTxOut["value"],
                    ScriptPubKey = ByteTwiddling.HexStringToByteArray(((string)jsonTxOut["script"]).ToUpperInvariant()),
                    Transaction = sTrans
                };
                sTrans.Outputs.Add(svs);
            }

            foreach (dynamic jsonTxIn in json.inputs)
            {
                SerializableValueSource svs = new SerializableValueSource();
                dynamic jsonPrevOut = jsonTxIn["prev_out"];
                if (jsonPrevOut != null)
                {
                    string prevTx = this.GetJSON(GetTransactionFromIndexUrlFormat, jsonPrevOut["tx_index"].ToString());
                    dynamic prevTxJson = JsonParser.Deserialize(prevTx);
                    svs = new SerializableTransactionValueSource
                          {
                              ChainStore = this,
                              TransactionIdentifier = prevTxJson.hash,
                              AvailableValue = (int)jsonPrevOut["value"]
                          };
                }
                else
                {
                    svs.AvailableValue = sTrans.Outputs.Sum(x => x.AvailableValue);
                }

                svs.ScriptPubKey = ByteTwiddling.HexStringToByteArray(jsonTxIn["script"].ToUpperInvariant());
                sTrans.Inputs.Add(svs);
            }

            this.PutTransaction(sTrans);
            return sTrans;
        }

        public override void PutBlock(IBlock block)
        {
            this.dcs.PutBlock(block);
        }

        public override void PutTransaction(ITransaction transaction)
        {
            this.dcs.PutTransaction(transaction);
        }
    }
}
