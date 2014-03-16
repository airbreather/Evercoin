﻿using System;
using System.Numerics;
using System.Runtime.Caching;

using Evercoin.Util;

namespace Evercoin.App
{
    internal sealed class TransactionCache
    {
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
            this.underlyingCache.Add(cacheKey, transaction, new CacheItemPolicy());
        }

        private static string CreateCacheKey(BigInteger identifier)
        {
            byte[] littleEndianUInt256Array = identifier.ToLittleEndianUInt256Array();
            Array.Reverse(littleEndianUInt256Array);
            string canonicalTxIdentifier = ByteTwiddling.ByteArrayToHexString(littleEndianUInt256Array);
            return "T;" + canonicalTxIdentifier;
        }
    }
}