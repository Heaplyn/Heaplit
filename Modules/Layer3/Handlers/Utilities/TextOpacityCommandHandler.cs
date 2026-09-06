// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles commands to dynamically adjust HUD text element opacities via event broadcasts.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class TextOpacityCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "textopacity", "textopac");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();
            double similarity = SearchUtil.GetSimilarity(cmd, "textopacity");

            if (parts.Length > 1 && int.TryParse(parts[1], out int targetOpacity))
            {
                targetOpacity = Math.Clamp(targetOpacity, 10, 100);
                suggestions.Add(new CommandResult
                {
                    TITLE = $"Set Text Opacity to {targetOpacity}%",
                    DESCRIPTION = "Adjust the opacity of search box and result lists",
                    EXECUTE = () => TriggerChange(targetOpacity),
                    SIMILARITY = similarity
                });
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "Set Text Opacity...",
                    DESCRIPTION = "Type a percentage (e.g. 'textopacity 70')",
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }

            return suggestions;
        }

        private static void TriggerChange(int percentage)
        {
            double opacityValue = percentage / 100.0;
            
            // Broadcasts the event to whoever is listening (Layer 4)
            CommandParser.TriggerTextOpacityChange(opacityValue);
            
            TextOverlay.Show($"📝 Text opacity set to {percentage}%", 2000);
        }
    }
}
