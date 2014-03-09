using System.Collections.Generic;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolTxOut
    {
        public ProtocolTxOut(long valueInSatoshis, IEnumerable<byte> scriptPubKey)
        {
            this.ValueInSatoshis = valueInSatoshis;
            this.ScriptPubKey = scriptPubKey.GetArray();
        }

        public long ValueInSatoshis { get; private set; }

        public byte[] ScriptPubKey { get; private set; }
    }
}
