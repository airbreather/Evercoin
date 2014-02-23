using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    internal sealed class ProtocolNetworkAddress
    {
        private readonly uint time;

        private readonly ulong services;

        private readonly IPAddress address;

        private readonly ushort port;

        public ProtocolNetworkAddress(uint time, ulong services, IPAddress address, ushort port)
        {
            this.time = time;
            this.services = services;
            this.address = address;
            this.port = port;
        }

        public ImmutableList<byte> Data
        {
            get
            {
                // Warning: the address and port will be big-endian,
                // unlike everything else in the protocol.
                return ImmutableList.CreateRange(BitConverter.GetBytes(this.time).LittleEndianToOrFromBitConverterEndianness())
                                    .AddRange(BitConverter.GetBytes(this.services).LittleEndianToOrFromBitConverterEndianness())
                                    .AddRange(this.address.MapToIPv6().GetAddressBytes())
                                    .AddRange(BitConverter.GetBytes(this.port).LittleEndianToOrFromBitConverterEndianness().Reverse());
            }
        }
    }
}
