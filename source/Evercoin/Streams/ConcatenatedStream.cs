using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Evercoin.Streams
{
    /// <summary>
    /// A <see cref="Stream"/> that just concatenates multiple streams.
    /// </summary>
    /// <remarks>
    /// This implementation is read-only and forward-only.
    /// If needed, I guess I could extend it to support seeking,
    /// but to support writing would be a major pain.
    /// </remarks>
    internal sealed class ConcatenatedStream : BaseStream
    {
        /// <summary>
        /// The <see cref="Stream"/> objects we're going to read from, in order.
        /// </summary>
        private readonly ReadOnlyCollection<Stream> streams;

        /// <summary>
        /// The index of the <see cref="Stream"/> object in <see cref="streams"/>
        /// that we are currently reading from.
        /// </summary>
        private int streamIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcatenatedStream"/> class.
        /// </summary>
        /// <param name="streams">
        /// The <see cref="Stream"/> objects to concatenate.
        /// </param>
        public ConcatenatedStream(IReadOnlyCollection<Stream> streams)
        {
            Contract.Requires<ArgumentNullException>(streams != null);
            Contract.Ensures(this.streams != null);
            Contract.Ensures(this.streamIndex == 0);
            this.streams = streams.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets a value indicating whether this stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
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
        protected override int ReadInternal(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;

            while (this.streamIndex < this.streams.Count)
            {
                Stream currentStream = this.streams[this.streamIndex];
                if (currentStream != null)
                {
                    bytesRead = currentStream.Read(buffer, offset, count);

                    if (bytesRead != 0)
                    {
                        break;
                    }
                }

                // We're at the end of the current stream.
                // Move onto the next stream, if any.
                this.streamIndex++;
            }

            return bytesRead;
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
                // I'm tempted to do each Dispose call in a try/catch, and throw an
                // AggregateException at the end with all the individual exceptions.
                foreach (Stream stream in this.streams)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        [ContractInvariantMethod]
        private void ContractInvariants()
        {
            Contract.Invariant(this.streams != null);
            Contract.Invariant(this.streamIndex >= 0);
        }
    }
}
