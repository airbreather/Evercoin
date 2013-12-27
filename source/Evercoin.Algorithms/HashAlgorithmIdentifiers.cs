using System;

namespace Evercoin.Algorithms
{
    /// <summary>
    /// Stores identifiers of well-known <see cref="IHashAlgorithm"/>
    /// objects.  Used for <see cref="IHashAlgorithmStore"/>.
    /// </summary>
    public static class HashAlgorithmIdentifiers
    {
        /// <summary>
        /// Data is run through two rounds of SHA-256.
        /// </summary>
        public static readonly Guid DoubleSHA256 = Guid.Parse("5D8F4412-0CF8-46F3-B15A-7E730F75974A");

        /// <summary>
        /// Data is run through SCrypt, with the same parameters used by Litecoin.
        /// </summary>
        /// <remarks>
        /// Litecoin uses SCrypt parameters (N=1024, r=1, p=1).
        /// The salt we use is just the input data.
        /// The result is 32 bytes (256 bits) in length.
        /// </remarks>
        public static readonly Guid LitecoinSCrypt = Guid.Parse("820A0BAC-0C17-4620-8AE8-4EEBFBB70415");
    }
}
