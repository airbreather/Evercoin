using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Evercoin.ProtocolObjects;
using Evercoin.Util;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class GetDataMessageBuilder
    {
        private const string GetDataText = "getdata";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly IRawNetwork network;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        public GetDataMessageBuilder(IRawNetwork network, IHashAlgorithmStore hashAlgorithmStore)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(GetDataText))
            {
                throw new ArgumentException("Command length is too short for the \"getdata\" command.", "network");
            }

            this.network = network;
            this.hashAlgorithmStore = hashAlgorithmStore;
        }

        public INetworkMessage BuildGetDataMessage(Guid clientId,
                                                   IEnumerable<ProtocolInventoryVector> dataToRequest)
        {
            Message message = new Message(this.network.Parameters, this.hashAlgorithmStore, clientId);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(GetDataText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            ProtocolInventoryVector[] dataToRequestList = dataToRequest.GetArray();
            ProtocolCompactSize size = (ulong)dataToRequestList.Length;

            byte[] sizeBytes = size.Data;
            IEnumerable<byte[]> dataToRequestSources = dataToRequestList.Select(x => x.Data);

            byte[] dataToRequestBytes = ByteTwiddling.ConcatenateData(dataToRequestSources);
            byte[] payload = ByteTwiddling.ConcatenateData(sizeBytes, dataToRequestBytes);

            message.CreateFrom(commandBytes, payload);
            return message;
        }
    }
}
