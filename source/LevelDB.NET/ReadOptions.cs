using System;

namespace LevelDb
{
    public sealed class ReadOptions : DisposableObject, IReadOptions
    {
        private readonly IntPtr underlyingValue;

        private bool verifyChecksums;

        private bool cacheResults;

        private ISnapshot overriddenSnapshot;

        internal ReadOptions()
        {
            this.underlyingValue = NativeMethods.leveldb_readoptions_create();
        }

        internal ReadOptions(IReadOptions copyFrom)
            : this()
        {
        }

        internal IntPtr UnderlyingValue
        {
            get
            {
                this.ThrowIfDisposed();
                return this.underlyingValue;
            }
        }

        protected override void DisposeUnmanagedResources()
        {
            NativeMethods.leveldb_readoptions_destroy(this.underlyingValue);
            base.DisposeUnmanagedResources();
        }

        public bool VerifyChecksums
        {
            get { return this.verifyChecksums; }
        }

        public bool CacheResults
        {
            get { return this.cacheResults; }
        }

        public ISnapshot OverriddenSnapshot
        {
            get { return this.overriddenSnapshot; }
            set { this.overriddenSnapshot = value; }
        }
    }
}
