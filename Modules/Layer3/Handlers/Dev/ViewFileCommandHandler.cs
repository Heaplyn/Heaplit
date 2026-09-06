// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles reading a text file and displaying it in the scrollable console overlay.

using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher
{
    public class ViewFileCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string cmd = parts[0];
            return SearchUtil.IsClose(cmd, "view") || SearchUtil.IsClose(cmd, "read");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0];
            double similarity = SearchUtil.GetSimilarity(cmd, "view");

            if (parts.Length > 1)
            {
                // Grab everything after the "view " keyword as the file path
                string filePath = query.Substring(cmd.Length).Trim();

                // Strip quotes if they dragged and dropped a file into the terminal
                filePath = filePath.Trim('"', '\'');

                suggestions.Add(new CommandResult
                {
                    TITLE = $"View File: {Path.GetFileName(filePath)}",
                    DESCRIPTION = $"Open and read contents of '{filePath}'",
                    EXECUTE = () => ReadAndDisplayFile(filePath),
                    SIMILARITY = similarity
                });
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "View File...",
                    DESCRIPTION = "Type a path (e.g. 'view C:\\temp\\log.txt')",
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }

            return suggestions;
        }

        private static void ReadAndDisplayFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    string ext = Path.GetExtension(path).ToLower();

                    if (ext == ".md" || ext == ".json" || ext == ".log")
                    {
                        ContentPreviewOverlay.Show(Path.GetFileName(path), content);
                    }
                    else
                    {
                        // Reuses the scrollable console overlay from Layer 2!
                        CliOutputOverlay.Show(Path.GetFileName(path), content);
                    }
                }
                else
                {
                    TextOverlay.Show($"⚠️ File not found:\n{path}", 3000);
                }
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Error reading file:\n{ex.Message}", 4000);
            }
        }
    }
}