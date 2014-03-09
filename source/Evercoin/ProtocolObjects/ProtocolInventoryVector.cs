using System;
using System.Numerics;

using Evercoin.Util;

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

        public byte[] Data
        {
            get
            {
                byte[] typeBytes = BitConverter.GetBytes((uint)this.Type)
                                               .LittleEndianToOrFromBitConverterEndianness();
                byte[] hashBytes = this.Hash.ToLittleEndianUInt256Array();

                return ByteTwiddling.ConcatenateData(typeBytes, hashBytes);
            }
        }

        public enum InventoryType : uint
        {
            Transaction = 1,
            Block = 2
        }
    }
}
