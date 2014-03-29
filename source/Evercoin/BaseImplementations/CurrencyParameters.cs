using System;

namespace Evercoin.BaseImplementations
{
    public sealed class CurrencyParameters : ICurrencyParameters
    {
        private readonly Guid identifier;

        private readonly string friendlyName;

        private readonly INetworkParameters networkParameters;

        private readonly IHashAlgorithmStore hashAlgorithmStore;

        private readonly IChainParameters chainParameters;

        private readonly IChainSerializer chainSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrencyParameters"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The value for <see cref="Identifier"/>.
        /// </param>
        /// <param name="friendlyName">
        /// The value for <see cref="FriendlyName"/>.
        /// </param>
        /// <param name="networkParameters">
        /// The value for <see cref="NetworkParameters"/>.
        /// </param>
        /// <param name="hashAlgorithmStore">
        /// The value for <see cref="HashAlgorithmStore"/>.
        /// </param>
        /// <param name="chainParameters">
        /// The value for <see cref="ChainParameters"/>.
        /// </param>
        /// <param name="chainSerializer">
        /// The value for <see cref="ChainSerializer"/>.
        /// </param>
        public CurrencyParameters(Guid identifier,
                                  string friendlyName,
                                  INetworkParameters networkParameters,
                                  IHashAlgorithmStore hashAlgorithmStore,
                                  IChainParameters chainParameters,
                                  IChainSerializer chainSerializer)
        {
            this.identifier = identifier;
            this.friendlyName = friendlyName;
            this.networkParameters = networkParameters;
            this.hashAlgorithmStore = hashAlgorithmStore;
            this.chainParameters = chainParameters;
            this.chainSerializer = chainSerializer;
        }

        /// <summary>
        /// Gets a <see cref="Guid"/> that uniquely identifies this currency.
        /// </summary>
        public Guid Identifier { get { return this.identifier; } }

        /// <summary>
        /// Gets the friendly name, e.g., "Bitcoin".
        /// </summary>
        public string FriendlyName { get { return this.friendlyName; } }

        /// <summary>
        /// Gets the <see cref="INetworkParameters"/> that define the
        /// communication protocol for the network.
        /// </summary>
        /// <remarks>
        /// For example, the magic number and seeds.
        /// </remarks>
        public INetworkParameters NetworkParameters { get { return this.networkParameters; } }

        /// <summary>
        /// Gets the <see cref="IChainParameters"/> that lay out certain
        /// chain-specific rules.
        /// </summary>
        public IChainParameters ChainParameters { get { return this.chainParameters; } }

        /// <summary>
        /// Gets the <see cref="IHashAlgorithmStore"/> that contains the hash
        /// algorithms to use for this currency.
        /// </summary>
        public IHashAlgorithmStore HashAlgorithmStore { get { return this.hashAlgorithmStore; } }

        public IChainSerializer ChainSerializer { get { return this.chainSerializer; } }
    }
}