using System.Numerics;
using System.Runtime.Caching;

namespace Evercoin.App
{
    internal sealed class BlockCache
    {
        private static readonly CacheItemPolicy StandardCacheItemPolicy = new CacheItemPolicy();
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
            this.underlyingCache.Add(cacheKey, block, StandardCacheItemPolicy);
        }

        private static string CreateCacheKey(BigInteger identifier)
        {
            FancyByteArray fancyByteArray = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(identifier, 32, Endianness.LittleEndian);
            return "B;" + fancyByteArray;
        }
    }
}