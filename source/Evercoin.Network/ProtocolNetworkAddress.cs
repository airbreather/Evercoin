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
        public ProtocolNetworkAddress()
        {
        }

        public ProtocolNetworkAddress(uint time, ulong services, IPAddress address, ushort port)
        {
            this.Time = time;
            this.Services = services;
            this.Address = address;
            this.Port = port;
        }

        public uint Time { get; private set; }

        public ulong Services { get; private set; }

        public IPAddress Address { get; private set; }

        public ushort Port { get; private set; }

        public ImmutableList<byte> GetData(int protocolVersion)
        {
            byte[] timeBytes = protocolVersion >= 31402 ?
                               BitConverter.GetBytes(this.Time).LittleEndianToOrFromBitConverterEndianness() :
                               new byte[0];

            return ImmutableList.CreateRange(timeBytes)
                                .AddRange(BitConverter.GetBytes(this.Services).LittleEndianToOrFromBitConverterEndianness())
                                .AddRange(this.Address.MapToIPv6().GetAddressBytes())
                                .AddRange(BitConverter.GetBytes(this.Port).LittleEndianToOrFromBitConverterEndianness().Reverse());
        }

        public async Task LoadFromStreamAsync(Stream stream, int protocolVersion, CancellationToken ct)
        {
            if (protocolVersion >= 31402)
            {
                var timeBytes = (await stream.ReadBytesAsyncWithIntParam(4, ct)).ToArray().LittleEndianToOrFromBitConverterEndianness();
                this.Time = BitConverter.ToUInt32(timeBytes, 0);
            }
            
            var servicesBytes = (await stream.ReadBytesAsyncWithIntParam(8, ct)).ToArray().LittleEndianToOrFromBitConverterEndianness();
            this.Services = BitConverter.ToUInt64(servicesBytes, 0);
            
            var addressBytes = (await stream.ReadBytesAsyncWithIntParam(16, ct)).ToArray();
            this.Address = new IPAddress(addressBytes);
            
            var portBytes = (await stream.ReadBytesAsyncWithIntParam(2, ct)).ToArray().LittleEndianToOrFromBitConverterEndianness().Reverse().ToArray();
            this.Port = BitConverter.ToUInt16(portBytes, 0);
        }
    }
}
