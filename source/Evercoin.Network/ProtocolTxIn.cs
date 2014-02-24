using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    public sealed class ProtocolTxIn
    {
        public ImmutableList<byte> ByteRepresentation { get; private set; }

        public string PrevOutTxId { get; private set; }

        public uint PrevOutN { get; private set; }

        public ImmutableList<byte> ScriptSig { get; private set; }

        public uint Sequence { get; private set; }

        public async Task LoadFromStreamAsync(Stream stream, CancellationToken ct)
        {
            ImmutableList<byte> prevOut = (await stream.ReadBytesAsyncWithIntParam(32, ct)).Reverse();
            this.PrevOutTxId = ByteTwiddling.ByteArrayToHexString(prevOut);

            ImmutableList<byte> nBytes = await stream.ReadBytesAsyncWithIntParam(4, ct);
            this.PrevOutN = BitConverter.ToUInt32(nBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

            ProtocolCompactSize scriptSigSize = new ProtocolCompactSize();
            await scriptSigSize.LoadFromStreamAsync(stream, ct);
            this.ScriptSig = await stream.ReadBytesAsync(scriptSigSize.Value, ct);

            ImmutableList<byte> seqBytes = await stream.ReadBytesAsyncWithIntParam(4, ct);
            this.Sequence = BitConverter.ToUInt32(seqBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

            this.ByteRepresentation = ImmutableList.CreateRange(prevOut.Reverse())
                                                   .AddRange(nBytes)
                                                   .AddRange(scriptSigSize.Data)
                                                   .AddRange(seqBytes);
        }
    }
}
