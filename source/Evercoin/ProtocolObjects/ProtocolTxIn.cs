using System;
using System.Collections.Generic;

using Evercoin.Util;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolTxIn
    {
        public ProtocolTxIn(FancyByteArray prevOutTxId, uint prevOutIndex, IEnumerable<byte> scriptSig, uint seq)
        {
            this.PrevOutTxId = prevOutTxId;
            this.PrevOutN = prevOutIndex;
            this.ScriptSig = scriptSig.GetArray();
            this.Sequence = seq;
        }

        public FancyByteArray PrevOutTxId { get; private set; }

        public uint PrevOutN { get; private set; }

        public byte[] ScriptSig { get; private set; }

        public uint Sequence { get; private set; }

        public byte[] Data
        {
            get
            {
                byte[] prevOutTxIdBytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(this.PrevOutTxId, 32, Endianness.LittleEndian);
                byte[] prevOutNBytes = BitConverter.GetBytes(this.PrevOutN).LittleEndianToOrFromBitConverterEndianness();
                byte[] scriptSigLengthBytes = ((ProtocolCompactSize)(ulong)this.ScriptSig.Length).Data;
                byte[] scriptSigBytes = this.ScriptSig;
                byte[] sequenceBytes = BitConverter.GetBytes(this.Sequence).LittleEndianToOrFromBitConverterEndianness();

                return ByteTwiddling.ConcatenateData(prevOutTxIdBytes, prevOutNBytes, scriptSigLengthBytes, scriptSigBytes, sequenceBytes);
            }
        }
    }
}
