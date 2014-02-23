using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Evercoin.Util;

namespace Evercoin
{
    public static class StreamExtensions
    {
        public static async Task<ImmutableList<byte>> ReadBytesAsync(this Stream stream, ulong numberOfBytesToRead)
        {
            ulong bytesRead = 0;
            ImmutableList<byte> bytes = ImmutableList<byte>.Empty;

            while (bytesRead < numberOfBytesToRead)
            {
                int numberOfBytesToReadThisOuterLoop = (int)Math.Min(Int32.MaxValue, numberOfBytesToRead - bytesRead);
                ImmutableList<byte> bytesReadThisOuterLoop = await stream.ReadBytesAsyncWithIntParam(numberOfBytesToReadThisOuterLoop);
                bytes = bytes.AddRange(bytesReadThisOuterLoop);
                bytesRead += (ulong)bytesReadThisOuterLoop.Count;
            }

            return bytes;
        }

        public static async Task<ImmutableList<byte>> ReadBytesAsyncWithIntParam(this Stream stream, int numberOfBytesToRead)
        {
            int numberOfBytesRead = 0;
            byte[] data = new byte[numberOfBytesToRead];
            while (numberOfBytesRead < numberOfBytesToRead)
            {
                int bytesReadThisLoop = await stream.ReadAsync(data, numberOfBytesRead, numberOfBytesToRead - data.Length);
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
