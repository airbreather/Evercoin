using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Net;

using Evercoin.Algorithms;

namespace Evercoin.App
{
    [Export(typeof(INetworkParameters))]
    internal sealed class SomeNetworkParams : INetworkParameters
    {
        private readonly HashSet<DnsEndPoint> seeds = new HashSet<DnsEndPoint>();

        /// <summary>
        /// Gets the version of the protocol being used here.
        /// </summary>
        public int ProtocolVersion { get { return 70001; } }

        /// <summary>
        /// Gets the protocol version before it is negotiated.
        /// </summary>
        /// <remarks>
        /// For Bitcoin, this is 209 except for really old clients.
        /// This affects the version message.
        /// </remarks>
        public int ProtocolVersionBeforeNegotiation { get { return 209; } }

        /// <summary>
        /// Gets the <see cref="IHashAlgorithm"/> used to verify that the
        /// payload content was received successfully.
        /// </summary>
        public IHashAlgorithm PayloadChecksumAlgorithm { get { return new BuiltinHashAlgorithmStore().GetHashAlgorithm(HashAlgorithmIdentifiers.DoubleSHA256); } }

        /// <summary>
        /// Gets the number of bytes from the head of
        /// <see cref="INetworkParameters.PayloadChecksumAlgorithm"/>'s result to add to the
        /// message header.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 4 for everything that exists today.
        /// </remarks>
        public int PayloadChecksumLengthInBytes { get { return 4; } }

        /// <summary>
        /// Gets the number of bytes to include as the command.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 12 for everything that exists today.
        /// </remarks>
        public int CommandLengthInBytes { get { return 12; } }

        /// <summary>
        /// Gets the number of bytes that need to be read for each message
        /// in order to determine what business logic to apply.
        /// </summary>
        /// <remarks>
        /// Looks like this is going to be 16 for everything that exists today:
        /// 4 bytes for the <see cref="INetworkParameters.StaticMessagePrefixData"/>.
        /// Remaining 12 bytes are the ASCII-encoded command.
        /// </remarks>
        public int MessagePrefixLengthInBytes { get { return 16; } }

        /// <summary>
        /// Gets the static data that starts every message for this network.
        /// Must be shorter than <see cref="INetworkParameters.MessagePrefixLengthInBytes"/>.
        /// </summary>
        /// <remarks>
        /// This is usually a sequence of 4 bytes that are uncommon
        /// in typical data streams.
        /// </remarks>
        public ImmutableList<byte> StaticMessagePrefixData { get { return ImmutableList.Create<byte>(0xF9, 0xBE, 0xB4, 0xD9); } }

        /// <summary>
        /// A set of <see cref="DnsEndPoint"/> objects that may be used
        /// to seed the network.
        /// </summary>
        /// <remarks>
        /// It is expected that the network's protocol provides messages
        /// that allow a node to request which other 
        /// </remarks>
        public ISet<DnsEndPoint> Seeds { get { return this.seeds; } }

        /// <summary>
        /// Determines whether or not a handler for our parameters can handle
        /// messages for another <see cref="INetworkParameters"/> object.
        /// </summary>
        /// <param name="other">
        /// The other <see cref="INetworkParameters"/>.
        /// </param>
        /// <returns>
        /// A value indicating whether this is compatible
        /// with <paramref name="other"/>.
        /// </returns>
        public bool IsCompatibleWith(INetworkParameters other)
        {
            return true;
        }
    }
}
