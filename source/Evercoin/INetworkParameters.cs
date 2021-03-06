using System;
using System.Collections.Generic;
using System.Net;

namespace Evercoin
{
    /// <summary>
    /// Represents the parameters for the messages on a cryptocurrency network.
    /// </summary>
    public interface INetworkParameters : IEquatable<INetworkParameters>
    {
        /// <summary>
        /// Gets the version of the protocol being used here.
        /// </summary>
        int ProtocolVersion { get; }

        /// <summary>
        /// Gets the protocol version before it is negotiated.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is 209 except for really old clients.
        /// This affects the version message, because network addresses for
        /// Bitcoin include a "timestamp" field in protocol versions newer
        /// than this version.
        /// </remarks>
        int ProtocolVersionBeforeNegotiation { get; }

        /// <summary>
        /// Gets the identifier of the <see cref="IHashAlgorithm"/> used to
        /// verify that the payload content was received successfully.
        /// </summary>
        Guid PayloadChecksumAlgorithmIdentifier { get; }

        /// <summary>
        /// Gets the number of bytes from the head of
        /// <see cref="PayloadChecksumAlgorithmIdentifier"/>'s result to add to
        /// the message header.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 4 for everything that exists today.
        /// </remarks>
        int PayloadChecksumLengthInBytes { get; }

        /// <summary>
        /// Gets the number of bytes to include as the command.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 12 for everything that exists today.
        /// </remarks>
        int CommandLengthInBytes { get; }

        /// <summary>
        /// Gets the number of bytes that need to be read for each message
        /// in order to determine what business logic to apply.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 16 for everything that exists today:
        /// 4 bytes for the <see cref="StaticMessagePrefixData"/>.
        /// 12 bytes for <see cref="CommandLengthInBytes"/>.
        /// </remarks>
        int MessagePrefixLengthInBytes { get; }

        /// <summary>
        /// Gets the static data that starts every message for this network.
        /// Must be shorter than <see cref="MessagePrefixLengthInBytes"/>.
        /// </summary>
        /// <remarks>
        /// This is usually a sequence of 4 bytes that are uncommon
        /// in typical data streams.
        /// </remarks>
        FancyByteArray StaticMessagePrefixData { get; }

        /// <summary>
        /// Gets a set of <see cref="DnsEndPoint"/> objects that may be used
        /// to seed the network.
        /// </summary>
        /// <remarks>
        /// It is expected that the network's protocol provides messages
        /// that allow a node to request which other 
        /// </remarks>
        ISet<DnsEndPoint> Seeds { get; }
    }
}
