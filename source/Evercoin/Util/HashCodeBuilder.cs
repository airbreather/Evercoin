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
            int newHash = this.hash;
            unchecked
            {
                newHash *= Mult;
                if (obj != null)
                {
                    newHash += obj.GetHashCode();
                }
            }

            return new HashCodeBuilder(newHash);
        }
    }
}
