using System;
using System.IO;

namespace Evercoin.Streams
{
    /// <summary>
    /// Acts as a simple wrapper around another <see cref="Stream"/>.
    /// </summary>
    /// <remarks>
    /// First use case for this is to create a <see cref="Stream"/> that just acts as a rate limiter.
    /// </remarks>
    public class StreamWrapper : BaseStream
    {
        private readonly Stream underlyingStream;

        protected StreamWrapper(Stream underlyingStream)
        {
            this.underlyingStream = underlyingStream;
        }

        public override bool CanRead
        {
            get { return this.underlyingStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this.underlyingStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return this.underlyingStream.CanWrite; }
        }

        public override void Flush()
        {
            this.underlyingStream.Flush();
        }

        public override long Length
        {
            get { return this.underlyingStream.Length; }
        }

        public override long Position
        {
            get { return this.underlyingStream.Position; }
            set { this.underlyingStream.Position = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.underlyingStream.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override int ReadInternal(byte[] buffer, int offset, int count)
        {
            return this.underlyingStream.Read(buffer, offset, count);
        }

        protected override void WriteInternal(byte[] buffer, int offset, int count)
        {
            this.underlyingStream.Write(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.underlyingStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.underlyingStream.SetLength(value);
        }
    }
}
