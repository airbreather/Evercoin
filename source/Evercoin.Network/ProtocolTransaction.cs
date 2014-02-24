using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal sealed class ProtocolTransaction
    {
        public ImmutableList<byte> ByteRepresentation { get; private set; }

        public uint Version { get; private set; }

        public ImmutableList<ProtocolTxIn> Inputs { get; private set; }

        public ImmutableList<ProtocolTxOut> Outputs { get; private set; }

        public uint LockTime { get; private set; }

        public async Task LoadFromStreamAsync(Stream stream, CancellationToken ct)
        {
            ImmutableList<byte> versionBytes = await stream.ReadBytesAsyncWithIntParam(4, ct);
            this.Version = BitConverter.ToUInt32(versionBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

            ProtocolCompactSize inputCount = new ProtocolCompactSize();
            await inputCount.LoadFromStreamAsync(stream, ct);

            this.Inputs = ImmutableList<ProtocolTxIn>.Empty;

            while ((ulong)this.Inputs.Count < inputCount.Value)
            {
                ProtocolTxIn txIn = new ProtocolTxIn();
                await txIn.LoadFromStreamAsync(stream, ct);
                this.Inputs = Inputs.Add(txIn);
            }

            ProtocolCompactSize outputCount = new ProtocolCompactSize();
            await outputCount.LoadFromStreamAsync(stream, ct);

            this.Outputs = ImmutableList<ProtocolTxOut>.Empty;
            while ((ulong)this.Outputs.Count < outputCount.Value)
            {
                ProtocolTxOut txOut = new ProtocolTxOut();
                await txOut.LoadFromStreamAsync(stream, ct);
                this.Outputs = this.Outputs.Add(txOut);
            }

            ImmutableList<byte> lockTimeBytes = await stream.ReadBytesAsyncWithIntParam(4, ct);
            this.LockTime = BitConverter.ToUInt32(lockTimeBytes.ToArray().LittleEndianToOrFromBitConverterEndianness(), 0);

            this.ByteRepresentation = ImmutableList.CreateRange(versionBytes)
                                                   .AddRange(inputCount.Data);
            foreach (ProtocolTxIn txIn in this.Inputs)
            {
                this.ByteRepresentation = this.ByteRepresentation.AddRange(txIn.ByteRepresentation);
            }

            this.ByteRepresentation = this.ByteRepresentation.AddRange(outputCount.Data);

            foreach (ProtocolTxOut txOut in this.Outputs)
            {
                this.ByteRepresentation = this.ByteRepresentation.AddRange(txOut.ByteRepresentation);
            }

            this.ByteRepresentation = this.ByteRepresentation.AddRange(lockTimeBytes);
        }
    }
}
