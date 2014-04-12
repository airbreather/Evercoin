namespace LevelDb
{
    public interface ILRUCache
    {
        ulong CapacityInBytes { get; }
    }
}