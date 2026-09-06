// Developer: heaplyn
// Date: 2026-08-09
// Summary: Executes direct CLI commands asynchronously via cmd.exe and feeds the results to the terminal overlay.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class CliRunnerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim();
            return query.StartsWith(">") && query.Length > 1;
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            string command = query.Substring(1).Trim();

            if (!string.IsNullOrEmpty(command))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"Run: {command}",
                    DESCRIPTION = "Execute in background cmd and display output overlay",
                    EXECUTE = () => ExecuteCommandAsync(command),
                    SIMILARITY = 2.0
                });
            }

            return suggestions;
        }

        private static void ExecuteCommandAsync(string command)
        {
            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();

                            string finalResult = output;
                            if (!string.IsNullOrEmpty(error))
                            {
                                finalResult += "\n--- ERRORS ---\n" + error;
                            }

                            // Launch retro terminal output overlay directly
                            CliOutputOverlay.Show(command, finalResult);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CliOutputOverlay.Show(command, $"Failed to execute: {ex.Message}");
                }
            });
        }
    }
}
