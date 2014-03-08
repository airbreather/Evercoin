using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Evercoin.ProtocolObjects;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class GetDataMessageBuilder
    {
        private const string GetDataText = "getdata";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly Network network;

        public GetDataMessageBuilder(INetwork network)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(GetDataText))
            {
                throw new ArgumentException("Command length is too short for the \"getdata\" command.", "network");
            }

            Network realNetwork = network as Network;
            if (realNetwork == null)
            {
                throw new NotSupportedException("Other things not supported yet because lol");
            }

            this.network = realNetwork;
        }

        public INetworkMessage BuildGetDataMessage(Guid clientId,
                                                   IEnumerable<ProtocolInventoryVector> dataToRequest)
        {
            Message message = new Message(this.network.Parameters, clientId);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(GetDataText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            ImmutableList<ProtocolInventoryVector> dataToRequestList = dataToRequest.ToImmutableList();
            ProtocolCompactSize size = (ulong)dataToRequestList.Count;

            message.CreateFrom(commandBytes, ImmutableList.CreateRange(size.Data)
                                                          .AddRange(dataToRequestList.SelectMany(x => x.Data)));
            return message;
        }
    }
}
