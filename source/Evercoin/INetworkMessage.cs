using System;
using System.Collections.Immutable;

namespace Evercoin
{
    /// <summary>
    /// Represents a message received or sent over the network.
    /// </summary>
    public interface INetworkMessage
    {
        /// <summary>
        /// Gets the network parameters for this message.
        /// </summary>
        INetworkParameters NetworkParameters { get; }

        /// <summary>
        /// Gets the ID of the remote client sending or receiving this message.
        /// </summary>
        Guid RemoteClient { get; }

        /// <summary>
        /// Gets the command of this message.
        /// </summary>
        ImmutableList<byte> CommandBytes { get; }

        /// <summary>
        /// Gets the payload of this message.
        /// </summary>
        ImmutableList<byte> Payload { get; }

        /// <summary>
        /// Gets the full content of this message.
        /// </summary>
        /// <remarks>
        /// Usually, this will be:
        /// 1. Static prefix
        /// 2. Command
        /// 3. Payload length
        /// 4. Payload checksum (first N bytes)
        /// 5. Payload
        /// where the size of each field is dictated
        /// by <see cref="NetworkParameters"/>.
        /// </remarks>
        ImmutableList<byte> FullData { get; }
    }
}
