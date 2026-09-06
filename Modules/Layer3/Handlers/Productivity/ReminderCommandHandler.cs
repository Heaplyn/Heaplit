// Developer: copilot
// Date: 2026-08-13
// Summary: Handles CLI commands to schedule relative/absolute reminders, display active reminders in the console, or delete them.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HeaplitLauncher
{
    public class ReminderCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "remind", "reminders");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();
            double similarity = SearchUtil.BestSimilarity(query, "remind", "reminders");

            if (parts.Length > 1)
            {
                string action = parts[1].ToLower();

                // List reminders
                if (action == "list")
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE = "🔔 View Active Reminders",
                        DESCRIPTION = "Display all currently pending alarms and reminders in the console",
                        SIMILARITY = similarity + 0.5,
                        EXECUTE = () => ListReminders()
                    });
                    return suggestions;
                }

                // Delete reminder
                if ((action == "delete" || action == "remove") && parts.Length > 2)
                {
                    if (int.TryParse(parts[2], out int idx))
                    {
                        suggestions.Add(new CommandResult
                        {
                            TITLE = $"🗑️ Delete Reminder #{idx}",
                            DESCRIPTION = "Remove this active reminder from the system scheduler",
                            SIMILARITY = similarity + 0.5,
                            EXECUTE = () => DeleteReminder(idx)
                        });
                        return suggestions;
                    }
                }
            }

            // --- REMINDER PARSING REVALUATION ---
            // Pattern 1: remind me in 10m to check the oven OR remind in 10m to check the oven
            var relativeMatch1 = Regex.Match(query, @"^(?:remind\s+me\s+in|remind\s+in)\s+(\d+)\s*([smh])\s+(?:to\s+)?(.+)$", RegexOptions.IgnoreCase);
            // Pattern 2: remind me to check the oven in 10m
            var relativeMatch2 = Regex.Match(query, @"^remind\s+me\s+to\s+(.+)\s+in\s+(\d+)\s*([smh])$", RegexOptions.IgnoreCase);

            if (relativeMatch1.Success)
            {
                int val = int.Parse(relativeMatch1.Groups[1].Value);
                string unit = relativeMatch1.Groups[2].Value.ToLower();
                string msg = relativeMatch1.Groups[3].Value.Trim();
                DateTime target = CalculateRelativeTime(val, unit);

                suggestions.Add(new CommandResult
                {
                    TITLE = $"🔔 Remind in {val}{unit}: \"{msg}\"",
                    DESCRIPTION = $"Set alert timer to fire on {target:HH:mm:ss}",
                    SIMILARITY = similarity + 1.0,
                    EXECUTE = () => ScheduleReminder(msg, target)
                });
                return suggestions;
            }
            else if (relativeMatch2.Success)
            {
                string msg = relativeMatch2.Groups[1].Value.Trim();
                int val = int.Parse(relativeMatch2.Groups[2].Value);
                string unit = relativeMatch2.Groups[3].Value.ToLower();
                DateTime target = CalculateRelativeTime(val, unit);

                suggestions.Add(new CommandResult
                {
                    TITLE = $"🔔 Remind in {val}{unit}: \"{msg}\"",
                    DESCRIPTION = $"Set alert timer to fire on {target:HH:mm:ss}",
                    SIMILARITY = similarity + 1.0,
                    EXECUTE = () => ScheduleReminder(msg, target)
                });
                return suggestions;
            }

            // Pattern 3: remind me to check mail at 15:30 OR remind me at 15:30 to check mail
            var absoluteMatch1 = Regex.Match(query, @"^remind\s+me\s+to\s+(.+)\s+at\s+(\d{1,2}:\d{2})$", RegexOptions.IgnoreCase);
            var absoluteMatch2 = Regex.Match(query, @"^(?:remind\s+me\s+at|remind\s+at)\s+(\d{1,2}:\d{2})\s+(?:to\s+)?(.+)$", RegexOptions.IgnoreCase);

            if (absoluteMatch1.Success)
            {
                string msg = absoluteMatch1.Groups[1].Value.Trim();
                string timeStr = absoluteMatch1.Groups[2].Value;
                DateTime target = ParseAbsoluteTime(timeStr);

                suggestions.Add(new CommandResult
                {
                    TITLE = $"🔔 Remind at {timeStr}: \"{msg}\"",
                    DESCRIPTION = $"Set alert scheduler to fire on {target:yyyy-MM-dd HH:mm:ss}",
                    SIMILARITY = similarity + 1.0,
                    EXECUTE = () => ScheduleReminder(msg, target)
                });
                return suggestions;
            }
            else if (absoluteMatch2.Success)
            {
                string timeStr = absoluteMatch2.Groups[1].Value;
                string msg = absoluteMatch2.Groups[2].Value.Trim();
                DateTime target = ParseAbsoluteTime(timeStr);

                suggestions.Add(new CommandResult
                {
                    TITLE = $"🔔 Remind at {timeStr}: \"{msg}\"",
                    DESCRIPTION = $"Set alert scheduler to fire on {target:yyyy-MM-dd HH:mm:ss}",
                    SIMILARITY = similarity + 1.0,
                    EXECUTE = () => ScheduleReminder(msg, target)
                });
                return suggestions;
            }

            // General defaults
            suggestions.Add(new CommandResult
            {
                TITLE = "🔔 View Active Reminders",
                DESCRIPTION = "List all active reminders using 'remind list'",
                SIMILARITY = similarity,
                EXECUTE = () => ListReminders()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "remind me in [duration] to [message]",
                DESCRIPTION = "Examples: 'remind me in 10m to stretch' or 'remind me at 18:00 to turn off PC'",
                SIMILARITY = similarity - 0.5,
                EXECUTE = null
            });

            return suggestions;
        }

        private static DateTime CalculateRelativeTime(int val, string unit)
        {
            var now = DateTime.Now;
            if (unit == "s") return now.AddSeconds(val);
            if (unit == "h") return now.AddHours(val);
            return now.AddMinutes(val); // default 'm'
        }

        private static DateTime ParseAbsoluteTime(string timeStr)
        {
            var now = DateTime.Now;
            var parts = timeStr.Split(':');
            int hour = int.Parse(parts[0]);
            int min = int.Parse(parts[1]);

            var target = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
            if (target <= now)
            {
                // Target is in past today, assume tomorrow
                target = target.AddDays(1);
            }
            return target;
        }

        private static void ScheduleReminder(string msg, DateTime target)
        {
            ReminderManager.AddReminder(msg, target);
            TextOverlay.Show($"🔔 Reminder Scheduled!\n\"{msg}\" at {target:HH:mm:ss}", 3000);
        }

        private static void DeleteReminder(int idx)
        {
            if (ReminderManager.DeleteReminder(idx))
            {
                TextOverlay.Show($"🗑️ Reminder #{idx} deleted.", 2000);
            }
            else
            {
                TextOverlay.Show($"⚠️ Invalid reminder index: {idx}", 3000);
            }
        }

        private static void ListReminders()
        {
            var active = ReminderManager.GetActiveReminders();
            var sb = new StringBuilder();

            sb.AppendLine("===================================================");
            sb.AppendLine("                HEAPLIT REMINDERS LIST              ");
            sb.AppendLine("===================================================");
            sb.AppendLine();

            if (active.Count == 0)
            {
                sb.AppendLine("[No active reminders scheduled. Type 'remind me in 5m to test' to set one.]");
            }
            else
            {
                for (int i = 0; i < active.Count; i++)
                {
                    var r = active[i];
                    sb.AppendLine($"{i + 1}. {r.TargetTime:yyyy-MM-dd HH:mm:ss} - {r.Message}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("---------------------------------------------------");
            sb.AppendLine("Commands:");
            sb.AppendLine("- remind me in <time> to <content> : Add a reminder");
            sb.AppendLine("- remind me at <time> to <content> : Add a reminder");
            sb.AppendLine("- remind delete <index>            : Delete a reminder");

            CliOutputOverlay.Show("Reminders List", sb.ToString());
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("remind me to [msg] in [time]", "Schedule an alert reminder", "remind me in 10m to stretching")
            };
        }
    }
}
