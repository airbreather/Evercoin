using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal sealed class ProtocolCompactSize
    {
        private ulong value;

        public ProtocolCompactSize()
        {
        }

        public ProtocolCompactSize(ulong value)
        {
            this.value = value;
        }

        public ImmutableList<byte> Data
        {
            get
            {
                if (this.value <= 0xfd)
                {
                    return ImmutableList.Create((byte)this.value);
                }

                if (this.value <= 0xffff)
                {
                    ushort shortValue = (ushort)this.value;
                    return ImmutableList.Create((byte)0xfd)
                                        .AddRange(BitConverter.GetBytes(shortValue)
                                                              .LittleEndianToOrFromBitConverterEndianness());
                }

                if (this.value <= 0xffffffff)
                {
                    uint intValue = (uint)this.value;
                    return ImmutableList.Create((byte)0xfe)
                                        .AddRange(BitConverter.GetBytes(intValue)
                                                              .LittleEndianToOrFromBitConverterEndianness());
                }

                return ImmutableList.Create((byte)0xfe)
                                    .AddRange(BitConverter.GetBytes(this.value)
                                                          .LittleEndianToOrFromBitConverterEndianness());
            }
        }

        public async Task LoadFromStreamAsync(Stream stream)
        {
            byte firstByte = (await stream.ReadBytesAsync(1))[0];
            switch (firstByte)
            {
                case 0xfd:
                    byte[] shortBytes = (await stream.ReadBytesAsync(2)).ToArray();
                    this.value = BitConverter.ToUInt16(shortBytes.LittleEndianToOrFromBitConverterEndianness(), 0);
                    break;

                case 0xfe:
                    byte[] intBytes = (await stream.ReadBytesAsync(4)).ToArray();
                    this.value = BitConverter.ToUInt32(intBytes.LittleEndianToOrFromBitConverterEndianness(), 0);
                    break;

                case 0xff:
                    byte[] longBytes = (await stream.ReadBytesAsync(8)).ToArray();
                    this.value = BitConverter.ToUInt64(longBytes.LittleEndianToOrFromBitConverterEndianness(), 0);
                    break;

                default:
                    this.value = firstByte;
                    break;
            }
        }
    }
}
