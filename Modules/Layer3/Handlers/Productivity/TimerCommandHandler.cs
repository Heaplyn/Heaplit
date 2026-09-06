// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles timer setup commands, launching background alert triggers on completion.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace HeaplitLauncher
{
    public class TimerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string cmd = parts[0];
            return SearchUtil.IsClose(cmd, "timer") || SearchUtil.IsClose(cmd, "time");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0];
            double similarity = SearchUtil.GetSimilarity(cmd, "timer");

            if (parts.Length > 1)
            {
                string timeStr = parts[1];
                int totalSeconds = ParseTimeToSeconds(timeStr);

                if (totalSeconds > 0)
                {
                    string durationText = FormatDurationText(totalSeconds);
                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"Start Timer for {durationText}",
                        DESCRIPTION = $"Triggers a HUD overlay notification when finished",
                        EXECUTE = () => StartTimer(totalSeconds, durationText),
                        SIMILARITY = similarity
                    });
                    return suggestions;
                }
            }

            // Default suggestion if no valid time typed yet
            suggestions.Add(new CommandResult
            {
                TITLE = "Start Timer...",
                DESCRIPTION = "Type a duration (e.g. 'timer 5' or 'timer 30s')",
                EXECUTE = null,
                SIMILARITY = similarity
            });

            return suggestions;
        }

        private static int ParseTimeToSeconds(string input)
        {
            var match = Regex.Match(input, @"^(\d+)([sm]?)$", RegexOptions.IgnoreCase);
            if (!match.Success) return 0;

            int value = int.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value.ToLower();

            if (unit == "s") return value;
            // Default to minutes if 'm' or no unit is provided
            return value * 60;
        }

        private static string FormatDurationText(int totalSeconds)
        {
            if (totalSeconds < 60) return $"{totalSeconds} second{(totalSeconds > 1 ? "s" : "")}";
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            if (seconds == 0) return $"{minutes} minute{(minutes > 1 ? "s" : "")}";
            return $"{minutes}m {seconds}s";
        }

        private static void StartTimer(int seconds, string durationText)
        {
            // Instantly notify timer start
            TextOverlay.Show($"⏰ Timer Started for {durationText}", 2000);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                TextOverlay.Show($"⏰ TIMER FINISHED!\n{durationText} has elapsed.", 5000);
            };
            timer.Start();
        }
    }
}
