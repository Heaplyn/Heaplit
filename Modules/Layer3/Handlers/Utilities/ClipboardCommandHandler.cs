// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to check, clear, or browse history of system clipboard.

using System;
using System.Collections.Generic;
using System.Windows;

namespace HeaplitLauncher
{
    public class ClipboardCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "clipboard", "cb", "clip", "clearclip", "cliphistory");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string mainCmd = parts[0].ToLower();

            double similarity = Math.Max(
                SearchUtil.GetSimilarity(mainCmd, "clipboard"),
                SearchUtil.GetSimilarity(mainCmd, "cb")
            );

            string filter = parts.Length > 1 ? parts[1].ToLower() : string.Empty;

            var history = ClipboardHistoryManager.GetHistory();

            if (history.Count > 0)
            {
                foreach (var item in history)
                {
                    string singleLine = item.Content.Replace("\r", " ").Replace("\n", " ");
                    if (!string.IsNullOrEmpty(filter) && !singleLine.ToLower().Contains(filter))
                    {
                        continue;
                    }

                    string preview = singleLine.Length > 60 ? singleLine.Substring(0, 60) + "..." : singleLine;
                    string capturedTime = item.Timestamp.ToString("HH:mm:ss");

                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"📋 [{capturedTime}] {preview}",
                        DESCRIPTION = "Click / Press Enter to copy back into active clipboard",
                        SIMILARITY  = similarity + 0.8,
                        EXECUTE     = () => CopyItemToClipboard(item.Content)
                    });
                }
            }

            // Standard commands
            suggestions.Add(new CommandResult
            {
                TITLE       = "📋 Open Visual Clipboard History",
                DESCRIPTION = "Browse, search, delete, and pin clipboard clips in a GUI window",
                SIMILARITY  = similarity + 2.0, // High priority
                EXECUTE     = () => ClipboardOverlay.Open()
            });

            suggestions.Add(new CommandResult
            {
                TITLE       = "🧹 Clear Clipboard History",
                DESCRIPTION = "Empty system clipboard and local history log",
                SIMILARITY  = similarity + 0.1,
                EXECUTE     = () => ClearClipboard()
            });

            return suggestions;
        }

        private static void CopyItemToClipboard(string content)
        {
            try
            {
                Clipboard.SetText(content);
                TextOverlay.Show("📋 Copied item back to clipboard!", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to copy: {ex.Message}", 3000);
            }
        }

        private static void ClearClipboard()
        {
            try
            {
                Clipboard.Clear();
                ClipboardHistoryManager.ClearHistory();
                TextOverlay.Show("🧹 Clipboard & history cleared!", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to clear: {ex.Message}", 3000);
            }
        }
    }
}

