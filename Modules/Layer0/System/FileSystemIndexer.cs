// Developer: heaplyn
// Date: 2026-09-02
// Summary: Slow, low-impact background filesystem indexer. Walks the user's drives one directory at
//          a time with an adaptive delay between each (scaled up under load), building a searchable
//          path index the AI can reference. Read-only. Skips OS/junk dirs. Persists to disk so it
//          resumes across runs. Gated behind ENABLE_FILE_INDEXING.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class FileSystemIndexer
    {
        private static readonly HashSet<string> _index = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new();
        private static int _started;

        private static string IndexFile =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "FileIndex.txt");

        public static int Count { get { lock (_lock) return _index.Count; } }
        public static bool IsScanning { get; private set; }

        // Directory names to skip entirely (OS internals, caches, VCS, huge dependency trees).
        private static readonly string[] SkipNames =
        {
            "windows", "$recycle.bin", "system volume information", "program files",
            "program files (x86)", "programdata", "node_modules", ".git", "obj", "bin",
            "appdata", "$windows.~ws", "$windows.~bt", "recovery", ".vs", ".gradle", ".nuget"
        };

        public static void Start()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
            _ = Task.Run(ScanLoopAsync);
        }

        public static List<string> Search(string query, int max = 8)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();
            lock (_lock)
                return _index.Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(max).ToList();
        }

        private static async Task ScanLoopAsync()
        {
            LoadIndex();
            try
            {
                while (true)
                {
                    if (!CoreRegistry.Data.Settings.Current.ENABLE_FILE_INDEXING)
                    {
                        await AdaptiveSleeper.DelayAsync(30000);   // idle-check while disabled
                        continue;
                    }

                    IsScanning = true;
                    var roots = GetRoots();
                    var stack = new Stack<string>(roots);
                    int sinceSave = 0;

                    while (stack.Count > 0)
                    {
                        if (!CoreRegistry.Data.Settings.Current.ENABLE_FILE_INDEXING) break;
                        string dir = stack.Pop();

                        try
                        {
                            foreach (var f in Directory.EnumerateFiles(dir))
                            { lock (_lock) _index.Add(f); }

                            foreach (var sub in Directory.EnumerateDirectories(dir))
                                if (!ShouldSkip(sub)) stack.Push(sub);
                        }
                        catch { /* access denied / transient — skip */ }

                        // Slow but sure: adaptive delay per directory (backs off under load).
                        int baseMs = Math.Max(20, CoreRegistry.Data.Settings.Current.FILE_INDEX_DELAY_MS);
                        await AdaptiveSleeper.DelayAsync(baseMs, default, maxMultiplier: 8);

                        if (++sinceSave >= 400) { SaveIndex(); sinceSave = 0; }
                    }

                    IsScanning = false;
                    SaveIndex();
                    try { DebugConsoleOverlay.Log("File-Index", $"Full pass complete — {Count} files indexed."); } catch { }

                    // Re-scan periodically to pick up new files (adaptive; hours between passes).
                    await AdaptiveSleeper.DelayAsync(1000 * 60 * 30, default, maxMultiplier: 2, maxCapMs: 1000 * 60 * 60);
                }
            }
            catch { IsScanning = false; }
        }

        private static IEnumerable<string> GetRoots()
        {
            // Prefer the user's home first (most relevant), then other fixed drives.
            var roots = new List<string>();
            try { roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)); } catch { }
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                    if (d.IsReady && d.DriveType == DriveType.Fixed && !roots.Contains(d.RootDirectory.FullName))
                        roots.Add(d.RootDirectory.FullName);
            }
            catch { }
            return roots.Where(Directory.Exists);
        }

        private static bool ShouldSkip(string dir)
        {
            try
            {
                string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar)).ToLowerInvariant();
                if (name.StartsWith(".")) return true;
                return SkipNames.Contains(name);
            }
            catch { return true; }
        }

        private static void LoadIndex()
        {
            try
            {
                if (File.Exists(IndexFile))
                    lock (_lock)
                        foreach (var line in File.ReadLines(IndexFile))
                            if (!string.IsNullOrWhiteSpace(line)) _index.Add(line);
            }
            catch { }
        }

        private static void SaveIndex()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(IndexFile)!);
                string[] snapshot;
                lock (_lock) snapshot = _index.ToArray();
                File.WriteAllLines(IndexFile, snapshot);
            }
            catch { }
        }
    }
}
