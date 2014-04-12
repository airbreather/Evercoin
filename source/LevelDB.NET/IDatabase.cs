using System;

namespace LevelDb
{
    public interface IDatabase : IDisposable
    {
        byte[] Get(byte[] key);

        byte[] Get(byte[] key, IReadOptions readOptions);

        void Put(byte[] key, byte[] value);

        void Put(byte[] key, byte[] value, IWriteOptions writeOptions);

        void Delete(byte[] key);

        void Delete(byte[] key, IWriteOptions writeOptions);
    }
}