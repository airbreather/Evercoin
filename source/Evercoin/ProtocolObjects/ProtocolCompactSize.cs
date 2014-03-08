using System;
using System.Collections.Immutable;

using Evercoin.Util;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolCompactSize
    {
        public ProtocolCompactSize(ulong value)
        {
            this.Value = value;
        }

        public ulong Value { get; private set; }

        public static implicit operator ulong(ProtocolCompactSize size)
        {
            return size.Value;
        }

        public static implicit operator ProtocolCompactSize(ulong value)
        {
            return new ProtocolCompactSize(value);
        }

        public ImmutableList<byte> Data
        {
            get
            {
                if (this.Value <= 0xfd)
                {
                    return ImmutableList.Create((byte)this.Value);
                }

                if (this.Value <= 0xffff)
                {
                    ushort shortValue = (ushort)this.Value;
                    return ImmutableList.Create((byte)0xfd)
                                        .AddRange(BitConverter.GetBytes(shortValue)
                                                              .LittleEndianToOrFromBitConverterEndianness());
                }

                if (this.Value <= 0xffffffff)
                {
                    uint intValue = (uint)this.Value;
                    return ImmutableList.Create((byte)0xfe)
                                        .AddRange(BitConverter.GetBytes(intValue)
                                                              .LittleEndianToOrFromBitConverterEndianness());
                }

                return ImmutableList.Create((byte)0xfe)
                                    .AddRange(BitConverter.GetBytes(this.Value)
                                                          .LittleEndianToOrFromBitConverterEndianness());
            }
        }
    }
}
