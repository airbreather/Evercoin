using System;
using System.Numerics;
using System.Runtime.Caching;

using Evercoin.Util;

namespace Evercoin.App
{
    internal sealed class BlockCache
    {
        private readonly MemoryCache underlyingCache;

        public BlockCache(MemoryCache underlyingCache)
        {
            this.underlyingCache = underlyingCache;
        }

        public bool ContainsBlock(BigInteger blockIdentifier)
        {
            string cacheKey = CreateCacheKey(blockIdentifier);
            return this.underlyingCache.Contains(cacheKey);
        }

        public bool TryGetBlock(BigInteger blockIdentifier, out IBlock foundBlock)
        {
            string cacheKey = CreateCacheKey(blockIdentifier);
            foundBlock = this.underlyingCache.Get(cacheKey) as IBlock;
            return foundBlock != null;
        }

        public void PutBlock(BigInteger blockIdentifier, IBlock block)
        {
            string cacheKey = CreateCacheKey(blockIdentifier);
            this.underlyingCache.Add(cacheKey, block, new CacheItemPolicy());
        }

        private static string CreateCacheKey(BigInteger identifier)
        {
            byte[] littleEndianUInt256Array = identifier.ToLittleEndianUInt256Array();
            Array.Reverse(littleEndianUInt256Array);
            string canonicalBlockIdentifier = ByteTwiddling.ByteArrayToHexString(littleEndianUInt256Array);
            return "B;" + canonicalBlockIdentifier;
        }
    }
}