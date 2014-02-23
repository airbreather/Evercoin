using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Network
{
    internal sealed class ProtocolString
    {
        private readonly string value;

        private readonly Encoding encoding;

        public ProtocolString(string value, Encoding encoding)
        {
            this.value = value;
            this.encoding = encoding;
        }

        public ImmutableList<byte> Data
        {
            get
            {
                byte[] data = this.encoding.GetBytes(this.value);
                ulong length = (ulong)data.Length;
                ProtocolCompactSize lengthVarInt = new ProtocolCompactSize(length);
                return ImmutableList.CreateRange(lengthVarInt.Data)
                                    .AddRange(data);
            }
        }
    }
}
