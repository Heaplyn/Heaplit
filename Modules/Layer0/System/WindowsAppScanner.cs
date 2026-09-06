// Developer: heaplyn
// Date: 2026-09-03
// Summary: Indexes Windows installed desktop applications with on-demand lazy scanning.
//          Eliminates heavy startup disk I/O; built-in apps resolve immediately.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HeaplitLauncher
{
    public class AppInfo
    {
        public string Name { get; set; } = string.Empty;
        public string NameLower { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public double SIMILARITY { get; set; }
    }

    public class WindowsAppScanner : IAppScannerService
    {
        private readonly List<AppInfo> _cachedApps = new();
        private bool _isIndexed = false;
        private bool _isIndexingInProgress = false;
        private readonly object _lock = new();

        public WindowsAppScanner()
        {
            // Built-in applications are pre-seeded with negligible memory footprint
            var map = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);
            AddBuiltInApps(map);
            _cachedApps.AddRange(map.Values);
        }

        void IAppScannerService.StartScan()
        {
            // Do not force heavy scan on startup — will run lazily on first search
        }

        public void IndexApplications(bool force = false)
        {
            lock (_lock)
            {
                if (!force && _isIndexed) return;
                if (_isIndexingInProgress) return;
                _isIndexingInProgress = true;
            }

            Task.Run(() =>
            {
                try
                {
                    var appMap = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);
                    AddBuiltInApps(appMap);
                    ScanStartMenuDirectory(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), appMap);
                    ScanStartMenuDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), appMap);

                    lock (_lock)
                    {
                        _cachedApps.Clear();
                        _cachedApps.AddRange(appMap.Values);
                        _isIndexed = true;
                        _isIndexingInProgress = false;
                    }
                }
                catch
                {
                    lock (_lock) _isIndexingInProgress = false;
                }
            });
        }

        List<AppInfo> IAppScannerService.GetMatchingApps(string query)
        {
            string q = query.ToLower().Trim();
            if (string.IsNullOrEmpty(q)) return new List<AppInfo>();

            // On-demand lazy index on first user search
            if (!_isIndexed && !_isIndexingInProgress)
            {
                IndexApplications(force: false);
            }

            lock (_lock)
            {
                var results = new List<AppInfo>();
                foreach (var a in _cachedApps)
                {
                    if (a.NameLower.Contains(q) || SearchUtil.IsAcronymMatch(q, a.NameLower))
                    {
                        a.SIMILARITY = SearchUtil.GetSimilarity(q, a.NameLower);
                        results.Add(a);
                    }
                }
                return results.OrderByDescending(a => a.SIMILARITY).ToList();
            }
        }

        private void AddBuiltInApps(Dictionary<string, AppInfo> map)
        {
            void Add(string n, string p)
            {
                if (!map.ContainsKey(n))
                    map[n] = new AppInfo { Name = n, NameLower = n.ToLower(), TargetPath = p };
            }

            Add("Calculator", "calc.exe");
            Add("Notepad", "notepad.exe");
            Add("Task Manager", "taskmgr.exe");
            Add("Command Prompt", "cmd.exe");
            Add("PowerShell", "powershell.exe");
            Add("File Explorer", "explorer.exe");
            Add("Paint", "mspaint.exe");
            Add("Snipping Tool", "snippingtool.exe");
        }

        private void ScanStartMenuDirectory(string baseDir, Dictionary<string, AppInfo> map)
        {
            try
            {
                if (!Directory.Exists(baseDir)) return;
                foreach (var file in Directory.GetFiles(baseDir, "*.lnk", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (!map.ContainsKey(name))
                    {
                        map[name] = new AppInfo { Name = name, NameLower = name.ToLower(), TargetPath = file };
                    }
                }
            }
            catch { }
        }

        // --- STATIC BRIDGES ---
        public static List<AppInfo> GetMatchingApps(string query) => CoreRegistry.System.Apps.GetMatchingApps(query);
        public static void IndexApplicationsGlobal(bool force = false) => ((WindowsAppScanner)CoreRegistry.System.Apps).IndexApplications(force);
    }
}
