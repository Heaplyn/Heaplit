// Developer: heaplyn
// Date: 2026-08-18
// Summary: Handles CLI commands to get or set system settings, API keys, and UI options.

using System;
using System.Collections.Generic;
using System.Linq;

namespace HeaplitLauncher
{
    public class SettingsCommandHandler : ICommandHandler
    {
        public List<string> Aliases = new List<string> {
            "settings",
            "options",
            "config",
            "setup"
        };
        
        public bool CanHandle(string query)
        {
            string q = query.Trim().ToLower();
            foreach (string Member in Aliases)
            {
                if (q == Member || SearchUtil.IsClose(q, Member))
                Console.WriteLine(q);
                    return true;
            }
            return SearchUtil.IsClose(q,"settings") || Aliases.Any(a => q == a || SearchUtil.IsClose(q, a)) ||
                   q.StartsWith("ontop") || q.StartsWith("topmost") || q.StartsWith("alwaysontop") ||
                   q.StartsWith("opacity") || q.StartsWith("alpha") || q == "sleep" || q == "wake" ||
                   q.StartsWith("setkey") || q.StartsWith("apikey") || q.StartsWith("obsidian");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string lowerQuery = query.Trim().ToLower();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();

            if (lowerQuery == "settings" || lowerQuery == "options" || lowerQuery == "config")
            {
                suggestions.Add(new CommandResult {
                    TITLE = "⚙️ Open Master Settings Studio",
                    DESCRIPTION = "Configure AI, Themes, Voice ID, and HUD behavior",
                    SIMILARITY = 10.0,
                    EXECUTE = () => SettingsOverlay.ShowOverlay()
                });
                return suggestions;
            }

            if (cmd == "obsidian" && parts.Length > 2 && parts[1] == "path")
            {
                string path = query.Substring(query.IndexOf(parts[2])).Trim();
                suggestions.Add(new CommandResult {
                    TITLE = "Set Obsidian Vault Path",
                    DESCRIPTION = $"New path: {path}",
                    SIMILARITY = 9.0,
                    EXECUTE = () => { SettingsManager.Current.OBSIDIAN_VAULT_PATH = path; SettingsManager.Save(); TextOverlay.Show("✅ Obsidian path updated.", 2500); }
                });
            }

            if (cmd == "ontop")
            {
                bool current = SettingsManager.Current.ALWAYS_ON_TOP;
                suggestions.Add(new CommandResult {
                    TITLE = $"📌 Toggle Always On Top (Currently: {(current ? "On" : "Off")})",
                    DESCRIPTION = $"Switch Always On Top to {!current}",
                    SIMILARITY = 8.5,
                    EXECUTE = () => SetAlwaysOnTop(!current)
                });
            }

            return suggestions;
        }

        private static void SetAlwaysOnTop(bool value)
        {
            try {
                SettingsManager.Current.ALWAYS_ON_TOP = value;
                SettingsManager.Save();
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    foreach (System.Windows.Window win in System.Windows.Application.Current.Windows) {
                        if (win is BaseOverlay bo) bo.Topmost = value;
                    }
                });
                TextOverlay.Show($"📌 Always On Top {(value ? "Enabled" : "Disabled")}", 2500);
            } catch { }
        }

        public List<CommandDesc> GetCommandDescriptions() => new List<CommandDesc> {
            new CommandDesc("options", "Open configuration studio", "options"),
            new CommandDesc("ontop", "Toggle window topmost", "ontop"),
            new CommandDesc("obsidian path <path>", "Set Obsidian vault", "obsidian path C:\\docs")
        };
    }
}
