// Developer: heaplyn
// Date: 2026-08-15
// Summary: High-performance User Action Journal.
//          Stores structured summaries of user actions, system events, and AI interjections.
//          Uses a JSONL (JSON Lines) format for lightweight, queryable local storage.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class ActionEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ActionType { get; set; } = string.Empty; // e.g., "APP_LAUNCH", "CODE_EDIT", "AUDIO_EVENT"
        public string Summary { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty; // Active Window, etc.
        public double Importance { get; set; } = 0.5; // 0.0 to 1.0
    }

    public static class ActionJournalManager
    {
        private static readonly string JournalPath = Path.Combine(PathHandler.GetDataDirectory(), "ActionJournal.jsonl");
        private static readonly object _lock = new object();

        public static void LogAction(string type, string summary, string context = "", double importance = 0.5)
        {
            var entry = new ActionEntry
            {
                ActionType = type,
                Summary = summary,
                Context = context,
                Importance = importance
            };

            Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        string json = JsonSerializer.Serialize(entry);
                        File.AppendAllText(JournalPath, json + Environment.NewLine);
                    }
                    catch { }
                }
            });
        }

        public static List<ActionEntry> GetRecentActions(int count = 20)
        {
            var results = new List<ActionEntry>();
            try
            {
                lock (_lock)
                {
                    if (!File.Exists(JournalPath)) return results;
                    var lines = File.ReadLines(JournalPath).Reverse().Take(count);
                    foreach (var line in lines)
                    {
                        var entry = JsonSerializer.Deserialize<ActionEntry>(line);
                        if (entry != null) results.Add(entry);
                    }
                }
            }
            catch { }
            return results;
        }

        public static string GetJournalSummaryForAi()
        {
            var recent = GetRecentActions(10);
            if (recent.Count == 0) return "No recent significant actions recorded.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("## RECENT USER ACTION JOURNAL");
            foreach (var act in recent)
            {
                sb.AppendLine($"- [{act.Timestamp:HH:mm}] {act.ActionType}: {act.Summary}");
            }
            return sb.ToString();
        }
    }
}
