// Developer: heaplyn
// Date: 2026-08-19
// Summary: Master Chronology and Activity Logging Manager.
//          Automatically records user actions, window transitions, commands, and major system events.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class ChronoLogManager
    {
        private static readonly object _lock = new object();
        private static string LogDir => Path.Combine(PathHandler.GetDataDirectory(), "Context", "History");

        static ChronoLogManager()
        {
            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
        }

        public static void LogEvent(string category, string detail)
        {
            Task.Run(async () => {
                try {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string entry = $"[{timestamp}] [{category.ToUpper()}] {detail}";

                    string dateFile = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log");

                    lock (_lock) {
                        File.AppendAllText(dateFile, entry + Environment.NewLine);
                    }

                    var memory = new MemoryNode {
                        Category = "Activity",
                        Content = $"User Activity: {category} - {detail}",
                        Timestamp = DateTime.Now
                    };
                    await ContextNotesManager.SyncMemoryToNotesAsync(memory);
                } catch { }
            });
        }

        public static string GetRecentLogs(int count = 20)
        {
            try
            {
                string dateFile = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                if (!File.Exists(dateFile)) return "No logs recorded today.";

                lock (_lock)
                {
                    var lines = File.ReadAllLines(dateFile);
                    return string.Join(Environment.NewLine, lines.TakeLast(count));
                }
            }
            catch { return "Error fetching recent logs."; }
        }

        public static string GetHistoryForDate(DateTime date)
        {
            string dateFile = Path.Combine(LogDir, $"{date:yyyy-MM-dd}.log");
            if (!File.Exists(dateFile)) return "No activity recorded for this date, Sir.";

            try {
                lock (_lock) {
                    var lines = File.ReadAllLines(dateFile);
                    if (lines.Length > 200) return string.Join(Environment.NewLine, lines.TakeLast(200)) + "\n... (Log truncated)";
                    return string.Join(Environment.NewLine, lines);
                }
            } catch { return "Error reading chronology logs."; }
        }

        public static void StartAutoTracker()
        {
            Task.Run(async () => {
                string lastWin = "";
                while (true) {
                    try {
                        string currentWin = CoreRegistry.Memory.GetCurrentWindowTitle();
                        if (currentWin != lastWin && !string.IsNullOrWhiteSpace(currentWin)) {
                            LogEvent("Window", $"Switched to: {currentWin}");
                            lastWin = currentWin;
                        }
                    } catch { }
                    await AdaptiveSleeper.DelayAsync(10000);
                }
            });
        }
    }
}
