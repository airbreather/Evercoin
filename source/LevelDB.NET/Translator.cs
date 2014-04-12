using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LevelDb
{
    internal static class Translator
    {
        public static DatabaseOptions GetDatabaseOptions(IDatabaseOptions databaseOptions)
        {
            DatabaseOptions typedDatabaseOptions = databaseOptions as DatabaseOptions;
            if (typedDatabaseOptions != null && !typedDatabaseOptions.IsDisposed)
            {
                return typedDatabaseOptions;
            }

            return new DatabaseOptions(databaseOptions);
        }

        public static LRUCache GetLRUCache(ILRUCache lruCache)
        {
            LRUCache typedLRUCache = lruCache as LRUCache;
            if (typedLRUCache != null && !typedLRUCache.IsDisposed)
            {
                return typedLRUCache;
            }

            return new LRUCache(lruCache);
        }

        public static Comparator GetComparator(IComparator comparator)
        {
            Comparator typedComparator = comparator as Comparator;
            if (typedComparator != null && !typedComparator.IsDisposed)
            {
                return typedComparator;
            }

            return new Comparator(comparator);
        }

        public static ReadOptions GetReadOptions(IReadOptions readOptions)
        {
            ReadOptions typedReadOptions = readOptions as ReadOptions;
            if (typedReadOptions != null && !typedReadOptions.IsDisposed)
            {
                return typedReadOptions;
            }

            return new ReadOptions(readOptions);
        }

        public static WriteOptions GetWriteOptions(IWriteOptions writeOptions)
        {
            WriteOptions typedWriteOptions = writeOptions as WriteOptions;
            if (typedWriteOptions != null && !typedWriteOptions.IsDisposed)
            {
                return typedWriteOptions;
            }

            return new WriteOptions(writeOptions);
        }

        public static FilterPolicy GetFilterPolicy(IFilterPolicy filterPolicy)
        {
            FilterPolicy typedWriteOptions = filterPolicy as FilterPolicy;
            if (typedWriteOptions != null && !typedWriteOptions.IsDisposed)
            {
                return typedWriteOptions;
            }

            return new FilterPolicy(filterPolicy);
        }
    }
}
