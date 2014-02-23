using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Evercoin
{
    public static class StreamExtensions
    {
        public static async Task<ImmutableList<byte>> ReadBytesAsync(this Stream stream, ulong numberOfBytesToRead, CancellationToken token)
        {
            ulong bytesRead = 0;
            ImmutableList<byte> bytes = ImmutableList<byte>.Empty;

            while (bytesRead < numberOfBytesToRead)
            {
                int numberOfBytesToReadThisOuterLoop = (int)Math.Min(Int32.MaxValue, numberOfBytesToRead - bytesRead);
                ImmutableList<byte> bytesReadThisOuterLoop = await stream.ReadBytesAsyncWithIntParam(numberOfBytesToReadThisOuterLoop, token);
                bytes = bytes.AddRange(bytesReadThisOuterLoop);
                bytesRead += (ulong)bytesReadThisOuterLoop.Count;
            }

            return bytes;
        }

        public static async Task<ImmutableList<byte>> ReadBytesAsyncWithIntParam(this Stream stream, int numberOfBytesToRead, CancellationToken token)
        {
            int numberOfBytesRead = 0;
            byte[] data = new byte[numberOfBytesToRead];
            while (numberOfBytesRead < numberOfBytesToRead)
            {
                int bytesReadThisLoop = await stream.ReadAsync(data, numberOfBytesRead, numberOfBytesToRead - numberOfBytesRead, token);
                if (bytesReadThisLoop == 0)
                {
                    throw new EndOfStreamException("Reached the end of the stream before all requested data was read.");
                }

                numberOfBytesRead += bytesReadThisLoop;
            }

            return data.ToImmutableList();
        }
    }
}
