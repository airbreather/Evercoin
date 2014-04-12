using System;
using System.Runtime.InteropServices;

namespace LevelDb
{
    public sealed class Comparator : DisposableObject, IComparator
    {
        private readonly IntPtr handle;

        internal Comparator(IComparator copyFrom)
        {
            IntPtr state = Marshal.AllocHGlobal(0);
            this.handle = NativeMethods.leveldb_comparator_create(state, Marshal.FreeHGlobal, delegate { return 0; }, delegate { return "3"; });
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
            NativeMethods.leveldb_comparator_destroy(this.handle);
            base.DisposeUnmanagedResources();
        }
    }
}
