using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin.Streams
{
    /// <summary>
    /// A <see cref="Stream"/> that limits the rate data is read and written.
    /// </summary>
    public sealed class ThrottledStream : StreamWrapper
    {
        /// <summary>
        /// An object to use to synchronize read requests.
        /// </summary>
        private readonly object readLock = new object();

        /// <summary>
        /// An object to use to synchronize write requests.
        /// </summary>
        private readonly object writeLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottledStream"/> class using a given <see cref="Stream"/>.
        /// </summary>
        /// <param name="underlyingStream">
        /// The <see cref="Stream"/> to throttle.
        /// </param>
        public ThrottledStream(Stream underlyingStream)
            : base(underlyingStream)
        {
        }

        /// <summary>
        /// Gets or sets the rate limit for read operations on this <see cref="ThrottledStream"/>, in bytes per second.
        /// </summary>
        public long ReadLimitBytesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the rate limit for write operations on this <see cref="ThrottledStream"/>, in bytes per second.
        /// </summary>
        public long WriteLimitBytesPerSecond { get; set; }

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
        /// The total number of bytes actually read.
        /// </returns>
        protected override int ReadInternal(byte[] buffer, int offset, int count)
        {
            lock (this.readLock)
            {
                Stopwatch readTimer = Stopwatch.StartNew();
                count = base.ReadInternal(buffer, offset, count);

                TimeSpan timeToSleep = GetDelay(readTimer, count, this.ReadLimitBytesPerSecond);
                Thread.Sleep(timeToSleep);
                return count;
            }
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
        protected override void WriteInternal(byte[] buffer, int offset, int count)
        {
            lock (this.writeLock)
            {
                Stopwatch writeTimer = Stopwatch.StartNew();
                base.WriteInternal(buffer, offset, count);

                TimeSpan timeToSleep = GetDelay(writeTimer, count, this.WriteLimitBytesPerSecond);
                Thread.Sleep(timeToSleep);
            }
        }

        /// <summary>
        /// Gets a <see cref="Task"/> that will complete when the given number of bytes 
        /// </summary>
        /// <param name="stopwatch">
        /// A <see cref="Stopwatch"/> that was started when the transfer began.
        /// </param>
        /// <param name="transferBytes">
        /// The amount of bytes being transferred.
        /// </param>
        /// <param name="rateLimitBytesPerSecond">
        /// The maximum number of bytes to transfer each second.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that will complete when the bytes have been transferred, potentially waiting.
        /// </returns>
        private static TimeSpan GetDelay(Stopwatch stopwatch, long transferBytes, long rateLimitBytesPerSecond)
        {
            double minimumTransferSeconds = transferBytes / (double)rateLimitBytesPerSecond;
            TimeSpan minimumTransferTime = TimeSpan.FromSeconds(minimumTransferSeconds);
            TimeSpan currentElapsed = stopwatch.Elapsed;
            TimeSpan delay = minimumTransferTime - currentElapsed;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            return Task.Delay(delay);
        }
    }
}
