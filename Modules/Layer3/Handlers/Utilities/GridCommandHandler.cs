// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles commands to display the visual file launchpad grid overlay (grid / files) and manage pinned file entries (pin / unpin).

using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher
{
    public class GridCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "grid", "files", "pin", "unpin");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();
            double similarity = SearchUtil.GetSimilarity(cmd, "grid");

            if (cmd == "grid" || cmd == "files")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE       = "File Launchpad Grid",
                    DESCRIPTION = "Open visual dashboard layout of saved/pinned files",
                    SIMILARITY  = similarity + 1.0,
                    EXECUTE     = () => FileGridOverlay.OpenDashboard()
                });
            }
            else if (cmd == "pin")
            {
                if (parts.Length > 1)
                {
                    string targetPath = parts[1].Trim();
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"Pin File: {Path.GetFileName(targetPath)}",
                        DESCRIPTION = $"Pin \"{targetPath}\" persistently to the File Launchpad Dashboard",
                        SIMILARITY  = (SearchUtil.BestSimilarity(query, "grid", "files", "pin", "unpin") + 2.0 * 0.01),
                        EXECUTE     = () => PinFileNatively(targetPath)
                    });
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "Pin File (Prompt)...",
                        DESCRIPTION = "Type a local file path to pin to your file launchpad grid",
                        SIMILARITY  = similarity + 0.6,
                        EXECUTE     = () => InputPromptOverlay.Show("Enter file path to pin:", (path) => PinFileNatively(path))
                    });

                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "Browse File to Pin...",
                        DESCRIPTION = "Open Windows file explorer to select a file to pin",
                        SIMILARITY  = similarity + 0.3,
                        EXECUTE     = () => PromptAndPinFile()
                    });
                }
            }
            else if (cmd == "unpin")
            {
                if (parts.Length > 1)
                {
                    string targetPath = parts[1].Trim();
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"Unpin File: {Path.GetFileName(targetPath)}",
                        DESCRIPTION = $"Remove \"{targetPath}\" from the File Launchpad Dashboard",
                        SIMILARITY  = (SearchUtil.BestSimilarity(query, "grid", "files", "pin", "unpin") + 2.0 * 0.01),
                        EXECUTE     = () => FileGridOverlay.UnpinFile(targetPath)
                    });
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "Unpin File (Prompt)...",
                        DESCRIPTION = "Type a file path to unpin from your file grid dashboard",
                        SIMILARITY  = similarity + 0.5,
                        EXECUTE     = () => InputPromptOverlay.Show("Enter file path to unpin:", (path) => FileGridOverlay.UnpinFile(path))
                    });
                }
            }

            return suggestions;
        }

        private static void PinFileNatively(string filePath)
        {
            string projectRoot = GetProjectRoot();
            string absolutePath = Path.IsPathRooted(filePath) 
                ? filePath 
                : Path.GetFullPath(Path.Combine(projectRoot, filePath));

            FileGridOverlay.PinFile(absolutePath);
        }

        private static void PromptAndPinFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select File to Pin",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FileGridOverlay.PinFile(openFileDialog.FileName);
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

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("grid / files", "View pinned files launchpad grid", "grid"),
                new CommandDesc("pin <filename>", "Pin file to launchpad dashboard", "pin C:\\notes.txt"),
                new CommandDesc("unpin <filename>", "Remove file from launchpad grid", "unpin C:\\notes.txt")
            };
        }
    }
}
