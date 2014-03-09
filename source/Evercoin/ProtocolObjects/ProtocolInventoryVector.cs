using System;
using System.Collections.Immutable;
using System.Numerics;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolInventoryVector
    {
        public ProtocolInventoryVector(InventoryType type, BigInteger hash)
        {
            this.Type = type;
            this.Hash = hash;
        }

        public InventoryType Type { get; private set; }

        public BigInteger Hash { get; private set; }

        public ImmutableList<byte> Data
        {
            get
            {
                return ImmutableList.CreateRange(BitConverter.GetBytes((uint)this.Type).LittleEndianToOrFromBitConverterEndianness())
                                    .AddRange(this.Hash.ToLittleEndianUInt256Array());
            }
        }

        public enum InventoryType : uint
        {
            Transaction = 1,
            Block = 2
        }
    }
}
