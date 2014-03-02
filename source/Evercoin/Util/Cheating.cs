using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Evercoin.Util
{
    public static class Cheating
    {
        public static readonly KeyedCollection<BigInteger, BigInteger> BlockIdentifiers = new BlockIdCollection
                                                                   {
                                                                       new BigInteger(ByteTwiddling.HexStringToByteArray("000000000019D6689C085AE165831E934FF763AE46A2A6C172B3F1B60A8CE26F").Reverse().ToArray())
                                                                   };

        private sealed class BlockIdCollection : KeyedCollection<BigInteger, BigInteger>
        {
            /// <summary>
            /// When implemented in a derived class, extracts the key from the specified element.
            /// </summary>
            /// <returns>
            /// The key for the specified element.
            /// </returns>
            /// <param name="item">The element from which to extract the key.</param>
            protected override BigInteger GetKeyForItem(BigInteger item)
            {
                return item;
            }
        }
    }
}
