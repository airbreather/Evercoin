using System.Numerics;
using System.Runtime.Caching;

namespace Evercoin.App
{
    internal sealed class TransactionCache
    {
        private static readonly CacheItemPolicy StandardCacheItemPolicy = new CacheItemPolicy();
        private readonly MemoryCache underlyingCache;

        public TransactionCache(MemoryCache underlyingCache)
        {
            this.underlyingCache = underlyingCache;
        }

        public bool ContainsTransaction(BigInteger transactionIdentifier)
        {
            string cacheKey = CreateCacheKey(transactionIdentifier);
            return this.underlyingCache.Contains(cacheKey);
        }

        public bool TryGetTransaction(BigInteger transactionIdentifier, out ITransaction foundTransaction)
        {
            string cacheKey = CreateCacheKey(transactionIdentifier);
            foundTransaction = this.underlyingCache.Get(cacheKey) as ITransaction;
            return foundTransaction != null;
        }

        public void PutTransaction(BigInteger transactionIdentifier, ITransaction transaction)
        {
            string cacheKey = CreateCacheKey(transactionIdentifier);
            this.underlyingCache.Add(cacheKey, transaction, StandardCacheItemPolicy);
        }

        private static string CreateCacheKey(BigInteger identifier)
        {
            FancyByteArray fancyByteArray = FancyByteArray.CreateFromBigIntegerWithDesiredLengthAndEndianness(identifier, 32, Endianness.LittleEndian);
            return "T;" + fancyByteArray;
        }
    }
}