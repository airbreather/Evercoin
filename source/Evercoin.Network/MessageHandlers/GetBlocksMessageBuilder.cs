using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class GetBlocksMessageBuilder
    {
        private const string GetBlocksText = "getblocks";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly INetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public GetBlocksMessageBuilder(INetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(GetBlocksText))
            {
                throw new ArgumentException("Command length is too short for the \"getblocks\" command.", "network");
            }

            this.network = network;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildGetDataMessage(Guid clientId,
                                                   IEnumerable<BigInteger> knownHashes,
                                                   BigInteger lastKnownHash)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, clientId);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(GetBlocksText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            uint protocolVersion = (uint)this.network.Parameters.ProtocolVersion;
            BigInteger[] knownHashList = knownHashes.GetArray();
            ProtocolCompactSize knownHashCount = (ulong)knownHashList.Length;

            byte[] protocolVersionBytes = BitConverter.GetBytes(protocolVersion).LittleEndianToOrFromBitConverterEndianness();
            byte[] knownHashCountBytes = knownHashCount.Data;
            IEnumerable<byte[]> knownHashByteSources = knownHashList.Select(x => x.ToLittleEndianUInt256Array());
            byte[] lastKnownHashBytes = lastKnownHash.ToLittleEndianUInt256Array();

            byte[] knownHashBytes = ByteTwiddling.ConcatenateData(knownHashByteSources);

            byte[] payload = ByteTwiddling.ConcatenateData(protocolVersionBytes, knownHashCountBytes, knownHashBytes, lastKnownHashBytes);
            message.CreateFrom(commandBytes, payload);
            return message;
        }
    }
}
