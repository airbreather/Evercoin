using System;
using System.Runtime.Caching;
using System.Timers;

namespace Evercoin.App
{
    internal sealed class Cache<T> : DisposableObject
        where T : class
    {
        private static readonly CacheItemPolicy StandardCacheItemPolicy = new CacheItemPolicy();
        private readonly MemoryCache underlyingCache;

        private readonly long keepHowManyItems;

        private readonly Func<T, FancyByteArray> serializer;

        private readonly Func<FancyByteArray, T> deserializer;

        private readonly Timer trimTimer;

        public Cache(MemoryCache underlyingCache, long keepHowManyItems, Func<T, FancyByteArray> serializer, Func<FancyByteArray, T> deserializer)
        {
            this.underlyingCache = underlyingCache;
            this.keepHowManyItems = keepHowManyItems;
            this.serializer = serializer;
            this.deserializer = deserializer;
        }

        public bool Contains(FancyByteArray blockIdentifier)
        {
            return this.underlyingCache.Contains(blockIdentifier.ToString());
        }

        public bool TryGetValue(FancyByteArray blockIdentifier, out T foundBlock)
        {
            FancyByteArray? bytes = this.underlyingCache.Get(blockIdentifier.ToString()) as FancyByteArray?;
            if (!bytes.HasValue)
            {
                foundBlock = null;
                return false;
            }

            foundBlock = this.deserializer(bytes.Value);
            return true;
        }

        public void Put(FancyByteArray blockIdentifier, T block)
        {
            FancyByteArray bytes = this.serializer(block);
            this.underlyingCache.Add(blockIdentifier.ToString(), bytes, StandardCacheItemPolicy);
            if ((this.keepHowManyItems / 0.8) < this.underlyingCache.GetCount())
            {
                this.underlyingCache.Trim(20);
            }
        }

        protected override void DisposeManagedResources()
        {
            this.trimTimer.Dispose();
        }
    }
}