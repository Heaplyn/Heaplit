// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles quick note appending (`note <text>`) and reminder popups (`remind <time> <msg>`).

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class ProductivityCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "note" || query.StartsWith("note ") || 
                   query == "remind" || query.StartsWith("remind ");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLower();

            if (cmd == "note")
            {
                if (parts.Length > 1)
                {
                    string text = parts[1];
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"Append Note: \"{text}\"",
                        DESCRIPTION = "Save quick timestamped entry into notes.txt",
                        SIMILARITY  = 2.0,
                        EXECUTE     = () => AppendNote(text)
                    });
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "Quick Note (Prompt)...",
                        DESCRIPTION = "Prompt for text entry to append into notes.txt",
                        SIMILARITY  = 1.5,
                        EXECUTE     = () => InputPromptOverlay.Show("Enter note text to save:", (text) => AppendNote(text))
                    });

                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "📓 Open Notes Studio",
                        DESCRIPTION = "Manage all hierarchical notes and categories",
                        SIMILARITY  = 1.0,
                        EXECUTE     = () => NoteManagerOverlay.ShowOverlay()
                    });
                }
            }
            else if (cmd == "remind")
            {
                if (parts.Length > 1)
                {
                    string args = parts[1];
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"Set Reminder: {args}",
                        DESCRIPTION = "e.g. '10s Check oven' or '5m Take a break'",
                        SIMILARITY  = 2.0,
                        EXECUTE     = () => ParseAndSetReminder(args)
                    });
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "Set Reminder (Prompt)...",
                        DESCRIPTION = "Prompt for reminder format: <duration> <message> (e.g. 5m Take break)",
                        SIMILARITY  = 1.5,
                        EXECUTE     = () => InputPromptOverlay.Show("Enter reminder (e.g. 10s Take break, 5m Rest):", (args) => ParseAndSetReminder(args))
                    });
                }
            }

            return suggestions;
        }

        private static void AppendNote(string noteText)
        {
            try
            {
                string relativePath = "Quick Notes.txt";
                string content = NotesManager.LoadNote(relativePath);
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {noteText}{Environment.NewLine}";
                NotesManager.SaveNote(relativePath, content + entry);
                TextOverlay.Show("📝 Note saved to Quick Notes!", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to save note: {ex.Message}", 3000);
            }
        }

        private static void ParseAndSetReminder(string args)
        {
            var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                TextOverlay.Show("⚠️ Invalid format! Use: remind <time><s|m|h> <message>", 3500);
                return;
            }

            string timeStr = parts[0].ToLower();
            string message = parts[1];

            int seconds = 0;
            if (timeStr.EndsWith("s") && int.TryParse(timeStr.TrimEnd('s'), out int s))
            {
                seconds = s;
            }
            else if (timeStr.EndsWith("m") && int.TryParse(timeStr.TrimEnd('m'), out int m))
            {
                seconds = m * 60;
            }
            else if (timeStr.EndsWith("h") && int.TryParse(timeStr.TrimEnd('h'), out int h))
            {
                seconds = h * 3600;
            }
            else if (int.TryParse(timeStr, out int defaultSec))
            {
                seconds = defaultSec;
            }
            else
            {
                TextOverlay.Show("⚠️ Invalid time duration (e.g. 30s, 5m, 1h)", 3500);
                return;
            }

            TextOverlay.Show($"⏰ Reminder set for {timeStr} from now", 2500);

            Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    TextOverlay.Show($"🔔 REMINDER: {message}", 6000);
                });
            });
        }

        private static string GetProjectRoot()
        {
            string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            if (Directory.Exists(Path.Combine(devPath, "Modules")))
            {
                return devPath;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("note <text>", "Quickly append text to notes.txt", "note Meeting at 3pm"),
                new CommandDesc("remind <time> <msg>", "Set popup alert timer (e.g. 5m, 30s)", "remind 10m Break")
            };
        }
    }
}
