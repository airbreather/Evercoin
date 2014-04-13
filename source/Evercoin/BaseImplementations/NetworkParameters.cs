using System;
using System.Collections.Generic;
using System.Net;

namespace Evercoin.BaseImplementations
{
    public sealed class NetworkParameters : INetworkParameters
    {
        private readonly int protocolVersion;

        private readonly int protocolVersionBeforeNegotiation;

        private readonly Guid payloadChecksumAlgorithmIdentifier;

        private readonly int payloadChecksumLengthInBytes;

        private readonly int commandLengthInBytes;

        private readonly int messagePrefixLengthInBytes;

        private readonly FancyByteArray staticMessagePrefixData;

        private readonly HashSet<DnsEndPoint> seeds;

        public NetworkParameters(int protocolVersion, int protocolVersionBeforeNegotiation, Guid payloadChecksumAlgorithmIdentifier, int payloadChecksumLengthInBytes, int commandLengthInBytes, int messagePrefixLengthInBytes, FancyByteArray staticMessagePrefixData, IEnumerable<DnsEndPoint> seeds)
        {
            this.protocolVersion = protocolVersion;
            this.protocolVersionBeforeNegotiation = protocolVersionBeforeNegotiation;
            this.payloadChecksumAlgorithmIdentifier = payloadChecksumAlgorithmIdentifier;
            this.payloadChecksumLengthInBytes = payloadChecksumLengthInBytes;
            this.commandLengthInBytes = commandLengthInBytes;
            this.messagePrefixLengthInBytes = messagePrefixLengthInBytes;
            this.staticMessagePrefixData = staticMessagePrefixData;
            this.seeds = new HashSet<DnsEndPoint>(seeds);
        }

        /// <summary>
        /// Gets the version of the protocol being used here.
        /// </summary>
        public int ProtocolVersion { get { return this.protocolVersion; } }

        /// <summary>
        /// Gets the protocol version before it is negotiated.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is 209 except for really old clients.
        /// This affects the version message, because network addresses for
        /// Bitcoin include a "timestamp" field in protocol versions newer
        /// than this version.
        /// </remarks>
        public int ProtocolVersionBeforeNegotiation { get { return this.protocolVersionBeforeNegotiation; } }

        /// <summary>
        /// Gets the identifier of the <see cref="IHashAlgorithm"/> used to
        /// verify that the payload content was received successfully.
        /// </summary>
        public Guid PayloadChecksumAlgorithmIdentifier { get { return this.payloadChecksumAlgorithmIdentifier; } }

        /// <summary>
        /// Gets the number of bytes from the head of
        /// <see cref="INetworkParameters.PayloadChecksumAlgorithmIdentifier"/>'s result to add to
        /// the message header.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 4 for everything that exists today.
        /// </remarks>
        public int PayloadChecksumLengthInBytes { get { return this.payloadChecksumLengthInBytes; } }

        /// <summary>
        /// Gets the number of bytes to include as the command.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 12 for everything that exists today.
        /// </remarks>
        public int CommandLengthInBytes { get { return this.commandLengthInBytes; } }

        /// <summary>
        /// Gets the number of bytes that need to be read for each message
        /// in order to determine what business logic to apply.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 16 for everything that exists today:
        /// 4 bytes for the <see cref="INetworkParameters.StaticMessagePrefixData"/>.
        /// 12 bytes for <see cref="INetworkParameters.CommandLengthInBytes"/>.
        /// </remarks>
        public int MessagePrefixLengthInBytes { get { return this.messagePrefixLengthInBytes; } }

        /// <summary>
        /// Gets the static data that starts every message for this network.
        /// Must be shorter than <see cref="INetworkParameters.MessagePrefixLengthInBytes"/>.
        /// </summary>
        /// <remarks>
        /// This is usually a sequence of 4 bytes that are uncommon
        /// in typical data streams.
        /// </remarks>
        public FancyByteArray StaticMessagePrefixData { get { return this.staticMessagePrefixData; } }

        /// <summary>
        /// Gets a set of <see cref="DnsEndPoint"/> objects that may be used
        /// to seed the network.
        /// </summary>
        /// <remarks>
        /// It is expected that the network's protocol provides messages
        /// that allow a node to request which other 
        /// </remarks>
        public ISet<DnsEndPoint> Seeds { get { return this.seeds; } }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(INetworkParameters other)
        {
            throw new NotImplementedException();
        }
    }
}
