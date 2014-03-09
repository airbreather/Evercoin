using System.Collections.Generic;
using System.Numerics;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolTxIn
    {
        public ProtocolTxIn(BigInteger prevOutTxId, uint prevOutIndex, IEnumerable<byte> scriptSig, uint seq)
        {
            this.PrevOutTxId = prevOutTxId;
            this.PrevOutN = prevOutIndex;
            this.ScriptSig = scriptSig.GetArray();
            this.Sequence = seq;
        }

        public BigInteger PrevOutTxId { get; private set; }

        public uint PrevOutN { get; private set; }

        public byte[] ScriptSig { get; private set; }

        public uint Sequence { get; private set; }
    }
}
