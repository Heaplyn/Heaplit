// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to preview, select, and persistently load Heaplit launcher window visual themes.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class ThemeCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "theme");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            string currentTheme = SettingsManager.Current.THEME;

            if (parts.Length > 1)
            {
                string targetTheme = parts[1].Trim().ToLower();
                suggestions.Add(new CommandResult
                {
                    TITLE       = $"Apply Theme: {targetTheme}",
                    DESCRIPTION = $"Switch visual layout style to {targetTheme} accents",
                    SIMILARITY  = (SearchUtil.BestSimilarity(query, "theme") + 2.0 * 0.01), // High priority match
                    EXECUTE     = () => ChangeTheme(targetTheme)
                });
            }
            else
            {
                // List all available themes
                var themes = new string[] { 
                    "purple", "dark", "blue", "green", "cyberpunk", "glass",
                    "dracula", "sunset", "crimson", "gold", "nordic" 
                };
                foreach (var th in themes)
                {
                    bool isCurrent = th.Equals(currentTheme, StringComparison.OrdinalIgnoreCase);
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"Theme: {th}" + (isCurrent ? " (active)" : ""),
                        DESCRIPTION = $"Switch HUD appearance to {th} theme style",
                        SIMILARITY  = (SearchUtil.BestSimilarity(query, "theme") + 1.0 * 0.01),
                        EXECUTE     = () => ChangeTheme(th)
                    });
                }
            }

            return suggestions;
        }

        private static void ChangeTheme(string themeName)
        {
            try
            {
                ThemeManager.ApplyTheme(themeName);
                
                // Save persistently in Settings JSON
                SettingsManager.Current.THEME = themeName;
                SettingsManager.Save();

                TextOverlay.Show($"🎨 Theme Switched to {themeName.ToUpper()}", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to apply theme: {ex.Message}", 3000);
            }
        }
    }
}
