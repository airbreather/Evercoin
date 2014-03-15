using System;
using System.Net;

using Evercoin.Util;

namespace Evercoin.ProtocolObjects
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

        public byte[] Data
        {
            get
            {
                byte[] timeBytes = this.time.HasValue ?
                                   BitConverter.GetBytes(this.Time).LittleEndianToOrFromBitConverterEndianness() :
                                   new byte[0];

                byte[] servicesBytes = BitConverter.GetBytes(this.Services).LittleEndianToOrFromBitConverterEndianness();

                // BIG-ENDIAN!!
                byte[] addressBytes = this.Address.MapToIPv6().GetAddressBytes();

                byte[] portBytes = BitConverter.GetBytes(this.Port).LittleEndianToOrFromBitConverterEndianness();

                // BIG-ENDIAN!!
                Array.Reverse(portBytes);

                return ByteTwiddling.ConcatenateData(timeBytes, servicesBytes, addressBytes, portBytes);
            }
        }
    }
}
