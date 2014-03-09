using System;

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
        /// For example, the magic number and seeds.
        /// </remarks>
        INetworkParameters NetworkParameters { get; }

        /// <summary>
        /// Gets the <see cref="IChainParameters"/> that lay out certain
        /// chain-specific rules.
        /// </summary>
        IChainParameters ChainParameters { get; }

        /// <summary>
        /// Gets the <see cref="IHashAlgorithmStore"/> that contains the hash
        /// algorithms to use for this currency.
        /// </summary>
        IHashAlgorithmStore HashAlgorithmStore { get; }
    }
}
