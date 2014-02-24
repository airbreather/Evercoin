using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal sealed class ProtocolInventoryVector
    {
        public const int HashSize = 32;

        public ProtocolInventoryVector()
        {
        }

        public ProtocolInventoryVector(InventoryType type, IEnumerable<byte> hash)
        {
            this.Type = type;
            this.Hash = hash.ToImmutableList();
        }

        public InventoryType Type { get; private set; }

        public ImmutableList<byte> Hash { get; private set; }

        public ImmutableList<byte> GetData()
        {
            return ImmutableList.CreateRange(BitConverter.GetBytes((uint)this.Type).LittleEndianToOrFromBitConverterEndianness())
                                .AddRange(this.Hash.Reverse());
        }

        public async Task LoadFromStreamAsync(Stream stream, CancellationToken ct)
        {
            byte[] typeBytes = (await stream.ReadBytesAsyncWithIntParam(4, ct)).ToArray().LittleEndianToOrFromBitConverterEndianness();
            this.Type = (InventoryType)BitConverter.ToUInt32(typeBytes, 0);

            this.Hash = (await stream.ReadBytesAsyncWithIntParam(HashSize, ct)).Reverse();
        }

        public enum InventoryType : uint
        {
            Transaction = 1,
            Block = 2
        }
    }
}
