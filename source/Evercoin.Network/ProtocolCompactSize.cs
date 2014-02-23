using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    public sealed class ProtocolCompactSize
    {
        public ProtocolCompactSize()
        {
        }

        public ProtocolCompactSize(ulong value)
        {
            this.Value = value;
        }

        public ulong Value { get; private set; }

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

        public async Task LoadFromStreamAsync(Stream stream, CancellationToken token)
        {
            byte firstByte = (await stream.ReadBytesAsync(1, token))[0];
            switch (firstByte)
            {
                case 0xfd:
                    byte[] shortBytes = (await stream.ReadBytesAsync(2, token)).ToArray();
                    this.Value = BitConverter.ToUInt16(shortBytes.LittleEndianToOrFromBitConverterEndianness(), 0);
                    break;

                case 0xfe:
                    byte[] intBytes = (await stream.ReadBytesAsync(4, token)).ToArray();
                    this.Value = BitConverter.ToUInt32(intBytes.LittleEndianToOrFromBitConverterEndianness(), 0);
                    break;

                case 0xff:
                    byte[] longBytes = (await stream.ReadBytesAsync(8, token)).ToArray();
                    this.Value = BitConverter.ToUInt64(longBytes.LittleEndianToOrFromBitConverterEndianness(), 0);
                    break;

                default:
                    this.Value = firstByte;
                    break;
            }
        }
    }
}
