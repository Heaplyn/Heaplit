// Developer: heaplyn
// Date: 2026-08-17
// Summary: High-performance Storage Cleanup & Analysis Service.
//          Handles temp files, recycle bin, large file discovery, and log rotation.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class StorageCleanupManager : IStorageCleanupService
    {
        [DllImport("shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
        const uint SHERB_NOCONFIRMATION = 0x00000001;
        const uint SHERB_NOPROGRESSUI = 0x00000002;
        const uint SHERB_NOSOUND = 0x00000004;

        public Task<long> GetTempFolderSizeAsync() => Task.Run(() => GetDirectorySize(Path.GetTempPath()));

        public Task<int> ClearTempFilesAsync() => Task.Run(() => {
            int count = 0;
            foreach (var file in Directory.GetFiles(Path.GetTempPath())) {
                try { File.Delete(file); count++; } catch { }
            }
            foreach (var dir in Directory.GetDirectories(Path.GetTempPath())) {
                try { Directory.Delete(dir, true); count++; } catch { }
            }
            return count;
        });

        public Task<long> GetRecycleBinSizeAsync() => Task.Run(() => {
            // Complex to get exact size via Shell, simplified estimate
            return 0L;
        });

        public Task<bool> EmptyRecycleBinAsync() => Task.Run(() => {
            try {
                int res = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                return res == 0;
            } catch { return false; }
        });

        public Task<List<StorageFileItem>> FindLargeFilesAsync(string rootPath, long minSize, int limit) => Task.Run(() => {
            var items = new List<StorageFileItem>();
            try {
                var dir = new DirectoryInfo(rootPath);
                var files = dir.GetFiles("*.*", SearchOption.AllDirectories)
                               .Where(f => f.Length >= minSize)
                               .OrderByDescending(f => f.Length)
                               .Take(limit);
                foreach (var f in files) items.Add(new StorageFileItem { Name = f.Name, Path = f.FullName, SizeBytes = f.Length });
            } catch { }
            return items;
        });

        public Task<long> GetLogFolderSizeAsync() => Task.Run(() => {
            string logDir = Path.Combine(PathHandler.GetDataDirectory(), "Logs");
            return GetDirectorySize(logDir);
        });

        public Task<int> CleanOldLogsAsync(int days) => Task.Run(() => {
            int count = 0;
            string logDir = Path.Combine(PathHandler.GetDataDirectory(), "Logs");
            if (!Directory.Exists(logDir)) return 0;
            foreach (var file in Directory.GetFiles(logDir)) {
                if (File.GetCreationTime(file) < DateTime.Now.AddDays(-days)) {
                    try { File.Delete(file); count++; } catch { }
                }
            }
            return count;
        });

        public Dictionary<string, string> GetDiskSpaceInfo()
        {
            var info = new Dictionary<string, string>();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady)) {
                double free = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                double total = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
                info[drive.Name] = $"{free:F1} GB free of {total:F1} GB";
            }
            return info;
        }

        private long GetDirectorySize(string path) {
            if (!Directory.Exists(path)) return 0;
            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
        }

        // Static Bridge
        public static Task<int> ClearTempStatic() => CoreRegistry.Data.StorageCleanup.ClearTempFilesAsync();
        public static Task<bool> EmptyBinStatic() => CoreRegistry.Data.StorageCleanup.EmptyRecycleBinAsync();
    }
}
