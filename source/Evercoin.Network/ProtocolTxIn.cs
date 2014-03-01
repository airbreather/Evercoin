using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace Evercoin.Network
{
    public sealed class ProtocolTxIn
    {
        public ProtocolTxIn(BigInteger prevOutTxId, uint prevOutIndex, IEnumerable<byte> scriptSig, uint seq)
        {
            this.PrevOutTxId = prevOutTxId;
            this.PrevOutN = prevOutIndex;
            this.ScriptSig = scriptSig.ToImmutableList();
            this.Sequence = seq;
        }

        public BigInteger PrevOutTxId { get; private set; }

        public uint PrevOutN { get; private set; }

        public ImmutableList<byte> ScriptSig { get; private set; }

        public uint Sequence { get; private set; }
    }
}
