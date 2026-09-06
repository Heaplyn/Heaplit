// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to get, set, or reset the custom download directory path for downloaded music media.

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace HeaplitLauncher
{
    public class DownloadPathCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "getdlpath", "resetdlpath", "setdlpath");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            string cmd = parts[0].ToLower();
            double similarity = SearchUtil.BestSimilarity(query, "getdlpath", "resetdlpath", "setdlpath"); // High priority match

            if (cmd == "getdlpath")
            {
                string currentPath = SettingsManager.Current.DOWNLOAD_DIRECTORY;
                string displayPath = string.IsNullOrWhiteSpace(currentPath) 
                    ? GetDefaultDownloadPath() + " [Default]"
                    : currentPath;

                suggestions.Add(new CommandResult
                {
                    TITLE       = $"Downloads Folder: {displayPath}",
                    DESCRIPTION = "Display the target directory path where music files are saved",
                    SIMILARITY  = similarity,
                    EXECUTE     = null
                });
            }
            else if (cmd == "resetdlpath")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE       = "Reset Downloads Folder",
                    DESCRIPTION = $"Restore default target path to project folder: {GetDefaultDownloadPath()}",
                    SIMILARITY  = similarity,
                    EXECUTE     = () => ResetDownloadPath()
                });
            }
            else if (cmd == "setdlpath")
            {
                if (parts.Length > 1)
                {
                    string targetPath = parts[1].Trim().Trim('"', '\'');
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"Set Downloads Folder to: {targetPath}",
                        DESCRIPTION = "Update the download destination folder for Lucida/YT-DLP",
                        SIMILARITY  = similarity,
                        EXECUTE     = () => SetDownloadPath(targetPath)
                    });
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "Set Downloads Folder...",
                        DESCRIPTION = "Type the destination folder path (e.g. setdlpath C:\\Users\\Name\\Music)",
                        SIMILARITY  = similarity,
                        EXECUTE     = null
                    });
                }
            }

            return suggestions;
        }

        private static string GetDefaultDownloadPath()
        {
            string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            if (Directory.Exists(Path.Combine(devPath, "Modules")))
            {
                return Path.Combine(devPath, "Downloads");
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
        }

        private static void SetDownloadPath(string path)
        {
            try
            {
                // Basic path validation
                if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    TextOverlay.Show("⚠️ Invalid folder path characters detected!", 3000);
                    return;
                }

                // If path doesn't exist, try to create it to verify write permissions
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                SettingsManager.Current.DOWNLOAD_DIRECTORY = path;
                SettingsManager.Save();
                TextOverlay.Show($"📁 Downloads directory configured successfully!", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to save directory: {ex.Message}", 3000);
            }
        }

        private static void ResetDownloadPath()
        {
            try
            {
                SettingsManager.Current.DOWNLOAD_DIRECTORY = string.Empty;
                SettingsManager.Save();
                TextOverlay.Show("📁 Downloads path reset to project default.", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Reset failed: {ex.Message}", 3000);
            }
        }
    }
}
