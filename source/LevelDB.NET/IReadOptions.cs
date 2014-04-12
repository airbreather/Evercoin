using System;

namespace LevelDb
{
    public interface IReadOptions : IDisposable
    {
        bool VerifyChecksums { get; }

        bool CacheResults { get; }

        ISnapshot OverriddenSnapshot { get; set; }
    }
}