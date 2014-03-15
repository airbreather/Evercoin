using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Evercoin.BaseImplementations;
using Evercoin.Util;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolTransaction
    {
        public ProtocolTransaction(uint version, IEnumerable<ProtocolTxIn> inputs, IEnumerable<ProtocolTxOut> outputs, uint lockTime)
        {
            this.Version = version;
            this.Inputs = inputs.GetArray();
            this.Outputs = outputs.GetArray();
            this.LockTime = lockTime;
        }

        public uint Version { get; private set; }

        public ProtocolTxIn[] Inputs { get; private set; }

        public ProtocolTxOut[] Outputs { get; private set; }

        public uint LockTime { get; private set; }

        public BigInteger TxId { get; private set; }

        public void CalculateTxId(IHashAlgorithm alg)
        {
            byte[] dataToHash = this.Data;
            byte[] hashResult = alg.CalculateHash(dataToHash);
            this.TxId = new BigInteger(hashResult);
        }

        public byte[] Data
        {
            get
            {
                // Version
                byte[] versionBytes = BitConverter.GetBytes(this.Version).LittleEndianToOrFromBitConverterEndianness();

                // Inputs
                byte[] inputCountBytes = ((ProtocolCompactSize)(ulong)this.Inputs.Length).Data;
                IEnumerable<byte[]> inputByteSources = this.Inputs.Select(x => x.PrevOutTxId.ToLittleEndianUInt256Array()
                                                                                            .Concat(BitConverter.GetBytes(x.PrevOutN).LittleEndianToOrFromBitConverterEndianness())
                                                                                            .Concat(((ProtocolCompactSize)(ulong)x.ScriptSig.Length).Data)
                                                                                            .Concat(x.ScriptSig)
                                                                                            .Concat(BitConverter.GetBytes(x.Sequence).LittleEndianToOrFromBitConverterEndianness())
                                                                                            .GetArray());

                // Outputs
                byte[] outputCountBytes = ((ProtocolCompactSize)(ulong)this.Outputs.Length).Data;
                IEnumerable<byte[]> outputByteSources = this.Outputs.Select(x => BitConverter.GetBytes(x.ValueInSatoshis).LittleEndianToOrFromBitConverterEndianness()
                                                                                                                         .Concat(((ProtocolCompactSize)(ulong)x.ScriptPubKey.Length).Data)
                                                                                                                         .Concat(x.ScriptPubKey)
                                                                                                                         .GetArray());

                // Lock time
                byte[] lockTimeBytes = BitConverter.GetBytes(this.LockTime).LittleEndianToOrFromBitConverterEndianness();

                byte[] inputBytes = ByteTwiddling.ConcatenateData(inputByteSources);
                byte[] outputBytes = ByteTwiddling.ConcatenateData(outputByteSources);

                return ByteTwiddling.ConcatenateData(versionBytes, inputCountBytes, inputBytes, outputCountBytes, outputBytes, lockTimeBytes);
            }
        }

        public ITransaction ToTransaction(IDictionary<BigInteger, ITransaction> prevTransactions, IBlock spendingBlock)
        {
            return new TypedTransaction
            {
                LockTime = this.LockTime,
                Version = this.Version,
                Inputs = this.Inputs.Select((x, n) => new TypedValueSpender
                                                      {
                                                          ScriptSignature = x.ScriptSig,
                                                          SequenceNumber = x.Sequence,
                                                          SpendingTransactionIdentifier = this.TxId,
                                                          SpendingTransactionInputIndex = (uint)n,
                                                          SpendingValueSource = x.PrevOutTxId.IsZero ? 
                                                                                (IValueSource)spendingBlock.Coinbase :
                                                                                prevTransactions[x.PrevOutTxId].Outputs[(int)x.PrevOutN]
                                                      }).ToArray(),
                Outputs = this.Outputs.Select((x, n) => new TypedTransactionValueSource 
                                                        {
                                                            AvailableValue = x.ValueInSatoshis,
                                                            OriginatingTransactionIdentifier = this.TxId,
                                                            OriginatingTransactionOutputIndex = (uint)n,
                                                            ScriptPublicKey = x.ScriptPubKey
                                                        }).ToArray()
            };
        }
    }
}
