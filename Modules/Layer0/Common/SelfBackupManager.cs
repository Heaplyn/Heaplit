// Developer: heaplyn
// Date: 2026-08-18
// Summary: Automated Codebase Backup & Rotation Manager.
//          Creates compressed snapshots of the entire project root.
//          Maintains a rolling rotation of the 4 most recent copies.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class SelfBackupManager
    {
        private static string BackupRoot => Path.Combine(PathHandler.GetProjectRoot(), "Backups");
        private const int MaxBackups = 4;

        public static async Task<string> CreateBackupAsync(string reason = "auto")
        {
            try
            {
                if (!Directory.Exists(BackupRoot)) Directory.CreateDirectory(BackupRoot);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string zipPath = Path.Combine(BackupRoot, $"Heaplit_Backup_{timestamp}_{reason}.zip");
                string sourceDir = PathHandler.GetProjectRoot();

                await Task.Run(() => {
                    // Create zip while excluding huge/temp folders
                    using (var zipFile = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            string relPath = Path.GetRelativePath(sourceDir, file);
                            // Exclude bin, obj, .git, and previous backups
                            if (relPath.Contains("\\bin\\") || relPath.Contains("\\obj\\") ||
                                relPath.Contains(".git\\") || relPath.Contains("Backups\\") ||
                                relPath.EndsWith(".zip")) continue;

                            zipFile.CreateEntryFromFile(file, relPath);
                        }
                    }
                });

                RotateBackups();
                DebugConsoleOverlay.Log("Backup", $"System Snapshot Created: {Path.GetFileName(zipPath)}");
                return zipPath;
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Backup-Error", ex.Message);
                return "";
            }
        }

        private static void RotateBackups()
        {
            try
            {
                var files = Directory.GetFiles(BackupRoot, "*.zip")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (files.Count > MaxBackups)
                {
                    foreach (var file in files.Skip(MaxBackups))
                    {
                        file.Delete();
                        DebugConsoleOverlay.Log("Backup-Rotation", $"Removed stale backup: {file.Name}");
                    }
                }
            }
            catch { }
        }
    }
}
