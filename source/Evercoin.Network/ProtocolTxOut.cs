using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal sealed class ProtocolTxOut
    {
        public ImmutableList<byte> ByteRepresentation { get; set; }

        public long ValueInSatoshis { get; private set; }

        public ImmutableList<byte> ScriptPubKey { get; private set; }

        public async Task LoadFromStreamAsync(Stream stream, CancellationToken ct)
        {
            ImmutableList<byte> valueBytes = await stream.ReadBytesAsyncWithIntParam(8, ct);
            this.ValueInSatoshis = BitConverter.ToInt64(valueBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

            ProtocolCompactSize scriptPubKeySize = new ProtocolCompactSize();
            await scriptPubKeySize.LoadFromStreamAsync(stream, ct);
            this.ScriptPubKey = await stream.ReadBytesAsync(scriptPubKeySize.Value, ct);

            this.ByteRepresentation = ImmutableList.CreateRange(valueBytes)
                                                   .AddRange(this.ScriptPubKey);
        }
    }
}
