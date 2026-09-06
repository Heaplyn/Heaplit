// Developer: heaplyn
// Date: 2026-08-19
// Summary: Manages synchronization of training data and configuration between a Main PC and a Backup PC.
//          Supports automated background syncing and manual "Pull" operations.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class SyncFileEntry
    {
        public string RelativePath { get; set; } = "";
        public long Size { get; set; } = 0;
        public DateTime LastModified { get; set; }
    }

    public static class BackupSyncManager
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        private static bool _isSyncing = false;

        public static bool IsSyncing => _isSyncing;

        public static void StartAutoSync()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var set = SettingsManager.Current;
                    if (set.AUTO_SYNC_WITH_BACKUP && !string.IsNullOrEmpty(set.BACKUP_PC_URL) && !set.IS_BACKUP_PC)
                    {
                        try { await RunSyncCycleAsync(); } catch { }
                    }
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromMinutes(Math.Max(5, set.AUTO_SYNC_INTERVAL_MINUTES)));
                }
            });
        }

        public static async Task<string> RunSyncCycleAsync()
        {
            if (_isSyncing) return "Sync already in progress.";
            var set = SettingsManager.Current;
            if (string.IsNullOrEmpty(set.BACKUP_PC_URL)) return "No backup PC URL configured.";

            _isSyncing = true;
            DebugConsoleOverlay.Log("Backup-Sync", $"Initiating sync with Backup PC: {set.BACKUP_PC_URL}");

            try
            {
                // 1. Get manifest from Backup PC
                var manifest = await GetBackupManifestAsync();
                if (manifest == null) throw new Exception("Failed to retrieve manifest from Backup PC.");

                // 2. Identify missing or outdated local files
                var dataDir = PathHandler.GetDataDirectory();
                var toDownload = new List<SyncFileEntry>();

                foreach (var entry in manifest)
                {
                    string localPath = Path.Combine(dataDir, entry.RelativePath);
                    if (!File.Exists(localPath))
                    {
                        toDownload.Add(entry);
                        continue;
                    }

                    var info = new FileInfo(localPath);
                    if (info.LastWriteTimeUtc < entry.LastModified.AddSeconds(-1)) // Buffer for FS precision
                    {
                        toDownload.Add(entry);
                    }
                }

                if (toDownload.Count == 0)
                {
                    DebugConsoleOverlay.Log("Backup-Sync", "Local training data is already up to date.");
                    return "Synchronization complete. No files updated.";
                }

                DebugConsoleOverlay.Log("Backup-Sync", $"Syncing {toDownload.Count} files from backup...");

                // 3. Download files (Batched or single zip if possible, but let's do one by one for robustness)
                int successCount = 0;
                foreach (var entry in toDownload)
                {
                    try
                    {
                        await DownloadFileAsync(entry.RelativePath);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        DebugConsoleOverlay.Log("Backup-Sync-Error", $"Failed to sync {entry.RelativePath}: {ex.Message}");
                    }
                }

                string msg = $"Synchronization complete. Updated {successCount}/{toDownload.Count} files.";
                DebugConsoleOverlay.Log("Backup-Sync", msg);
                return msg;
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Backup-Sync-Error", $"Sync cycle failed: {ex.Message}");
                return $"Sync failed: {ex.Message}";
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private static async Task<List<SyncFileEntry>?> GetBackupManifestAsync()
        {
            var set = SettingsManager.Current;
            string url = $"{set.BACKUP_PC_URL.TrimEnd('/')}/api/backup/manifest";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(set.BACKUP_PC_SECRET))
                req.Headers.Add("X-Heaplit-Secret", set.BACKUP_PC_SECRET);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            string json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<SyncFileEntry>>(json);
        }

        private static async Task DownloadFileAsync(string relativePath)
        {
            var set = SettingsManager.Current;
            string url = $"{set.BACKUP_PC_URL.TrimEnd('/')}/api/backup/download?path={Uri.EscapeDataString(relativePath)}";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(set.BACKUP_PC_SECRET))
                req.Headers.Add("X-Heaplit-Secret", set.BACKUP_PC_SECRET);

            var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode) throw new Exception($"Server returned {resp.StatusCode}");

            string localPath = Path.Combine(PathHandler.GetDataDirectory(), relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

            using (var stream = await resp.Content.ReadAsStreamAsync())
            using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs);
            }

            // Sync the timestamp if the server provided it
            if (resp.Headers.TryGetValues("X-Last-Modified", out var values))
            {
                if (DateTime.TryParse(values.First(), out DateTime dt))
                    File.SetLastWriteTimeUtc(localPath, dt);
            }
        }

        // --- Server-Side Logic (Run on the Backup PC) ---

        public static List<SyncFileEntry> GenerateManifest()
        {
            var list = new List<SyncFileEntry>();
            var dataDir = PathHandler.GetDataDirectory();

            // Whitelist of directories to sync
            var targets = new[] { "Models", "Training", "Context", "Intelligence", "VoiceDataset" };

            foreach (var target in targets)
            {
                string dir = Path.Combine(dataDir, target);
                if (!Directory.Exists(dir)) continue;

                foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    var info = new FileInfo(file);
                    list.Add(new SyncFileEntry
                    {
                        RelativePath = Path.GetRelativePath(dataDir, file),
                        Size = info.Length,
                        LastModified = info.LastWriteTimeUtc
                    });
                }
            }

            return list;
        }
    }
}
