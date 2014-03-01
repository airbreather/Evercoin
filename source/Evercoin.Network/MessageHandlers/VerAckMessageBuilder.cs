﻿using System;
using System.Collections.Immutable;
using System.Text;

namespace Evercoin.Network.MessageHandlers
{
    internal sealed class VerAckMessageBuilder
    {
        private const string VerAckText = "verack";
        private static readonly Encoding CommandEncoding = Encoding.ASCII;

        private readonly Network network;

        public VerAckMessageBuilder(INetwork network)
        {
            if (network.Parameters.CommandLengthInBytes < CommandEncoding.GetByteCount(VerAckText))
            {
                throw new ArgumentException("Command length is too short for the \"version\" command.", "network");
            }

            Network realNetwork = network as Network;
            if (realNetwork == null)
            {
                throw new NotSupportedException("Other things not supported yet because lol");
            }

            this.network = realNetwork;
        }

        public INetworkMessage BuildVerAckMessage(Guid clientId)
        {
            Message message = new Message(this.network.Parameters, clientId);

            byte[] commandBytes = new byte[this.network.Parameters.CommandLengthInBytes];
            byte[] unpaddedCommandBytes = CommandEncoding.GetBytes(VerAckText);
            Array.Copy(unpaddedCommandBytes, commandBytes, unpaddedCommandBytes.Length);

            message.CreateFrom(commandBytes, ImmutableList<byte>.Empty);
            return message;
        }
    }
}