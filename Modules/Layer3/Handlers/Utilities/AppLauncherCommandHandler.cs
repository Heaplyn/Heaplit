// Developer: heaplyn
// Date: 2026-08-08
// Summary: Catch-all process launching handler that executes commands, URL links, and shortcut references via cmd shell.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HeaplitLauncher
{
    public class AppLauncherCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            string q = query.Trim().ToLower();

            if (q == "index apps" || q == "reindex" || q == "refresh apps") return true;

            // Do not catch plain conversational English sentences
            if (q.Split(' ').Length > 3 && !q.Contains(":\\") && !q.Contains("/") && !q.Contains("."))
                return false;

            // Only catch things that look like paths, executables, URLs, or very short single words
            return q.Contains(":\\") || q.Contains("/") || q.Contains(".") || q.Length < 15;
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim().ToLower();

            if (q == "index apps" || q == "reindex" || q == "refresh apps")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔄 Re-Index Windows Applications",
                    DESCRIPTION = "Force a full scan of installed apps and Start Menu shortcuts",
                    SIMILARITY = 8.0,
                    EXECUTE = () => {
                        TextOverlay.Show("🔄 Re-indexing system applications...", 3000);
                        WindowsAppScanner.IndexApplicationsGlobal(true);
                    }
                });
                return suggestions;
            }

            query = query.Trim();

            suggestions.Add(new CommandResult
            {
                TITLE = $"Run: {query}",
                DESCRIPTION = $"Execute '{query}' via Windows Shell",
                EXECUTE = () => LaunchProcess(query),
                SIMILARITY = 0.05
            });

            return suggestions;
        }

        private static void LaunchProcess(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" {command}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                if (command.Contains("://") || command.EndsWith(".exe") || command.EndsWith(".lnk"))
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = command,
                        UseShellExecute = true
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = command,
                        UseShellExecute = true
                    };
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to launch process: {ex.Message}");
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start {command}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                catch
                {
                    // Swallow
                }
            }
        }
    }
}
