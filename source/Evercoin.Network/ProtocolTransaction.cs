using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

using Evercoin.Util;

namespace Evercoin.Network
{
    public sealed class ProtocolTransaction
    {
        public ProtocolTransaction(uint version, IEnumerable<ProtocolTxIn> inputs, IEnumerable<ProtocolTxOut> outputs, uint lockTime)
        {
            this.Version = version;
            this.Inputs = inputs.ToImmutableList();
            this.Outputs = outputs.ToImmutableList();
            this.LockTime = lockTime;
        }

        public uint Version { get; private set; }

        public ImmutableList<ProtocolTxIn> Inputs { get; private set; }

        public ImmutableList<ProtocolTxOut> Outputs { get; private set; }

        public uint LockTime { get; private set; }

        public BigInteger TxId { get; private set; }

        public void CalculateTxId(IHashAlgorithm alg)
        {
            ImmutableList<byte> dataToHash = ImmutableList.CreateRange(BitConverter.GetBytes(this.Version).LittleEndianToOrFromBitConverterEndianness())
                .AddRange(((ProtocolCompactSize)(ulong)this.Inputs.Count).Data)
                .AddRange(this.Inputs.SelectMany(x =>
                    x.PrevOutTxId.ToLittleEndianUInt256Array()
                        .Concat(BitConverter.GetBytes(x.PrevOutN).LittleEndianToOrFromBitConverterEndianness())
                        .Concat(((ProtocolCompactSize)(ulong)x.ScriptSig.Count).Data)
                        .Concat(x.ScriptSig)
                        .Concat(BitConverter.GetBytes(x.Sequence).LittleEndianToOrFromBitConverterEndianness())))
                .AddRange(((ProtocolCompactSize)(ulong)this.Outputs.Count).Data)
                .AddRange(this.Outputs.SelectMany(x => BitConverter.GetBytes(x.ValueInSatoshis).LittleEndianToOrFromBitConverterEndianness()
                    .Concat(((ProtocolCompactSize)(ulong)x.ScriptPubKey.Count).Data)
                    .Concat(x.ScriptPubKey)))
                .AddRange(BitConverter.GetBytes(this.LockTime).LittleEndianToOrFromBitConverterEndianness())
                .ToImmutableList();

            ImmutableList<byte> hashResult = alg.CalculateHash(dataToHash);
            this.TxId = new BigInteger(hashResult.ToArray());
        }
    }
}
