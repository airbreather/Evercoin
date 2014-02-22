using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;

namespace Evercoin
{
    /// <summary>
    /// Represents the parameters for the messages on a cryptocurrency network.
    /// </summary>
    public interface INetworkParameters
    {
        /// <summary>
        /// Gets the number of bytes that need to be read for each message
        /// in order to determine what business logic to apply.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 16 for everything that exists today:
        /// 4 bytes for the <see cref="StaticMessagePrefixData"/>.
        /// Remaining 12 bytes are the ASCII-encoded command.
        /// </remarks>
        uint MessagePrefixLengthInBytes { get; }

        /// <summary>
        /// Gets the static data that starts every message for this network.
        /// Must be shorter than <see cref="MessagePrefixLengthInBytes"/>.
        /// </summary>
        /// <remarks>
        /// This is usually a sequence of 4 bytes that are uncommon
        /// in typical data streams.
        /// </remarks>
        IImmutableList<byte> StaticMessagePrefixData { get; }

        /// <summary>
        /// A set of <see cref="DnsEndPoint"/> objects that may be used
        /// to seed the network.
        /// </summary>
        /// <remarks>
        /// It is expected that the network's protocol provides messages
        /// that allow a node to request which other 
        /// </remarks>
        ISet<DnsEndPoint> Seeds { get; }
    }
}
