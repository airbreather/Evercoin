using System;

namespace LevelDb
{
    public sealed class LRUCache : DisposableObject, ILRUCache
    {
        private readonly ulong capacityInBytes;

        private readonly IntPtr handle;

        internal LRUCache(ulong capacityInBytes)
        {
            this.capacityInBytes = capacityInBytes;
            this.handle = NativeMethods.leveldb_cache_create_lru(this.capacityInBytes);
        }

        internal LRUCache(ILRUCache copyFrom)
        {
            this.capacityInBytes = copyFrom.CapacityInBytes;
            this.handle = NativeMethods.leveldb_cache_create_lru(this.capacityInBytes);
        }

        internal IntPtr Handle
        {
            get
            {
                this.ThrowIfDisposed();
                return this.handle;
            }
        }

        public ulong CapacityInBytes { get { return this.capacityInBytes; } }

        protected override void DisposeUnmanagedResources()
        {
            NativeMethods.leveldb_cache_destroy(this.handle);
            base.DisposeUnmanagedResources();
        }
    }
}
