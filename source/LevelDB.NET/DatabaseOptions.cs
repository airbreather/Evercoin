using System;

namespace LevelDb
{
    public sealed class DatabaseOptions : DisposableObject, IDatabaseOptions
    {
        private readonly IntPtr underlyingHandle;

        // NOTE: this class assumes the ownership of these values once they are set,
        // along with all the responsibilities that come with that.
        private Comparator overriddenComparator;

        private FilterPolicy overriddenFilterPolicy;

        private LRUCache overriddenLruCache;

        // No P/Invoke method exists to let us create loggers.
        ////private Logger overriddenLogger;

        // NOTE: The default values of these fields are just so that someone can check at runtime to see what the value will be.
        // These values are actually defined in the native source code, and if they get changed, then these will be wrong!
        private bool createIfMissing = false;

        private bool throwErrorIfExists = false;

        private bool useParanoidChecks = false;

        private ulong writeBufferSizeInBytes = 4 << 20;

        private int maximumNumberOfOpenFiles = 1000;

        private ulong blockSizeInBytes = 4096;

        private int blockRestartIntervalInNumberOfKeys = 16;

        private CompressionOption compressionOption = CompressionOption.SnappyCompression;

        internal DatabaseOptions()
        {
            this.underlyingHandle = NativeMethods.leveldb_options_create();
        }

        internal DatabaseOptions(IDatabaseOptions copyFrom)
            : this()
        {
            this.OverriddenComparator = copyFrom.OverriddenComparator;
            this.OverriddenFilterPolicy = copyFrom.OverriddenFilterPolicy;
            this.OverriddenLruCache = copyFrom.OverriddenLruCache;
            this.CreateIfMissing = copyFrom.CreateIfMissing;
            this.ThrowErrorIfExists = copyFrom.ThrowErrorIfExists;
            this.UseParanoidChecks = copyFrom.UseParanoidChecks;
            this.WriteBufferSizeInBytes = copyFrom.WriteBufferSizeInBytes;
            this.MaximumNumberOfOpenFiles = copyFrom.MaximumNumberOfOpenFiles;
            this.BlockSizeInBytes = copyFrom.BlockSizeInBytes;
            this.BlockRestartIntervalInNumberOfKeys = copyFrom.BlockRestartIntervalInNumberOfKeys;
            this.CompressionOption = copyFrom.CompressionOption;
        }

        public IComparator OverriddenComparator
        {
            get { return this.overriddenComparator; }
            set
            {
                this.ThrowIfDisposed();
                if (this.overriddenComparator != null)
                {
                    this.overriddenComparator.Dispose();
                }

                if (value != null)
                {
                    this.overriddenComparator = Translator.GetComparator(value);
                    NativeMethods.leveldb_options_set_comparator(this.Handle, this.overriddenComparator.Handle);
                }
                else
                {
                    this.overriddenComparator = null;
                    NativeMethods.leveldb_options_set_comparator(this.Handle, IntPtr.Zero);
                }
            }
        }

        public IFilterPolicy OverriddenFilterPolicy
        {
            get { return this.overriddenFilterPolicy; }
            set
            {
                this.ThrowIfDisposed();
                if (this.overriddenFilterPolicy != null)
                {
                    this.overriddenFilterPolicy.Dispose();
                }

                if (value != null)
                {
                    this.overriddenFilterPolicy = Translator.GetFilterPolicy(value);
                    NativeMethods.leveldb_options_set_filter_policy(this.Handle,this.overriddenFilterPolicy.Handle);
                }
                else
                {
                    this.overriddenFilterPolicy = null;
                    NativeMethods.leveldb_options_set_filter_policy(this.Handle, IntPtr.Zero);
                }
            }
        }

        public ILRUCache OverriddenLruCache
        {
            get { return this.overriddenLruCache; }
            set
            {
                this.ThrowIfDisposed();
                if (this.overriddenLruCache != null)
                {
                    this.overriddenLruCache.Dispose();
                }

                if (value != null)
                {
                    this.overriddenLruCache = Translator.GetLRUCache(value);
                    NativeMethods.leveldb_options_set_cache(this.Handle, this.overriddenLruCache.Handle);
                }
                else
                {
                    this.overriddenLruCache = null;
                    NativeMethods.leveldb_options_set_cache(this.Handle, IntPtr.Zero);
                }
            }
        }

        public bool CreateIfMissing
        {
            get { return this.createIfMissing; }
            set
            {
                this.ThrowIfDisposed();
                this.createIfMissing = value;
                NativeMethods.leveldb_options_set_create_if_missing(this.Handle, value);
            }
        }

        public bool ThrowErrorIfExists
        {
            get { return this.throwErrorIfExists; }
            set
            {
                this.ThrowIfDisposed();
                this.throwErrorIfExists = value;
                NativeMethods.leveldb_options_set_error_if_exists(this.Handle, value);
            }
        }

        public bool UseParanoidChecks
        {
            get { return this.useParanoidChecks; }
            set
            {
                this.ThrowIfDisposed();
                this.useParanoidChecks = value;
                NativeMethods.leveldb_options_set_paranoid_checks(this.Handle, value);
            }
        }

        public ulong WriteBufferSizeInBytes
        {
            get { return this.writeBufferSizeInBytes; }
            set
            {
                this.ThrowIfDisposed();
                this.writeBufferSizeInBytes = value;
                NativeMethods.leveldb_options_set_write_buffer_size(this.Handle, value);
            }
        }

        public int MaximumNumberOfOpenFiles
        {
            get { return this.maximumNumberOfOpenFiles; }
            set
            {
                this.ThrowIfDisposed();
                this.maximumNumberOfOpenFiles = value;
                NativeMethods.leveldb_options_set_max_open_files(this.Handle, value);
            }
        }

        public ulong BlockSizeInBytes
        {
            get { return this.blockSizeInBytes; }
            set
            {
                this.ThrowIfDisposed();
                this.blockSizeInBytes = value;
                NativeMethods.leveldb_options_set_block_size(this.Handle, value);
            }
        }

        public int BlockRestartIntervalInNumberOfKeys
        {
            get { return this.blockRestartIntervalInNumberOfKeys; }
            set
            {
                this.ThrowIfDisposed();
                this.blockRestartIntervalInNumberOfKeys = value;
                NativeMethods.leveldb_options_set_block_restart_interval(this.Handle, value);
            }
        }

        public CompressionOption CompressionOption
        {
            get { return this.compressionOption; }
            set
            {
                this.ThrowIfDisposed();
                this.compressionOption = value;
                NativeMethods.leveldb_options_set_compression(this.Handle, value);
            }
        }

        internal IntPtr Handle
        {
            get
            {
                this.ThrowIfDisposed();
                return this.underlyingHandle;
            }
        }

        protected override void DisposeUnmanagedResources()
        {
            NativeMethods.leveldb_options_destroy(this.Handle);
            base.DisposeUnmanagedResources();
        }
    }
}
