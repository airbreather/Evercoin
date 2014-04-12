using System;
using System.Runtime.InteropServices;

namespace LevelDb
{
    public sealed class Database : DisposableObject, IDatabase
    {
        private readonly IntPtr underlyingHandle;

        private readonly DatabaseOptions options;

        internal Database(IDatabaseOptions options, string path)
        {
            this.options = Translator.GetDatabaseOptions(options);

            string error;
            this.underlyingHandle = NativeMethods.leveldb_open(this.options.Handle, path, out error);
            if (!String.IsNullOrEmpty(error))
            {
                throw new LevelDbException(error);
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

        public byte[] Get(byte[] key)
        {
            return this.Get(key, new ReadOptions());
        }

        public byte[] Get(byte[] key, IReadOptions readOptions)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (readOptions == null)
            {
                throw new ArgumentNullException("readOptions");
            }

            this.ThrowIfDisposed();

            IntPtr resultPointer;
            ulong valueLength;
            string error;
            using (ReadOptions typedReadOptions = Translator.GetReadOptions(readOptions))
            {
                resultPointer = NativeMethods.leveldb_get(this.Handle, typedReadOptions.UnderlyingValue, key, (ulong)key.LongLength, out valueLength, out error);
            }

            if (!String.IsNullOrEmpty(error))
            {
                throw new LevelDbException(error);
            }

            if (resultPointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                byte[] results = new byte[valueLength];
                Marshal.Copy(resultPointer, results, 0, (int)valueLength);
                return results;
            }
            finally
            {
                NativeMethods.leveldb_free(resultPointer);
            }
        }

        public void Put(byte[] key, byte[] value)
        {
            this.Put(key, value, new WriteOptions());
        }

        public void Put(byte[] key, byte[] value, IWriteOptions writeOptions)
        {
            string error;
            using (WriteOptions typedWriteOptions = Translator.GetWriteOptions(writeOptions))
            {
                ulong _;
                NativeMethods.leveldb_put(this.Handle, typedWriteOptions.Handle, key, (ulong)key.LongLength, value, (ulong)value.LongLength, out error);
            }

            if (!String.IsNullOrEmpty(error))
            {
                throw new LevelDbException(error);
            }
        }

        public void Delete(byte[] key)
        {
            this.Delete(key, new WriteOptions());
        }

        public void Delete(byte[] key, IWriteOptions writeOptions)
        {
            string error;
            using (WriteOptions typedWriteOptions = Translator.GetWriteOptions(writeOptions))
            {
                ulong _;
                NativeMethods.leveldb_delete(this.Handle, typedWriteOptions.Handle, key, (ulong)key.LongLength, out error);
            }

            if (!String.IsNullOrEmpty(error))
            {
                throw new LevelDbException(error);
            }
        }

        protected override void DisposeManagedResources()
        {
            this.options.Dispose();
            base.DisposeManagedResources();
        }

        protected override void DisposeUnmanagedResources()
        {
            NativeMethods.leveldb_close(this.Handle);
            base.DisposeUnmanagedResources();
        }
    }
}
