using System;
using System.Collections.Generic;

namespace Evercoin
{
    /// <summary>
    /// Encapsulates all the different parameters that define a cryptocurrency.
    /// </summary>
    public interface ICurrencyParameters
    {
        /// <summary>
        /// Gets a <see cref="Guid"/> that uniquely identifies this currency.
        /// </summary>
        Guid Identifier { get; }

        /// <summary>
        /// Gets the friendly name, e.g., "Bitcoin".
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> that define the
        /// communication protocol for the network.
        /// </summary>
        /// <remarks>
        /// For example, the magic number, seeds, size of each message part,
        /// message checksum hash algorithm identifier, etc.
        /// </remarks>
        INetworkParameters NetworkParameters { get; }

        /// <summary>
        /// Gets the <see cref="IChainParameters"/> that lay out certain
        /// chain-specific rules.
        /// </summary>
        /// <remarks>
        /// For example, the block hash algorithm identifier, the number of
        /// blocks at each difficulty target, the maximum difficulty target,
        /// the desired amount of time between blocks, etc.
        /// </remarks>
        IChainParameters ChainParameters { get; }

        /// <summary>
        /// Gets the <see cref="IHashAlgorithmStore"/> that contains the hash
        /// algorithms to use for this currency.
        /// </summary>
        IHashAlgorithmStore HashAlgorithmStore { get; }

        IChainSerializer ChainSerializer { get; }

        IChainValidator ChainValidator { get; }
    }
}
