namespace Evercoin.Storage
{
    internal static class EfValueConverters
    {
        public static uint UInt32FromEfInt32(int value)
        {
            return unchecked((uint)(value + int.MaxValue));
        }

        public static int EfInt32FromUInt32(uint value)
        {
            return unchecked((int)(value - int.MaxValue));
        }
        public static ulong UInt64FromEfInt64(long value)
        {
            return unchecked((ulong)(value + long.MaxValue));
        }

        public static long EfInt64FromUInt64(ulong value)
        {
            return unchecked((long)(value - long.MaxValue));
        }
    }
}
