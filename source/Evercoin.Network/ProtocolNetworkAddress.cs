using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin.Network
{
    public sealed class ProtocolNetworkAddress
    {
        private readonly uint? time;

        public ProtocolNetworkAddress(uint? time, ulong services, IPAddress address, ushort port)
        {
            this.time = time;
            this.Services = services;
            this.Address = address;
            this.Port = port;
        }

        public uint Time { get { return this.time.GetValueOrDefault(); }}

        public ulong Services { get; private set; }

        public IPAddress Address { get; private set; }

        public ushort Port { get; private set; }

        public ImmutableList<byte> Data
        {
            get
            {
                byte[] timeBytes = this.time.HasValue ?
                                   BitConverter.GetBytes(this.Time).LittleEndianToOrFromBitConverterEndianness() :
                                   new byte[0];

                return ImmutableList.CreateRange(timeBytes)
                                    .AddRange(BitConverter.GetBytes(this.Services).LittleEndianToOrFromBitConverterEndianness())
                                    .AddRange(this.Address.MapToIPv6().GetAddressBytes())
                                    .AddRange(BitConverter.GetBytes(this.Port).LittleEndianToOrFromBitConverterEndianness().Reverse());
            }
        }
    }
}
