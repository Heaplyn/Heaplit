// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for storage analysis and cleanup operations.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public interface IStorageCleanupService
    {
        Task<long> GetTempFolderSizeAsync();
        Task<int> ClearTempFilesAsync();
        Task<long> GetRecycleBinSizeAsync();
        Task<bool> EmptyRecycleBinAsync();
        Task<List<StorageFileItem>> FindLargeFilesAsync(string rootPath, long minSizeInBytes, int limit);
        Task<long> GetLogFolderSizeAsync();
        Task<int> CleanOldLogsAsync(int olderThanDays);
        Dictionary<string, string> GetDiskSpaceInfo();
    }

    public class StorageFileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string ReadableSize => FormatSize(SizeBytes);

        private string FormatSize(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            if (bytes == 0) return "0" + suf[0];
            long mag = (long)Math.Log(bytes, 1024);
            decimal adjustedSize = (decimal)bytes / (decimal)Math.Pow(1024, mag);
            return string.Format("{0:n1} {1}", adjustedSize, suf[mag]);
        }
    }
}
