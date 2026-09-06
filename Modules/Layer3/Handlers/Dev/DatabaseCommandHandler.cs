// Developer: heaplyn
// Date: 2026-08-16
// Summary: Handles system database operations including resetting memory, filtering by importance, and maintenance.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class DatabaseCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query.StartsWith("db ") || query.StartsWith("database ") || query == "reset db" || query == "filter db";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string lower = query.Trim().ToLower();
            string[] parts = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double similarity = SearchUtil.GetSimilarity(parts[0], "database");

            // ── RESET ───────────────────────────────────────────────────────────
            if (lower.Contains("reset"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🧹 Reset Semantic Memory Database",
                    DESCRIPTION = "Wipe all long-term AI facts and activity history",
                    SIMILARITY = 9.0,
                    EXECUTE = () => ResetMemory()
                });

                suggestions.Add(new CommandResult
                {
                    TITLE = "🎙️ Reset Voice Dataset",
                    DESCRIPTION = "Delete all captured voice clips and trigger logs",
                    SIMILARITY = 8.5,
                    EXECUTE = () => ResetVoice()
                });
            }

            // ── FILTER ──────────────────────────────────────────────────────────
            if (lower.Contains("filter") || parts.Length >= 2 && double.TryParse(parts.Last(), out _))
            {
                double pct = 50;
                if (parts.Length >= 3 && double.TryParse(parts[2], out double p)) pct = p;
                else if (parts.Length == 2 && double.TryParse(parts[1], out double p2)) pct = p2;

                suggestions.Add(new CommandResult
                {
                    TITLE = $"🔍 Filter Memory: {pct}% Importance",
                    DESCRIPTION = $"Prune low-value data. Keep only nodes >= {pct}% score.",
                    SIMILARITY = 9.0,
                    EXECUTE = () => FilterMemory(pct)
                });
            }

            // Default suggestions
            if (suggestions.Count == 0)
            {
                suggestions.Add(new CommandResult { TITLE = "📊 Database Maintenance...", DESCRIPTION = "Use 'db reset' or 'db filter <%>'", SIMILARITY = similarity, EXECUTE = null });
            }

            return suggestions;
        }

        private void ResetMemory()
        {
            var res = System.Windows.MessageBox.Show("Are you sure you want to WIPE all Semantic Memory? This cannot be undone.", "Database Reset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (res == System.Windows.MessageBoxResult.Yes)
            {
                SemanticMemoryManager.ResetDatabase();
                TextOverlay.Show("🧠 Semantic Memory Reset Successful", 3000);
            }
        }

        private void ResetVoice()
        {
            var res = System.Windows.MessageBox.Show("Are you sure you want to delete all historical voice data?", "Voice Reset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (res == System.Windows.MessageBoxResult.Yes)
            {
                VoiceDatasetManager.ResetDatabase();
                TextOverlay.Show("🎙️ Voice Dataset Reset Successful", 3000);
            }
        }

        private void FilterMemory(double percentage)
        {
            int removed = SemanticMemoryManager.FilterByImportance(percentage);
            TextOverlay.Show($"🧹 Filtered Database: Removed {removed} low-importance nodes.", 3500);
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("db reset", "Wipe AI long-term memory", "db reset"),
                new CommandDesc("db filter <%>", "Prune low-importance data", "db filter 70")
            };
        }

        public void OnStart() { }
    }
}
