using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace Evercoin.Streams
{
    /// <summary>
    /// A base class for <see cref="Stream"/> classes.  Eliminates a ton of
    /// repetitive code every time I want to implement <see cref="Stream"/>.
    /// </summary>
    public class BaseStream : Stream
    {
        /// <summary>
        /// Gets a value indicating whether this stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the length of this stream, in bytes.
        /// </summary>
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets a value indicating whether this stream is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Reads a sequence of bytes from the current stream
        /// and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes.
        /// When this method returns, the buffer contains the specified byte array with the values between 
        /// <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced
        /// by the bytes read from the current source. 
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer"/>
        /// at which to begin storing the data read from the current stream. 
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to be read from the current stream. 
        /// </param>
        /// <returns>
        /// The total number of bytes read into the buffer.
        /// This can be less than the number of bytes requested if that many bytes are not currently available,
        /// or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. 
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="buffer"/> is <b>null</b>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the stream was closed. 
        /// </exception>
        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();
            this.ThrowIfNotReadable();
            return this.ReadInternal(buffer, offset, count);
        }
        
        /// <summary>
        /// Writes a sequence of bytes to the current stream
        /// and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. 
        /// </param>
        /// <param name="count">
        /// The number of bytes to be written to the current stream.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. 
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="buffer"/> is <b>null</b>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the stream was closed. 
        /// </exception>
        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();
            this.ThrowIfNotWritable();
            this.WriteInternal(buffer, offset, count);
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream
        /// and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// The method is not supported.
        /// </summary>
        /// <param name="offset">
        /// The parameter is not used.
        /// </param>
        /// <param name="origin">
        /// The parameter is not used.
        /// </param>
        /// <returns>
        /// Nothing, ever.
        /// </returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">
        /// The desired length of the current stream in bytes.
        /// </param>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The stream does not support both writing and seeking.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the stream was closed.
        /// </exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Implements <see cref="Stream.Read"/> assuming that parameters have been validated.
        /// </summary>
        /// <param name="buffer">
        /// Where to store bytes that are read.
        /// </param>
        /// <param name="offset">
        /// Where in <paramref name="buffer"/> to start.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to read.
        /// </param>
        /// <returns>
        /// The total number of bytes read.
        /// </returns>
        protected virtual int ReadInternal(byte[] buffer, int offset, int count)
        {
            Contract.Requires<ArgumentNullException>(buffer != null, "buffer != null");
            Contract.Requires<ArgumentOutOfRangeException>(offset >= 0, "offset >= 0");
            Contract.Requires<ArgumentOutOfRangeException>(count >= 0, "count >= 0");
            Contract.Requires<ArgumentOutOfRangeException>(count <= buffer.Length - offset, " count <= (buffer.Length - offset)");
            Contract.Requires<NotSupportedException>(this.CanRead, "stream does not support reading.");
            Contract.Requires<ObjectDisposedException>(!this.IsDisposed, "cannot read from a closed stream.");
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            throw new NotSupportedException();
        }

        /// <summary>
        /// Implements <see cref="Stream.Write"/> assuming that parameters have been validated.
        /// </summary>
        /// <param name="buffer">
        /// The bytes to write.
        /// </param>
        /// <param name="offset">
        /// Where the bytes to write start in <paramref name="buffer"/>.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to write.
        /// </param>
        protected virtual void WriteInternal(byte[] buffer, int offset, int count)
        {
            Contract.Requires<ArgumentNullException>(buffer != null, "buffer != null");
            Contract.Requires<ArgumentOutOfRangeException>(offset >= 0, "offset >= 0");
            Contract.Requires<ArgumentOutOfRangeException>(count >= 0, "count >= 0");
            Contract.Requires<ArgumentOutOfRangeException>(count <= buffer.Length - offset, "count <= (buffer.Length - offset)");
            Contract.Requires<NotSupportedException>(this.CanWrite, "stream does not support writing.");
            Contract.Requires<ObjectDisposedException>(!this.IsDisposed, "cannot write to a closed stream.");
            throw new NotSupportedException();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the stream and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <b>true</b> to release both managed and unmanaged resources; <b>false</b> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.IsDisposed = true;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Throws if this stream has already been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the stream was closed. 
        /// </exception>
        protected void ThrowIfDisposed()
        {
            Contract.Ensures(Contract.OldValue(this.CanRead) == this.CanRead);
            Contract.Ensures(Contract.OldValue(this.CanWrite) == this.CanWrite);
            Contract.Ensures(!this.IsDisposed);

            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        /// <summary>
        /// Throws if this stream does not support writing.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// This stream does not support writing.
        /// </exception>
        protected void ThrowIfNotWritable()
        {
            Contract.Ensures(Contract.OldValue(this.CanRead) == this.CanRead);
            Contract.Ensures(Contract.OldValue(this.IsDisposed) == this.IsDisposed);
            Contract.Ensures(this.CanWrite);

            if (!this.CanWrite)
            {
                throw new NotSupportedException("Stream does not support writing.");
            }
        }

        /// <summary>
        /// Throws if this stream does not support reading.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// This stream does not support reading.
        /// </exception>
        protected void ThrowIfNotReadable()
        {
            Contract.Ensures(Contract.OldValue(this.CanWrite) == this.CanWrite);
            Contract.Ensures(Contract.OldValue(this.IsDisposed) == this.IsDisposed);
            Contract.Ensures(this.CanRead);

            if (!this.CanRead)
            {
                throw new NotSupportedException("Stream does not support reading.");
            }
        }
    }
}
