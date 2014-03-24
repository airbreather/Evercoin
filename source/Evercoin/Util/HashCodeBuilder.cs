using System.Collections.Generic;

namespace Evercoin.Util
{
    public sealed class HashCodeBuilder
    {
        private const int Seed = 17;

        private const int Mult = 31;

        private readonly int hash;

        public HashCodeBuilder()
            : this(Seed)
        {
        }

        public HashCodeBuilder(int seed)
        {
            this.hash = seed;
        }

        public static implicit operator int(HashCodeBuilder builder)
        {
            return builder.hash;
        }

        public HashCodeBuilder HashWith<T>(T obj)
        {
            int newHash = Accumulate(this.hash, obj);
            return new HashCodeBuilder(newHash);
        }

        public HashCodeBuilder HashWithEnumerable<T>(IEnumerable<T> obj)
        {
            if (obj == null)
            {
                return this.HashWith(obj);
            }

            int newHash = this.hash;

            int count = 0;
            foreach (T element in obj)
            {
                newHash = Accumulate(newHash, element);
                count++;
            }

            // Accumulate with the count to ensure that
            // accumulating with an empty enumerable doesn't have
            // the same result as not accumulating at all.
            newHash = Accumulate(newHash, count);

            return new HashCodeBuilder(newHash);
        }

        private static int Accumulate<T>(int oldHash, T obj)
        {
            int newHash = oldHash;

            unchecked
            {
                newHash *= Mult;
                if (obj != null)
                {
                    newHash += obj.GetHashCode();
                }
            }

            return newHash;
        }
    }
}
