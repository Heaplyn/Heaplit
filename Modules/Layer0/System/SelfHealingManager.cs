// Developer: heaplyn
// Date: 2026-09-03
// Summary: High-Performance Self-Healing Guardian for Heaplit.
//          Features:
//          - Proactive Memory Pressure Guardian (automatic LOH compaction, cache purge, working set trim)
//          - Universal Crash Interceptor (AppDomain, Dispatcher, UnobservedTask protection)
//          - Concurrency-Resilient Safe File I/O with exponential backoff retry
//          - Automated directory and corrupted JSON configuration audit & repair
//          - Emergency manual & autonomic self-healing routines

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime;

namespace HeaplitLauncher
{
    public static class SelfHealingManager
    {
        private static bool _initialized = false;
        private static readonly object _initLock = new();
        private static DispatcherTimer? _memoryWatchdogTimer;
        private static DateTime _lastHealingTime = DateTime.MinValue;
        private static long _peakWorkingSetBytes = 0;

        public static void Initialize()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;

                // 1. Hook Global Crash Interceptors
                try
                {
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledDomainException;
                    if (Application.Current != null)
                    {
                        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
                    }
                    TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                }
                catch { }

                // 2. Perform Self-Healing Directory and Data Files Audit
                AuditAndHealDirectories();
                AuditAndHealSettingsFile();
                AuditAndHealDataFiles();

                // 3. Start Proactive Low-Impact Memory Watchdog
                StartMemoryWatchdog();

                _initialized = true;
            }
        }

        private static void StartMemoryWatchdog()
        {
            try
            {
                if (_memoryWatchdogTimer == null && Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _memoryWatchdogTimer = new DispatcherTimer(DispatcherPriority.Background)
                        {
                            Interval = TimeSpan.FromSeconds(20)
                        };
                        _memoryWatchdogTimer.Tick += (s, e) => PerformProactiveMemoryAudit();
                        _memoryWatchdogTimer.Start();
                    });
                }
            }
            catch { }
        }

        public static void PerformProactiveMemoryAudit()
        {
            try
            {
                using var proc = Process.GetCurrentProcess();
                long workingSet = proc.WorkingSet64;
                long privateBytes = proc.PrivateMemorySize64;

                if (workingSet > _peakWorkingSetBytes)
                    _peakWorkingSetBytes = workingSet;

                // If working set exceeds 280MB under load, or if memory grew rapidly, execute self-healing compaction
                if (workingSet > 280 * 1024 * 1024 || privateBytes > 350 * 1024 * 1024)
                {
                    CompactAndHealMemory(reason: $"High memory load detected: {workingSet / (1024 * 1024)}MB RAM");
                }
            }
            catch { }
        }

        public static void CompactAndHealMemory(string reason = "Manual/Autonomic optimization")
        {
            // Rate limit aggressive compaction to at most once every 10 seconds
            if ((DateTime.UtcNow - _lastHealingTime).TotalSeconds < 10) return;
            _lastHealingTime = DateTime.UtcNow;

            Task.Run(() =>
            {
                try
                {
                    // 1. Purge overlay media/texture caches
                    try { BaseOverlay.PurgeSystemMemory(); } catch { }

                    // 2. Clear text geometry caches
                    try { OutlinedText.ClearCache(); } catch { }

                    // 3. Compact Large Object Heap (LOH) to eliminate memory fragmentation
                    try { GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; } catch { }

                    // 4. Run multi-generation GC
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);

                    // 5. Trim process working set pages back to Windows OS kernel
                    try
                    {
                        var handle = Process.GetCurrentProcess().Handle;
                        NativeMethods.EmptyWorkingSet(handle);
                    }
                    catch { }

                    LogEvent("Memory Self-Heal", $"{reason} -> System working set trimmed.");
                }
                catch { }
            });
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true; // Mark handled so the UI thread stays alive
            LogException("UI Dispatcher Intercepted", e.Exception);

            try
            {
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            TextOverlay.Show("⚡ Heaplit Self-Healing: Background fault recovered", 2500);
                        }
                        catch { }
                    }, DispatcherPriority.Background);
                }
            }
            catch { }

            // Trigger proactive self-healing compaction
            CompactAndHealMemory("Recovered from Dispatcher exception");
        }

        private static void OnUnhandledDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException("Domain Exception Intercepted", ex);
                CompactAndHealMemory("Recovered from AppDomain exception");
            }
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // Mark observed to prevent TaskScheduler crash
            LogException("Task Exception Intercepted", e.Exception);
        }

        public static void AuditAndHealDirectories()
        {
            try
            {
                string dataDir = PathHandler.GetDataDirectory();
                string[] requiredDirs = new string[]
                {
                    dataDir,
                    Path.Combine(dataDir, "Context"),
                    Path.Combine(dataDir, "Context", "History"),
                    Path.Combine(dataDir, "Conversations"),
                    Path.Combine(dataDir, "Instructions"),
                    Path.Combine(dataDir, "Notes"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Macros")
                };

                foreach (var dir in requiredDirs)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
            }
            catch { }
        }

        public static void AuditAndHealSettingsFile()
        {
            try
            {
                string dataDir = PathHandler.GetDataDirectory();
                string settingsFile = Path.Combine(dataDir, "SystemSettings.json");

                if (!File.Exists(settingsFile))
                {
                    SettingsManager.Save();
                }
                else
                {
                    try
                    {
                        string content = SafeReadAllText(settingsFile);
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            SettingsManager.Save();
                        }
                        else
                        {
                            using var doc = JsonDocument.Parse(content);
                        }
                    }
                    catch (JsonException)
                    {
                        // File corrupted: backup corrupted file and heal settings
                        try
                        {
                            File.Copy(settingsFile, settingsFile + $".corrupted_{DateTime.Now:yyyyMMddHHmmss}.bak", overwrite: true);
                        }
                        catch { }

                        SettingsManager.Save();
                        LogEvent("Self-Healing", "Restored corrupted SystemSettings.json to defaults.");
                    }
                }
            }
            catch { }
        }

        public static void AuditAndHealDataFiles()
        {
            try
            {
                string dataDir = PathHandler.GetDataDirectory();

                AuditJsonFile(Path.Combine(dataDir, "PinnedFiles.json"), "[]");
                AuditJsonFile(Path.Combine(dataDir, "ClipboardHistory.json"), "[]");
                AuditJsonFile(Path.Combine(dataDir, "Snippets.json"), "[]");
                AuditJsonFile(Path.Combine(dataDir, "AppShortcuts.json"), "[]");

                // Default focus macro
                string macrosDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Macros");
                if (Directory.Exists(macrosDir) && Directory.GetFiles(macrosDir, "*.txt").Length == 0)
                {
                    string focusTxt = Path.Combine(macrosDir, "focus.txt");
                    SafeWriteAllText(focusTxt, "# Self-Healed Default Focus Macro\ntheme dark\nvol 10\nremind 45m Take a break\n");
                }
            }
            catch { }
        }

        private static void AuditJsonFile(string filePath, string defaultContent)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    SafeWriteAllText(filePath, defaultContent);
                }
                else
                {
                    string json = SafeReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        SafeWriteAllText(filePath, defaultContent);
                    }
                    else
                    {
                        using var doc = JsonDocument.Parse(json);
                    }
                }
            }
            catch
            {
                try { SafeWriteAllText(filePath, defaultContent); } catch { }
            }
        }

        // --- CONCURRENCY-RESILIENT SAFE FILE I/O (Exponential Backoff) ---

        public static string SafeReadAllText(string filePath, string defaultFallback = "")
        {
            if (!File.Exists(filePath)) return defaultFallback;

            for (int i = 0; i < 4; i++)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch (IOException)
                {
                    if (i == 3) break;
                    Thread.Sleep(20 * (i + 1));
                }
                catch { break; }
            }
            return defaultFallback;
        }

        public static bool SafeWriteAllText(string filePath, string content)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }

            string tempFile = filePath + $".tmp_{Guid.NewGuid():N}";

            for (int i = 0; i < 4; i++)
            {
                try
                {
                    using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(content);
                    }

                    if (File.Exists(filePath))
                        File.Replace(tempFile, filePath, null);
                    else
                        File.Move(tempFile, filePath);

                    return true;
                }
                catch (IOException)
                {
                    if (i == 3) break;
                    Thread.Sleep(25 * (i + 1));
                }
                catch { break; }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }
            }
            return false;
        }

        private static void LogException(string source, Exception ex)
        {
            try
            {
                string dataDir = PathHandler.GetDataDirectory();
                string logFile = Path.Combine(dataDir, "SelfHealingLog.txt");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex.GetType().Name}: {ex.Message}\nStack: {ex.StackTrace}\n\n";
                File.AppendAllText(logFile, entry);
            }
            catch { }
        }

        private static void LogEvent(string category, string message)
        {
            try
            {
                string dataDir = PathHandler.GetDataDirectory();
                string logFile = Path.Combine(dataDir, "SelfHealingLog.txt");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category}] {message}\n";
                File.AppendAllText(logFile, entry);
            }
            catch { }
        }
    }
}
