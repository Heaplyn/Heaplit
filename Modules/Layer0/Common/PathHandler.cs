using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace HeaplitLauncher
{
    public static class PathHandler
    {
        private static string? CachedDataDir;

        /// <summary>
        /// Returns a persistent Data directory. In development, points to the project root.
        /// In production/deployed, points to the AppDomain base or LocalAppData.
        /// </summary>
        public static string GetDataDirectory()
        {
            if (CachedDataDir != null) return CachedDataDir;

            string root = GetProjectRoot();
            string dataDir = Path.Combine(root, "Data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

            CachedDataDir = dataDir;
            return CachedDataDir;
        }

        public static string GetDownloadsDirectory()
        {
            string root = GetProjectRoot();
            string downloadsDir = Path.Combine(root, "Downloads");
            if (!Directory.Exists(downloadsDir))
            {
                Directory.CreateDirectory(downloadsDir);
            }
            return downloadsDir;
        }

        public static string GetProjectRoot()
        {
            string CheckDir = AppDomain.CurrentDomain.BaseDirectory;
            for (int I = 0; I < 8; I++)
            {
                if (File.Exists(Path.Combine(CheckDir, "HeaplitLauncher.csproj")) ||
                    File.Exists(Path.Combine(CheckDir, "Heaplit.sln")) ||
                    Directory.Exists(Path.Combine(CheckDir, ".git")) ||
                    Directory.Exists(Path.Combine(CheckDir, "Modules")))
                {
                    return CheckDir;
                }
                var Parent = Directory.GetParent(CheckDir);
                if (Parent == null) break;
                CheckDir = Parent.FullName;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetCurrentSourceDirectory([CallerFilePath] string CallerPath = "")
        {
            return Path.GetDirectoryName(CallerPath) ?? string.Empty;
        }
    }
}
