// Developer: heaplyn
// Date: 2026-08-14
// Summary: Handles packages uninstall commands (winget, npm, python/pip)
//          and supports self-uninstallation of the Heaplit launcher.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public class UninstallCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "uninstall");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string args = query.Length > 9 ? query.Substring(9).Trim() : "";

            if (string.IsNullOrEmpty(args))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🗑️ Uninstall Packages or Heaplit",
                    DESCRIPTION = "Syntax: uninstall [winget/npm/python/self] [package_name]",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "uninstall") + 5.0 * 0.01),
                    EXECUTE = () => TextOverlay.Show("Example: uninstall winget sideloadly", 4000)
                });
                return suggestions;
            }

            // Self-Uninstall Route
            if (args.ToLower() == "self" || args.ToLower() == "heaplit")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "⚠️ Completely Uninstall Heaplit Launcher",
                    DESCRIPTION = "Purges all local configurations, templates, voice models, and files",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "uninstall") + 9.0 * 0.01),
                    EXECUTE = () =>
                    {
                        var confirm = MessageBox.Show(
                            "This action will completely remove Heaplit, delete all local configuration profiles, voiceprints, reminders, and close the application. Proceed with uninstallation?",
                            "Confirm Full Uninstallation",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning
                        );

                        if (confirm == MessageBoxResult.Yes)
                        {
                            TtsManager.Speak("Heaplit uninstallation initiated. Goodbye, owner.");
                            TextOverlay.Show("Goodbye...", 3000);
                            
                            // Write and trigger uninstaller cleanup script
                            Task.Run(async () =>
                            {
                                await Task.Delay(2000);
                                RunSelfUninstallerScript();
                            });
                        }
                    }
                });
                return suggestions;
            }

            // Split action parameters
            int spaceIdx = args.IndexOf(' ');
            string provider = spaceIdx != -1 ? args.Substring(0, spaceIdx).ToLower() : args.ToLower();
            string pkg = spaceIdx != -1 ? args.Substring(spaceIdx + 1).Trim() : "";

            // Winget uninstaller
            if (provider == "winget")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🗑️ Uninstall Winget Package: {pkg}",
                    DESCRIPTION = $"Runs: winget uninstall {pkg} --silent",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "uninstall") + 6.8 * 0.01),
                    EXECUTE = () => RunUninstallProcess("winget", $"uninstall {pkg} --silent")
                });
            }
            // NPM uninstaller
            else if (provider == "npm")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🗑️ Uninstall NPM Package: {pkg}",
                    DESCRIPTION = $"Runs: npm uninstall -g {pkg}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "uninstall") + 6.8 * 0.01),
                    EXECUTE = () => RunUninstallProcess("cmd.exe", $"/c npm uninstall -g {pkg}")
                });
            }
            // Python/Pip uninstaller
            else if (provider == "python" || provider == "pip")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🗑️ Uninstall Python pip Package: {pkg}",
                    DESCRIPTION = $"Runs: pip uninstall -y {pkg}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "uninstall") + 6.8 * 0.01),
                    EXECUTE = () => RunUninstallProcess("cmd.exe", $"/c pip uninstall -y {pkg}")
                });
            }

            return suggestions;
        }

        private void RunUninstallProcess(string processName, string arguments)
        {
            try
            {
                TextOverlay.Show($"🗑️ Uninstalling package via {processName}...", 4000);
                Process.Start(new ProcessStartInfo
                {
                    FileName = processName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = false
                });
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"❌ Uninstall failed: {ex.Message}", 4000);
            }
        }

        private void RunSelfUninstallerScript()
        {
            try
            {
                string projectDir = AppDomain.CurrentDomain.BaseDirectory;
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox"); // Wait, no, appdata is App Data Directory: C:\Users\Kyle\.gemini\antigravity
                string geminiAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity");

                string batchScript = $@"@echo off
timeout /t 2 /nobreak > NUL
echo Removing app binaries...
rmdir /s /q ""{projectDir}""
echo Removing user settings, models and cache...
rmdir /s /q ""{geminiAppData}""
echo Heaplit has been completely removed.
pause
del ""%~f0""
exit
";

                string tempBatch = Path.Combine(Path.GetTempPath(), "heaplit_uninstaller.bat");
                File.WriteAllText(tempBatch, batchScript);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{tempBatch}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                });

                // Exit process
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to execute self uninstaller: {ex.Message}");
            }
        }
    }
}
