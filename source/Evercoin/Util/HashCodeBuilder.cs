using System.Collections.Generic;

namespace Evercoin.Util
{
    public static class HashCodeBuilder
    {
        private const int Seed = 17;

        private const int Mult = 31;

        public static int BeginHashCode()
        {
            return Seed;
        }

        public static int MixHashCodeWith<T>(this int hashCode, T obj)
        {
            unchecked
            {
                hashCode *= Mult;
                if (obj != null)
                {
                    hashCode += obj.GetHashCode();
                }
            }

            return hashCode;
        }

        public static int MixHashCodeWithEnumerable<T>(this int hashCode, IEnumerable<T> obj)
        {
            if (obj == null)
            {
                return MixHashCodeWith(hashCode, obj);
            }

            int count = 0;
            foreach (T element in obj)
            {
                hashCode = MixHashCodeWith(hashCode, element);
                count++;
            }

            // Accumulate with the count to ensure that
            // accumulating with an empty enumerable doesn't have
            // the same result as not accumulating at all.
            return MixHashCodeWith(hashCode, count);
        }
    }
}
