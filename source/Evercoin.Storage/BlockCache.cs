using System.Runtime.Caching;

namespace Evercoin.Storage
{
    internal sealed class BlockCache
    {
        private readonly MemoryCache underlyingCache;

        public BlockCache(MemoryCache underlyingCache)
        {
            this.underlyingCache = underlyingCache;
        }

        public bool ContainsBlock(string blockIdentifier)
        {
            string cacheKey = CreateCacheKey(blockIdentifier);
            return this.underlyingCache.Contains(cacheKey);
        }

        public bool TryGetBlock(string blockIdentifier, out IBlock foundBlock)
        {
            string cacheKey = CreateCacheKey(blockIdentifier);
            foundBlock = this.underlyingCache.Get(cacheKey) as IBlock;
            return foundBlock != null;
        }

        public void PutBlock(IBlock block)
        {
            string cacheKey = CreateCacheKey(block.Identifier);
            this.underlyingCache.Add(cacheKey, block, new CacheItemPolicy());
        }

        private static string CreateCacheKey(string identifier)
        {
            return "B;" + identifier;
        }
    }
}
