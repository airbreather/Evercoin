using System.Runtime.Caching;

namespace Evercoin.Storage
{
    internal sealed class TransactionCache
    {
        private readonly MemoryCache underlyingCache;

        public TransactionCache(MemoryCache underlyingCache)
        {
            this.underlyingCache = underlyingCache;
        }

        public bool ContainsTransaction(string transactionIdentifier)
        {
            string cacheKey = CreateCacheKey(transactionIdentifier);
            return this.underlyingCache.Contains(cacheKey);
        }

        public bool TryGetTransaction(string transactionIdentifier, out ITransaction foundTransaction)
        {
            string cacheKey = CreateCacheKey(transactionIdentifier);
            foundTransaction = this.underlyingCache.Get(cacheKey) as ITransaction;
            return foundTransaction != null;
        }

        public void PutTransaction(ITransaction transaction)
        {
            string cacheKey = CreateCacheKey(transaction.Identifier);
            this.underlyingCache.Add(cacheKey, transaction, new CacheItemPolicy());
        }

        private static string CreateCacheKey(string identifier)
        {
            return "T;" + identifier;
        }
    }
}
