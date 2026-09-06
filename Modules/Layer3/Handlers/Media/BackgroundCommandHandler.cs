using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class BackgroundCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "background", "bg", "gif");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var results = new List<CommandResult>();
            string[] parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                results.Add(new CommandResult
                {
                    TITLE = "🖼️ Background Mode: Gradient",
                    DESCRIPTION = "Switch to animated liquid gradient background",
                    EXECUTE = () => SetBackgroundMode("Gradient")
                });
                results.Add(new CommandResult
                {
                    TITLE = "🖼️ Background Mode: Solid",
                    DESCRIPTION = "Switch to solid theme color background",
                    EXECUTE = () => SetBackgroundMode("Solid")
                });
                results.Add(new CommandResult
                {
                    TITLE = "🖼️ Background Mode: Media (GIF)",
                    DESCRIPTION = "Switch to GIF/Media background mode",
                    EXECUTE = () => SetBackgroundMode("Media")
                });
            }
            else if (parts.Length >= 2)
            {
                string sub = parts[1].ToLower();
                if (sub == "set" && parts.Length >= 3)
                {
                    string path = query.Substring(query.IndexOf(parts[2])).Trim();
                    results.Add(new CommandResult
                    {
                        TITLE = $"🖼️ Set Background GIF: {Path.GetFileName(path)}",
                        DESCRIPTION = $"Use this file as your media background: {path}",
                        EXECUTE = () => SetBackgroundMedia(path)
                    });
                }
            }

            return results;
        }

        private void SetBackgroundMode(string mode)
        {
            SettingsManager.Current.BACKGROUND_MODE = mode;
            SettingsManager.Save();
            ThemeManager.ApplyTheme(SettingsManager.Current.THEME);
            TextOverlay.Show($"🖼️ Background Mode: {mode}", 2000);
        }

        private void SetBackgroundMedia(string path)
        {
            if (File.Exists(path))
            {
                SettingsManager.Current.BACKGROUND_MODE = "Media";
                SettingsManager.Current.BACKGROUND_MEDIA_SOURCE = path;
                SettingsManager.Save();
                ThemeManager.ApplyTheme(SettingsManager.Current.THEME);
                TextOverlay.Show($"🖼️ Background GIF Set!", 2000);
            }
            else
            {
                TextOverlay.Show("⚠️ File not found!", 2000);
            }
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("background [mode]", "Switch background between Solid, Gradient, or Media", "bg gradient"),
                new CommandDesc("bg set [path]", "Set a specific GIF file as background", "bg set C:\\path\\to\\my.gif")
            };
        }

        public void OnStart() { }
    }
}
