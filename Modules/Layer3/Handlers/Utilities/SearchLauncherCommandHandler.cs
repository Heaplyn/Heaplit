// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to query Google Search directly inside the default system web browser.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HeaplitLauncher
{
    public class SearchLauncherCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query.StartsWith("google ") || query.StartsWith("search ");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                string searchTerms = parts[1].Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE       = $"Google: {searchTerms}",
                    DESCRIPTION = "Open Google search results in your default browser",
                    SIMILARITY  = 2.0, // High priority match
                    EXECUTE     = () => LaunchSearch(searchTerms)
                });
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE       = "Google Search...",
                    DESCRIPTION = "Type a search query (e.g. google WPF window styling)",
                    SIMILARITY  = 1.0,
                    EXECUTE     = null
                });
            }

            return suggestions;
        }

        private static void LaunchSearch(string searchTerms)
        {
            try
            {
                UserActivityContextManager.TrackUserQuery(searchTerms);
                string escapedQuery = Uri.EscapeDataString(searchTerms);
                string url = $"https://www.google.com/search?q={escapedQuery}";

                Process.Start(new ProcessStartInfo
                {
                    FileName        = url,
                    UseShellExecute = true
                });
                TextOverlay.Show($"🔍 Searching Google: \"{searchTerms}\"", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Search failed: {ex.Message}", 3000);
            }
        }
    }
}
