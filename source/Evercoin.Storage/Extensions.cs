using System.Runtime.Serialization;

namespace Evercoin.Storage
{
    internal static class Extensions
    {
        public static T GetValue<T>(this SerializationInfo info, string name)
        {
            return (T)info.GetValue(name, typeof(T));
        }
    }
}
