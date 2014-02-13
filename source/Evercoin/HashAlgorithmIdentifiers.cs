using System;

namespace Evercoin
{
    /// <summary>
    /// Stores identifiers of well-known <see cref="IHashAlgorithm"/>
    /// objects.  Used for <see cref="IHashAlgorithmStore"/>.
    /// </summary>
    public static class HashAlgorithmIdentifiers
    {
        /// <summary>
        /// Data is run through one round of SHA-1.
        /// </summary>
        public static readonly Guid SHA1 = new Guid("F09C918B-CA2E-4C93-B8B0-C358F5531044");

        /// <summary>
        /// Data is run through one round of SHA-256.
        /// </summary>
        public static readonly Guid SHA256 = new Guid("C99B0F6A-C5AA-4AC2-BFCB-946D488553D1");

        /// <summary>
        /// Data is run through one round of RIPEMD-160.
        /// </summary>
        public static readonly Guid RipeMd160 = new Guid("BB5758DE-011F-4981-908E-3E2A524D9A12");

        /// <summary>
        /// Data is run through one round of SHA-256, then one round of RIPEMD-160.
        /// </summary>
        public static readonly Guid SHA256ThenRipeMd160 = new Guid("FF2AB0F9-13E0-4F99-92CB-ED84A6F3D046");

        /// <summary>
        /// Data is run through two rounds of SHA-256.
        /// </summary>
        public static readonly Guid DoubleSHA256 = new Guid("5D8F4412-0CF8-46F3-B15A-7E730F75974A");

        /// <summary>
        /// Data is run through SCrypt, with the same parameters used by Litecoin.
        /// </summary>
        /// <remarks>
        /// Litecoin uses SCrypt parameters (N=1024, r=1, p=1).
        /// The salt we use is just the input data.
        /// The result is 32 bytes (256 bits) in length.
        /// </remarks>
        public static readonly Guid LitecoinSCrypt = new Guid("820A0BAC-0C17-4620-8AE8-4EEBFBB70415");
    }
}
