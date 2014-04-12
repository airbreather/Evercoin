using System;

namespace LevelDb
{
    public sealed class FilterPolicy : DisposableObject, IFilterPolicy
    {
        private readonly IntPtr handle;

        internal FilterPolicy()
        {
        }

        internal FilterPolicy(IFilterPolicy copyFrom)
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
            NativeMethods.leveldb_filterpolicy_destroy(this.handle);
            base.DisposeUnmanagedResources();
        }
    }
}
