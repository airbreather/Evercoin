using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

namespace Evercoin.Network.MessageBuilders
{
    internal sealed class GetBlocksMessageBuilder
    {
        private const string GetBlocksText = "getblocks";
        private const string GetHeadersText = "getheaders";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly IRawNetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public GetBlocksMessageBuilder(IRawNetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(GetBlocksText))
            {
                throw new ArgumentException("Command length is too short for the \"getblocks\" command.", "network");
            }

            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(GetHeadersText))
            {
                throw new ArgumentException("Command length is too short for the \"getheaders\" command.", "network");
            }

            this.network = network;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildGetBlocksMessage(INetworkPeer peer,
                                                     IEnumerable<FancyByteArray> knownHashes,
                                                     FancyByteArray lastKnownHash,
                                                     BlockRequestType requestType)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, peer);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes;
            switch (requestType)
            {
                case BlockRequestType.HeadersOnly:
                    unpaddedCommandBytes = CommandEncoding.GetBytes(GetHeadersText);
                    break;

                default:
                    unpaddedCommandBytes = CommandEncoding.GetBytes(GetBlocksText);
                    break;
            }

            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            uint protocolVersion = (uint)this.network.Parameters.ProtocolVersion;
            FancyByteArray[] knownHashList = knownHashes.GetArray();
            ProtocolCompactSize knownHashCount = (ulong)knownHashList.Length;

            byte[] protocolVersionBytes = BitConverter.GetBytes(protocolVersion).LittleEndianToOrFromBitConverterEndianness();
            byte[] knownHashCountBytes = knownHashCount.Data;
            IEnumerable<byte[]> knownHashByteSources = knownHashList.Select(x => FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(x, 32, Endianness.LittleEndian).Value);
            byte[] lastKnownHashBytes = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(lastKnownHash, 32, Endianness.LittleEndian);

            byte[] knownHashBytes = ByteTwiddling.ConcatenateData(knownHashByteSources);

            byte[] payload = ByteTwiddling.ConcatenateData(protocolVersionBytes, knownHashCountBytes, knownHashBytes, lastKnownHashBytes);
            message.CreateFrom(commandBytes, payload);
            return message;
        }
    }
}
