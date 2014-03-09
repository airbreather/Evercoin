using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;

using Evercoin.Util;

namespace Evercoin.App
{
    [Export(typeof(INetworkParameters))]
    internal sealed class SomeNetworkParams : INetworkParameters
    {
        private readonly ImmutableHashSet<DnsEndPoint> seeds = ImmutableHashSet<DnsEndPoint>.Empty;

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
        public Guid PayloadChecksumAlgorithmIdentifier { get { return HashAlgorithmIdentifiers.DoubleSHA256; } }

        /// <summary>
        /// Gets the number of bytes from the head of
        /// <see cref="PayloadChecksumAlgorithmIdentifier"/>'s result to add to
        /// the message header.
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
        public ImmutableHashSet<DnsEndPoint> Seeds { get { return this.seeds; } }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(INetworkParameters other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null &&
                   this.ProtocolVersion == other.ProtocolVersion &&
                   this.ProtocolVersionBeforeNegotiation == other.ProtocolVersionBeforeNegotiation &&
                   this.PayloadChecksumAlgorithmIdentifier == other.PayloadChecksumAlgorithmIdentifier &&
                   this.PayloadChecksumLengthInBytes == other.PayloadChecksumLengthInBytes &&
                   this.CommandLengthInBytes == other.CommandLengthInBytes &&
                   this.MessagePrefixLengthInBytes == other.MessagePrefixLengthInBytes &&
                   this.StaticMessagePrefixData.SequenceEqual(other.StaticMessagePrefixData) &&
                   this.Seeds.SetEquals(other.Seeds);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as INetworkParameters);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            HashCodeBuilder builder = new HashCodeBuilder()
                .HashWith(this.ProtocolVersion)
                .HashWith(this.ProtocolVersionBeforeNegotiation)
                .HashWith(this.PayloadChecksumAlgorithmIdentifier)
                .HashWith(this.PayloadChecksumLengthInBytes)
                .HashWith(this.CommandLengthInBytes);
            builder = this.StaticMessagePrefixData.Aggregate(builder, (prevBuilder, nextByte) => prevBuilder.HashWith(nextByte));
            return this.Seeds.Aggregate(builder, (prevBuilder, nextSeed) => prevBuilder.HashWith(nextSeed));
        }
    }
}
