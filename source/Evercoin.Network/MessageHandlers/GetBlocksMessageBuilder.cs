using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;

using Evercoin.ProtocolObjects;

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
            ImmutableList<BigInteger> knownHashList = knownHashes.ToImmutableList();
            ProtocolCompactSize knownHashCount = (ulong)knownHashList.Count;

            ImmutableList<byte> payload = ImmutableList.CreateRange(BitConverter.GetBytes(protocolVersion).LittleEndianToOrFromBitConverterEndianness())
                                                       .AddRange(knownHashCount.Data);
            payload = knownHashList.Aggregate(payload, (prevPayload, nextHash) => prevPayload.AddRange(nextHash.ToLittleEndianUInt256Array()));

            payload = payload.AddRange(lastKnownHash.ToLittleEndianUInt256Array());

            message.CreateFrom(commandBytes, payload);
            return message;
        }
    }
}
