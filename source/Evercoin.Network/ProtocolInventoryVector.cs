using System;
using System.Collections.Immutable;
using System.Numerics;

using Evercoin.Util;

namespace Evercoin.Network
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

        public byte[] HashBytes
        {
            get
            {
                byte[] hash = this.Hash.ToByteArray();
                Array.Resize(ref hash, 32);
                return hash;
            }
        }

        public ImmutableList<byte> Data
        {
            get
            {
                return ImmutableList.CreateRange(BitConverter.GetBytes((uint)this.Type).LittleEndianToOrFromBitConverterEndianness())
                                    .AddRange(this.HashBytes);
            }
        }

        public enum InventoryType : uint
        {
            Transaction = 1,
            Block = 2
        }
    }
}
