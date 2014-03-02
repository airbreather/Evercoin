using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Evercoin.Util
{
    public static class Cheating
    {
        private static int maxIndex = 0;
        private static readonly object syncLock = new object();

        private static BigInteger[] BlockIdentifiers = { new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").Reverse().ToArray()) };

        public static void Add(int height, BigInteger blockIdentifier)
        {
            lock (syncLock)
            {
                if (BlockIdentifiers.Length < height + 1)
                {
                    Array.Resize(ref BlockIdentifiers, height + 30000);
                }

                BlockIdentifiers[height] = blockIdentifier;
                maxIndex = Math.Max(maxIndex, height);
            }
        }

        public static IReadOnlyList<BigInteger> GetBlockIdentifiers()
        {
            lock (syncLock)
            {
                return new ArraySegment<BigInteger>(BlockIdentifiers, 0, maxIndex + 1);
            }
        }
    }
}
