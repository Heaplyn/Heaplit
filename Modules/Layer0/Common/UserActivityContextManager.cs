// Developer: heaplyn
// Date: 2026-08-13
// Summary: User System Activity & History Context Manager.
// Gathers real-time active window, command history, recent debug logs, and clipboard context for AI queries.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace HeaplitLauncher
{
    public class ActivityItem
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Text { get; set; } = string.Empty;
    }

    public static class UserActivityContextManager
    {
        private static readonly List<ActivityItem> _recentQueries = new List<ActivityItem>();
        private static readonly object _lock = new object();

        public static void TrackUserQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            lock (_lock)
            {
                _recentQueries.Add(new ActivityItem { Text = query.Trim() });
                if (_recentQueries.Count > 20) _recentQueries.RemoveAt(0);

                // ANTI-ROT: Prune items older than 30 minutes
                _recentQueries.RemoveAll(q => (DateTime.Now - q.Timestamp).TotalMinutes > 30);
            }
            PredictiveStreamManager.IngestEvent("COMMAND", query);
            ActionJournalManager.LogAction("USER_QUERY", query, ScreenMonitorEngine.ActiveWindowTitle);
        }

        public static string BuildFullActivityContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[USER ENVIRONMENT & RECENT ACTIVITY CONTEXT]");
            sb.AppendLine($"• Current Local Time: {DateTime.Now:F}");

            // 1. Active Window Context
            try
            {
                ScreenMonitorEngine.UpdateActiveWindowInfo();
                string activeWin = ScreenMonitorEngine.ActiveWindowTitle;
                string activeProc = ScreenMonitorEngine.ActiveProcessName;
                if (!string.IsNullOrEmpty(activeWin))
                {
                    sb.AppendLine($"• Active Window: '{activeWin}' ({activeProc})");
                }
            }
            catch { }

            // 2. Recent Search & Command Queries
            lock (_lock)
            {
                // Prune rot before building string
                _recentQueries.RemoveAll(q => (DateTime.Now - q.Timestamp).TotalMinutes > 30);

                if (_recentQueries.Count > 0)
                {
                    sb.AppendLine($"• Recent HUD Queries & Commands (Active Session):");
                    foreach (var q in _recentQueries.TakeLast(5))
                    {
                        sb.AppendLine($"  - [{q.Timestamp:HH:mm:ss}] {q.Text}");
                    }
                }
            }

            // 3. Clipboard Context (If Text)
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try {
                            if (Clipboard.ContainsText())
                            {
                                string clip = Clipboard.GetText().Trim();
                                if (!string.IsNullOrEmpty(clip))
                                {
                                    string preview = clip.Length > 200 ? clip.Substring(0, 200) + "..." : clip;
                                    preview = preview.Replace("\r", " ").Replace("\n", " ");
                                    sb.AppendLine($"• Current Clipboard Content: \"{preview}\"");
                                }
                            }
                        } catch { }
                    });
                }
            }
            catch { }

            sb.AppendLine("--------------------------------------------------");
            return sb.ToString();
        }
    }
}
