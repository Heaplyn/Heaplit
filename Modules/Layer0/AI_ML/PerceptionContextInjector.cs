// Developer: heaplyn
// Date: 2026-09-02
// Summary: Gathers Heaplit's live "senses" — the active window, the latest screen capture summary,
//          and the project files most relevant to the request — into a compact text block injected
//          into every AI prompt, so the model can reason about what's on screen and in the codebase.
//          Efficiency-aware: skips the heavier work when the system is under load.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HeaplitLauncher
{
    public static class PerceptionContextInjector
    {
        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? "") : s.Substring(0, max) + "…";

        public static string Gather(string prompt)
        {
            try
            {
                if (!CoreRegistry.Data.Settings.Current.ENABLE_PERCEPTION_CONTEXT) return "";

                var sb = new StringBuilder();

                // 1) Active window / process (what the user is looking at right now).
                try
                {
                    string win = ScreenMonitorEngine.ActiveWindowTitle;
                    string proc = ScreenMonitorEngine.ActiveProcessName;
                    if (!string.IsNullOrWhiteSpace(win))
                        sb.AppendLine($"- Active window: \"{win}\"" + (string.IsNullOrWhiteSpace(proc) ? "" : $" ({proc})"));
                }
                catch { }

                // 2) Latest on-screen summary from the periodic screen monitor (if running/recent).
                try
                {
                    if (!string.IsNullOrWhiteSpace(ScreenMonitorEngine.LastAiSummary))
                    {
                        var age = DateTime.Now - ScreenMonitorEngine.LastCaptureTime;
                        if (age.TotalMinutes < 5)
                            sb.AppendLine($"- On screen ({(int)age.TotalSeconds}s ago): {Truncate(ScreenMonitorEngine.LastAiSummary, 700)}");
                    }
                }
                catch { }

                // 3) Project files most relevant to the request (skip under load — it's the heavy bit).
                try
                {
                    if (!NeuralResourceManager.IsThrottled)
                    {
                        var terms = Regex.Matches(prompt.ToLowerInvariant(), @"[a-z0-9_\.]{4,}")
                                         .Select(m => m.Value).Distinct().Take(12).ToList();
                        var files = CoreRegistry.Intelligence.ProjectContext.GetFileSummaries();
                        var matches = files
                            .Where(f => terms.Any(t =>
                                f.FilePath.ToLowerInvariant().Contains(t) ||
                                (f.Summary?.ToLowerInvariant().Contains(t) ?? false)))
                            .Take(5).ToList();
                        if (matches.Count > 0)
                        {
                            sb.AppendLine("- Relevant project files:");
                            foreach (var f in matches)
                                sb.AppendLine($"    • {Path.GetFileName(f.FilePath)} — {Truncate(f.Summary, 160)}");
                        }
                    }
                }
                catch { }

                // 4) Files from the slow filesystem index that match the request (path matches only).
                try
                {
                    if (!NeuralResourceManager.IsThrottled)
                    {
                        var terms = Regex.Matches(prompt, @"[A-Za-z0-9_\.\-]{4,}")
                                         .Select(m => m.Value).Distinct().Take(6);
                        var hits = new List<string>();
                        foreach (var t in terms)
                        {
                            hits.AddRange(FileSystemIndexer.Search(t, 3));
                            if (hits.Count >= 6) break;
                        }
                        hits = hits.Distinct().Take(6).ToList();
                        if (hits.Count > 0)
                        {
                            sb.AppendLine("- Files on disk matching the request:");
                            foreach (var h in hits) sb.AppendLine($"    • {h}");
                        }
                    }
                }
                catch { }

                if (sb.Length == 0) return "";
                return "[PERCEPTION CONTEXT — what Heaplit currently sees / knows]\n" + sb;
            }
            catch { return ""; }
        }
    }
}
