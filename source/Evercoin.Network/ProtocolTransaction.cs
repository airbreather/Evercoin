using System.Collections.Generic;
using System.Collections.Immutable;

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
    }
}
