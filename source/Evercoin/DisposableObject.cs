using System;
using System.Diagnostics;

namespace Evercoin
{
    // Yeah, this doesn't implement the "standard" IDisposable pattern.
    // Seriously, an "also do another thing" bool parameter?
    // Not on my watch.
    public abstract class DisposableObject : IDisposable
    {
        protected DisposableObject()
        {
            this.DisposeLock = new object();
        }

        ~DisposableObject()
        {
            Debug.Assert(this.IsDisposed, "Freeing an IDisposable object without disposing it first.");
            this.DisposeUnmanagedResources();
        }

        public bool IsDisposed { get; private set; }

        protected object DisposeLock { get; private set; }

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            lock (this.DisposeLock)
            {
                // Check IsDisposed again -- another thread may have already disposed it.
                if (this.IsDisposed)
                {
                    return;
                }

                this.DisposeManagedResources();
                this.DisposeUnmanagedResources();
                this.IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void DisposeManagedResources()
        {
        }

        protected virtual void DisposeUnmanagedResources()
        {
        }

        protected void ThrowIfDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }
}
