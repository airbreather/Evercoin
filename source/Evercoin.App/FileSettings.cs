using System.ComponentModel.Composition;

namespace Evercoin.App
{
    internal sealed class FileSettings
    {
        [Export("Disk.BlockStorageFolderPath")]
        public string BlockStoragePath { get; set; }

        [Export("Disk.TransactionStorageFolderPath")]
        public string TransactionStoragePath { get; set; }
    }
}
