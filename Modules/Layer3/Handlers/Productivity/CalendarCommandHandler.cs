// Developer: copilot
// Date: 2026-08-13
// Summary: Handles CLI commands to open/view the Calendar GUI or quickly add events directly via the input bar.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HeaplitLauncher
{
    public class CalendarCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "cal", "calendar");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();
            double similarity = SearchUtil.BestSimilarity(query, "cal", "calendar");

            // Check if user is typing a direct event logging statement: calendar add yyyy-MM-dd hh:mm event name
            var addMatch = Regex.Match(query, @"^(?:calendar|cal)\s+add\s+(\d{4}-\d{2}-\d{2})\s+(\S+)\s+(.+)$", RegexOptions.IgnoreCase);
            if (addMatch.Success)
            {
                string dateStr = addMatch.Groups[1].Value;
                string timeStr = addMatch.Groups[2].Value;
                string title = addMatch.Groups[3].Value.Trim();

                suggestions.Add(new CommandResult
                {
                    TITLE = $"📅 Create Event: \"{title}\" on {dateStr} at {timeStr}",
                    DESCRIPTION = "Add a calendar event directly into the Heaplit Planner database",
                    SIMILARITY = similarity + 1.0,
                    EXECUTE = () => CalendarOverlay.LogEvent(title, dateStr, timeStr)
                });
                return suggestions;
            }

            // Suggest opening the overlay
            suggestions.Add(new CommandResult
            {
                TITLE = "📅 Open Calendar Overlay",
                DESCRIPTION = "Launch Heaplit visual Month Calendar and daily planners",
                SIMILARITY = similarity,
                EXECUTE = () => CalendarOverlay.Open()
            });

            // Help info hint
            suggestions.Add(new CommandResult
            {
                TITLE = "calendar add [yyyy-mm-dd] [time] [event details]...",
                DESCRIPTION = "Quickly log a calendar event (e.g. cal add 2026-08-15 14:00 Standup meeting)",
                SIMILARITY = similarity - 0.5,
                EXECUTE = null
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("calendar / cal", "Open calendar overlay or log events directly", "cal add 2026-08-15 14:00 Meetup")
            };
        }
    }
}
