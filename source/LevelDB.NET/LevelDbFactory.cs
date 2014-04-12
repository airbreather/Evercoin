namespace LevelDb
{
    public sealed class LevelDbFactory
    {
        public Database OpenDatabase(string directoryPath)
        {
            return this.OpenDatabase(directoryPath, new DatabaseOptions());
        }

        public Database OpenDatabase(string directoryPath, IDatabaseOptions databaseOptions)
        {
            return new Database(databaseOptions, directoryPath);
        }
        public LRUCache CreateLRUCache(ulong capacityInBytes)
        {
            return new LRUCache(capacityInBytes);
        }

        public DatabaseOptions CreateDatabaseOptions()
        {
            return new DatabaseOptions();
        }
    }
}
