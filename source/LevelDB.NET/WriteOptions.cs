using System;

namespace LevelDb
{
    public sealed class WriteOptions : DisposableObject, IWriteOptions
    {
        private readonly IntPtr handle;

        internal WriteOptions()
        {
            this.handle = NativeMethods.leveldb_writeoptions_create();
        }

        internal WriteOptions(IWriteOptions copyFrom)
            : this()
        {
        }

        internal IntPtr Handle
        {
            get
            {
                this.ThrowIfDisposed();
                return this.handle;
            }
        }

        protected override void DisposeUnmanagedResources()
        {
            NativeMethods.leveldb_writeoptions_destroy(this.handle);
            base.DisposeUnmanagedResources();
        }
    }
}
