// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles 'open <filePath>' commands to launch files natively with their default associated Windows applications.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace HeaplitLauncher
{
    public class OpenNativeCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "open" || query.StartsWith("open ");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            double similarity = SearchUtil.GetSimilarity(query.ToLower(), "open");

            if (parts.Length > 1)
            {
                string targetPath = parts[1].Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE       = $"Open Natively: {Path.GetFileName(targetPath)}",
                    DESCRIPTION = $"Open \"{targetPath}\" with its default associated Windows application",
                    SIMILARITY  = 2.0, // High priority match
                    EXECUTE     = () => OpenNatively(targetPath)
                });
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE       = "Browse File to Open...",
                    DESCRIPTION = "Open Windows file explorer to select a file to run natively",
                    SIMILARITY  = similarity + 0.6,
                    EXECUTE     = () => PromptAndOpenNatively()
                });

                suggestions.Add(new CommandResult
                {
                    TITLE       = "Open File (Prompt)...",
                    DESCRIPTION = "Enter a file path to launch natively using Windows Shell",
                    SIMILARITY  = similarity + 0.3,
                    EXECUTE     = () => InputPromptOverlay.Show("Enter file path to open natively:", (path) => OpenNatively(path))
                });
            }

            return suggestions;
        }

        private static void OpenNatively(string filePath)
        {
            try
            {
                // Resolve relative path if not absolute
                string projectRoot = GetProjectRoot();
                string absolutePath = Path.IsPathRooted(filePath) 
                    ? filePath 
                    : Path.GetFullPath(Path.Combine(projectRoot, filePath));

                Process.Start(new ProcessStartInfo
                {
                    FileName = absolutePath,
                    UseShellExecute = true
                });
                TextOverlay.Show($"🚀 Opening: {Path.GetFileName(absolutePath)}", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Open failed: {ex.Message}", 3000);
            }
        }

        private static void PromptAndOpenNatively()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open File Natively",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenNatively(openFileDialog.FileName);
            }
        }

        private static string GetProjectRoot()
        {
            string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            if (Directory.Exists(Path.Combine(devPath, "Modules")))
            {
                return devPath;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
