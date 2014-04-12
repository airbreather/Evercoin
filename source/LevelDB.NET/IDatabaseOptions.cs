using System;

namespace LevelDb
{
    public interface IDatabaseOptions : IDisposable
    {
        IComparator OverriddenComparator { get; set; }

        IFilterPolicy OverriddenFilterPolicy { get; set; }

        ILRUCache OverriddenLruCache { get; set; }

        bool CreateIfMissing { get; set; }

        bool ThrowErrorIfExists { get; set; }

        bool UseParanoidChecks { get; set; }

        ulong WriteBufferSizeInBytes { get; set; }

        int MaximumNumberOfOpenFiles { get; set; }

        ulong BlockSizeInBytes { get; set; }

        int BlockRestartIntervalInNumberOfKeys { get; set; }

        CompressionOption CompressionOption { get; set; }
    }
}