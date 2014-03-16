using System;
using System.Collections.Generic;

using Evercoin.Util;

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

        public byte[] Data
        {
            get
            {
                byte[] valueBytes = BitConverter.GetBytes(this.ValueInSatoshis).LittleEndianToOrFromBitConverterEndianness();
                byte[] scriptPubKeyLengthBytes = ((ProtocolCompactSize)(ulong)this.ScriptPubKey.Length).Data;
                byte[] scriptPubKeyBytes = this.ScriptPubKey;

                return ByteTwiddling.ConcatenateData(valueBytes, scriptPubKeyLengthBytes, scriptPubKeyBytes);
            }
        }
    }
}
