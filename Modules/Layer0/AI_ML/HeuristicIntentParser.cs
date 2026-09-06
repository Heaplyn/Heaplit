// Developer: heaplyn
// Date: 2026-08-16
// Summary: Fast Local Heuristic Intent Parser.
//          Bypasses the LLM for common system commands using Regex and Keyword analysis.
//          Enables basic functionality even when LLM backends are down or offline.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class HeuristicIntentParser
    {
        public static async Task<string?> TryHandleLocallyAsync(string query)
        {
            string q = query.ToLower().Trim();

            // 1. App Launching (e.g. "open notepad", "launch chrome")
            var launchMatch = Regex.Match(q, @"^(?:open|launch|start|run)\s+(?<app>.+)$");
            if (launchMatch.Success)
            {
                string app = launchMatch.Groups["app"].Value.Trim();
                var matches = CoreRegistry.System.Apps.GetMatchingApps(app);
                if (matches.Any(m => m.SIMILARITY >= 0.6))
                {
                    var best = matches.OrderByDescending(m => m.SIMILARITY).First();
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        try {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = best.TargetPath, UseShellExecute = true });
                        } catch {
                            System.Diagnostics.Process.Start("cmd.exe", $"/c start \"\" \"{best.TargetPath}\"");
                        }
                    });
                    return $"📱 Done. Launched {best.Name}.";
                }
            }

            // 2. System Power (e.g. "restart", "shutdown")
            if (q == "restart" || q == "reboot") { NativeMethods.Restart(); return "🔄 Restarting system..."; }
            if (q == "shutdown" || q == "power off") { System.Diagnostics.Process.Start("shutdown", "/s /t 0"); return "🛑 Shutting down..."; }

            // 3. Simple Build Intent (e.g. "build this c# project")
            var buildMatch = Regex.Match(q, @"^(?:build|compile)\s+(?:this\s+)?(?<lang>c#|cs|cpp|c\+\+|rust|rs|python|py)\s+project$", RegexOptions.IgnoreCase);
            if (buildMatch.Success)
            {
                string lang = buildMatch.Groups["lang"].Value;
                string root = PathHandler.GetProjectRoot();
                // This is a heuristic guess at the active project root
                _ = Task.Run(async () => await BuildSystemManager.BuildProjectAsync(lang, root));
                return $"🛠️ Initiated {lang.ToUpper()} build for the current project root.";
            }

            // 4. Time/Date
            if (q.Contains("what time") || q == "time") return $"🕒 The current time is {DateTime.Now:h:mm tt}.";
            if (q.Contains("what day") || q == "date") return $"📅 Today is {DateTime.Now:dddd, MMMM d, yyyy}.";

            // 5. Volume
            var volMatch = Regex.Match(q, @"^volume\s+(?<val>\d+)$");
            if (volMatch.Success)
            {
                CommandParser.ExecuteFirstSuggestion(q);
                return $"🔊 Volume set to {volMatch.Groups["val"].Value}%.";
            }

            return null; // Let LLM handle it
        }
    }
}
