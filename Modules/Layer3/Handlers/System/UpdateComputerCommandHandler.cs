// Developer: heaplyn
// Summary: Handles CLI commands to check for Windows OS updates or query winget for outdated programs.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class UpdateComputerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "pcupdate" || query == "sysupdate" || query == "update pc" || 
                   query == "update computer" || query == "update system" || 
                   query == "winget check" || query.StartsWith("update") || query.StartsWith("upgrade");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = Math.Max(
                SearchUtil.GetSimilarity(query, "update pc"),
                SearchUtil.GetSimilarity(query, "update computer")
            );

            // Suggestion 1: Check winget program updates
            suggestions.Add(new CommandResult
            {
                TITLE       = "Check Software Updates",
                DESCRIPTION = "Run 'winget upgrade' to scan for outdated desktop programs",
                SIMILARITY  = similarity + 0.1, // Slight priority boost
                EXECUTE     = () => Task.Run(async () => await CheckWingetUpdatesAsync())
            });

            // Suggestion 2: Open Windows settings OS check
            suggestions.Add(new CommandResult
            {
                TITLE       = "Check Windows OS Updates",
                DESCRIPTION = "Launch Windows Update Settings panel to scan for system patches",
                SIMILARITY  = similarity,
                EXECUTE     = () => OpenWindowsUpdateSettings()
            });

            return suggestions;
        }

        private static async Task CheckWingetUpdatesAsync()
        {
            // Display quick loading notification
            TextOverlay.Show("🔍 Scanning for program updates...", 3000);

            var log = new StringBuilder();
            log.AppendLine("===================================================");
            log.AppendLine("            WINDOWS PROGRAM UPDATE CHECK           ");
            log.AppendLine("===================================================");
            log.AppendLine();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "winget.exe",
                    Arguments              = "upgrade",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        process.WaitForExit();

                        if (string.IsNullOrWhiteSpace(output) || output.Contains("No applicable upgrade found"))
                        {
                            log.AppendLine("✅ All desktop applications are up to date!");
                        }
                        else
                        {
                            log.AppendLine(output);
                        }

                        if (!string.IsNullOrEmpty(error))
                        {
                            log.AppendLine("\n--- ERRORS ---\n" + error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.AppendLine($"❌ Error checking winget updates: {ex.Message}");
            }

            // Display results in the persistent retro terminal
            CliOutputOverlay.Show("Software Updates Scan", log.ToString());
        }

        private static void OpenWindowsUpdateSettings()
        {
            try
            {
                TextOverlay.Show("⚙️ Opening Windows Update Settings...", 2500);
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "explorer.exe",
                    Arguments       = "ms-settings:windowsupdate",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to open Settings: {ex.Message}", 3000);
            }
        }
    }
}
