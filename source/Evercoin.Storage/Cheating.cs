using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Evercoin.Storage
{
    internal static class Cheating
    {
        private static readonly object syncLock = new object();

        private static bool copied = false;

        public static void CopyLevelDbDll()
        {
            if (copied)
            {
                return;
            }

            lock (syncLock)
            {
                if (copied)
                {
                    return;
                }

                const string ResourceTag = "leveldb-native.dll";
                Assembly thisAssembly = Assembly.GetExecutingAssembly();

                string thisAssemblyFolderPath = Path.GetDirectoryName(thisAssembly.Location);
                string targetFilePath = Path.Combine(thisAssemblyFolderPath, ResourceTag);

                string fullResourceTag = String.Join(".", thisAssembly.GetName().Name, ResourceTag);

                byte[] resourceData;
                using (var ms = new MemoryStream())
                {
                    using (var resourceStream = thisAssembly.GetManifestResourceStream(fullResourceTag))
                    {
                        resourceStream.CopyTo(ms);
                    }

                    resourceData = ms.ToArray();
                }

                if (!File.Exists(targetFilePath) ||
                    !File.ReadAllBytes(targetFilePath).SequenceEqual(resourceData))
                {
                    File.WriteAllBytes(targetFilePath, resourceData);
                }

                copied = true;
            }
        }
    }
}
