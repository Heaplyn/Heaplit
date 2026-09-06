// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles commands to dynamically adjust the launcher HUD window's opacity level.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class OpacityCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string cmd = parts[0];
            return SearchUtil.IsClose(cmd, "opacity") || SearchUtil.IsClose(cmd, "opac");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0];
            double similarity = SearchUtil.GetSimilarity(cmd, "opacity");

            if (parts.Length > 1 && int.TryParse(parts[1], out int targetOpacity))
            {
                targetOpacity = Math.Clamp(targetOpacity, 10, 100); // Keep it visible (min 10%)
                suggestions.Add(new CommandResult
                {
                    TITLE = $"Set HUD Opacity to {targetOpacity}%",
                    DESCRIPTION = $"Adjust the launcher transparency",
                    EXECUTE = () => SetWindowOpacity(targetOpacity),
                    SIMILARITY = similarity
                });
            }
            else
            {
                // Default suggestion if no number is typed yet
                suggestions.Add(new CommandResult
                {
                    TITLE = "Set HUD Opacity...",
                    DESCRIPTION = "Type a percentage (e.g. 'opacity 80')",
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }

            return suggestions;
        }

        private static void SetWindowOpacity(int percentage)
        {
            try
            {
                var mainWin = System.Windows.Application.Current.MainWindow;
                if (mainWin != null)
                {
                    mainWin.Opacity = percentage / 100.0;
                }
            }
            catch
            {
                // Fail-safe
            }
        }

        public void OnStart()
        {
            SetWindowOpacity(100); // Default to 100% opacity on startup
        }
    }
}
