using System;

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

        public byte[] Data
        {
            get
            {
                byte[] result;
                if (this.Value < 0xfd)
                {
                    result = new byte[1];

                    result[0] = (byte)this.Value;
                }
                else if (this.Value <= 0xffff)
                {
                    result = new byte[3];

                    ushort shortValue = (ushort)this.Value;
                    byte[] shortValueBytes = BitConverter.GetBytes(shortValue)
                                                         .LittleEndianToOrFromBitConverterEndianness();

                    result[0] = 0xfd;
                    Buffer.BlockCopy(shortValueBytes, 0, result, 1, 2);
                }
                else if (this.Value <= 0xffffffff)
                {
                    result = new byte[5];

                    uint intValue = (uint)this.Value;
                    byte[] intValueBytes = BitConverter.GetBytes(intValue)
                                                       .LittleEndianToOrFromBitConverterEndianness();

                    result[0] = 0xfe;
                    Buffer.BlockCopy(intValueBytes, 0, result, 1, 4);
                }
                else
                {
                    result = new byte[9];
                    byte[] longValueBytes = BitConverter.GetBytes(this.Value)
                                                        .LittleEndianToOrFromBitConverterEndianness();

                    result[0] = 0xff;
                    Buffer.BlockCopy(longValueBytes, 0, result, 1, 8);
                }

                return result;
            }
        }
    }
}
