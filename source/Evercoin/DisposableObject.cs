using System;
using System.Diagnostics;
using System.Globalization;

namespace Evercoin
{
    /// <summary>
    /// A base for classes that want to implement <see cref="IDisposable"/>
    /// without having to jump through hoops.
    /// </summary>
    public abstract class DisposableObject : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableObject"/> class.
        /// </summary>
        protected DisposableObject()
        {
            this.DisposeLock = new object();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DisposableObject"/> class.
        /// </summary>
        ~DisposableObject()
        {
            Debug.Assert(this.IsDisposed, String.Format(CultureInfo.InvariantCulture, "Freeing an IDisposable object without disposing it first: {0}.", this.GetType().Name));
            this.DisposeUnmanagedResources();
        }

        /// <summary>
        /// Gets a value indicating whether this object is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets an object to lock on for synchronizing calls to Dispose.
        /// </summary>
        protected object DisposeLock { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
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

        /// <summary>
        /// When overridden in a derived class, releases managed resources that
        /// implement the <see cref="IDisposable"/> interface.
        /// </summary>
        protected virtual void DisposeManagedResources()
        {
        }

        /// <summary>
        /// When overridden in a derived class, releases unmanaged resources.
        /// </summary>
        protected virtual void DisposeUnmanagedResources()
        {
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> if this is disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }
}
