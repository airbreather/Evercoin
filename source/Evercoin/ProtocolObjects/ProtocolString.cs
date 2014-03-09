using System.Text;

using Evercoin.Util;

namespace Evercoin.ProtocolObjects
{
    public sealed class ProtocolString
    {
        private readonly string value;

        private readonly Encoding encoding;

        public ProtocolString(string value, Encoding encoding)
        {
            this.value = value;
            this.encoding = encoding;
        }

        public byte[] Data
        {
            get
            {
                byte[] data = this.encoding.GetBytes(this.value);

                ulong length = (ulong)data.Length;
                ProtocolCompactSize lengthVarInt = new ProtocolCompactSize(length);
                byte[] lengthBytes = lengthVarInt.Data;

                return ByteTwiddling.ConcatenateData(lengthBytes, data);
            }
        }
    }
}
