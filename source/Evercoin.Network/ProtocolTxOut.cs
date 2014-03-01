using System.Collections.Generic;
using System.Collections.Immutable;


namespace Evercoin.Network
{
    public sealed class ProtocolTxOut
    {
        public ProtocolTxOut(long valueInSatoshis, IEnumerable<byte> scriptPubKey)
        {
            this.ValueInSatoshis = valueInSatoshis;
            this.ScriptPubKey = scriptPubKey.ToImmutableList();
        }

        public long ValueInSatoshis { get; private set; }

        public ImmutableList<byte> ScriptPubKey { get; private set; }
    }
}
