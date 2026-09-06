// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to launch the interactive Git/GitHub setup wizard.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace HeaplitLauncher
{
    public class GitSetupCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "gitsetup" || query == "setupgit" || query == "git config" || query == "git setup";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            double similarity = SearchUtil.GetSimilarity(query.Trim().ToLower(), "gitsetup");

            suggestions.Add(new CommandResult
            {
                TITLE       = "Setup GitHub Workspace",
                DESCRIPTION = "Configure Git user identity, link remote repositories, and authenticate with GitHub",
                SIMILARITY  = similarity,
                EXECUTE     = RunGitSetup
            });

            return suggestions;
        }

        private static void RunGitSetup()
        {
            TextOverlay.Show("⚙️ Starting GitHub Setup Wizard...", 2500);

            string scriptPath = GetScriptPath();
            string projectRoot = GetProjectRoot();

            if (!File.Exists(scriptPath))
            {
                MessageBox.Show($"Setup script not found. Checked locations:\n" +
                                $"- {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "Layer0", "git_setup.bat")}\n" +
                                $"- {Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Modules\Layer0\git_setup.bat"))}", 
                                "Heaplit Git Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName         = "cmd.exe",
                        Arguments        = $"/c start \"Heaplit GitHub Setup Wizard\" \"{scriptPath}\"",
                        WorkingDirectory = projectRoot,
                        UseShellExecute  = true, // Opens a new visible and interactive command window
                        CreateNoWindow   = false
                    }
                };
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch setup wizard:\n{ex.Message}", "Heaplit Git Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetScriptPath()
        {
            string binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "Layer0", "git_setup.bat");
            if (File.Exists(binPath))
            {
                return binPath;
            }

            string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Modules\Layer0\git_setup.bat"));
            if (File.Exists(devPath))
            {
                return devPath;
            }

            return binPath;
        }

        private static string GetProjectRoot()
        {
            string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            if (Directory.Exists(Path.Combine(devPath, "Modules")) || Directory.Exists(Path.Combine(devPath, ".git")))
            {
                return devPath;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
