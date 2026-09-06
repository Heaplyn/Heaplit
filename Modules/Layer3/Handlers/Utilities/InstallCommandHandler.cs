// Developer: heaplyn
// Date: 2026-08-14
// Summary: Handles packages install commands (winget, npm, python, dotnet, or universal website scraping installer).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class InstallCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "install", "suite", "devsuite", "developer suite");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string trimmed = query.Trim().ToLower();
            string args = query.Length > 8 ? query.Substring(8).Trim() : "";

            if (trimmed == "suite" || trimmed == "devsuite" || trimmed == "developer suite" || trimmed == "install")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🛠️ Open Universal Developer & Offline Suite",
                    DESCRIPTION = "One-click setup for Languages, Game Engines, and Package Managers.",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 10.0 * 0.01),
                    EXECUTE = () => DevSuiteOverlay.ShowOverlay()
                });
                if (trimmed != "install") return suggestions;
            }

            if (string.IsNullOrEmpty(args))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "📥 Install Packages & Tools",
                    DESCRIPTION = "Syntax: install [winget/npm/python/dotnet/url] [package_name]",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 5.0 * 0.01),
                    EXECUTE = () => TextOverlay.Show("Example: install winget sideloadly", 4000)
                });
                return suggestions;
            }

            // Route 1: Web Installer Scraper
            if (args.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || args.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🌐 Scrape & Install from: {args}",
                    DESCRIPTION = "Downloads and executes Windows installer binary from this webpage",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 7.0 * 0.01),
                    EXECUTE = () =>
                    {
                        Task.Run(async () =>
                        {
                            string result = await UniversalInstaller.InstallFromUrlAsync(args);
                            TextOverlay.Show(result, 5000);
                        });
                    }
                });
                return suggestions;
            }

            // Split action parameters
            int spaceIdx = args.IndexOf(' ');
            string provider = spaceIdx != -1 ? args.Substring(0, spaceIdx).ToLower() : args.ToLower();
            string pkg = spaceIdx != -1 ? args.Substring(spaceIdx + 1).Trim() : "";

            // Route 2: Winget installer
            if (provider == "winget")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"📦 Install Winget Package: {pkg}",
                    DESCRIPTION = $"Runs: winget install {pkg} --silent",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 6.8 * 0.01),
                    EXECUTE = () => RunInstallProcess("winget", $"install {pkg} --silent")
                });
            }
            // Route 3: NPM installer
            else if (provider == "npm")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"📦 Install NPM Package: {pkg}",
                    DESCRIPTION = $"Runs: npm install -g {pkg}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 6.8 * 0.01),
                    EXECUTE = () => RunInstallProcess("cmd.exe", $"/c npm install -g {pkg}")
                });
            }
            // Route 4: Python installer
            else if (provider == "python" || provider == "pip")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🐍 Install Python pip Package: {pkg}",
                    DESCRIPTION = $"Runs: pip install {pkg}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 6.8 * 0.01),
                    EXECUTE = () => RunInstallProcess("cmd.exe", $"/c pip install {pkg}")
                });
            }
            // Route 5: Dotnet workloads installer
            else if (provider == "dotnet" || provider == "workload")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🛠️ Install .NET Workload: {pkg}",
                    DESCRIPTION = $"Runs: dotnet workload install {pkg} --source https://api.nuget.org/v3/index.json",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 6.8 * 0.01),
                    EXECUTE = () => RunInstallProcess("dotnet", $"workload install {pkg} --source https://api.nuget.org/v3/index.json", runAsAdmin: true)
                });
            }
            else
            {
                // General fallback: Winget search install
                suggestions.Add(new CommandResult
                {
                    TITLE = $"📥 Install '{args}' via Winget",
                    DESCRIPTION = $"Runs: winget install {args}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 6.0 * 0.01),
                    EXECUTE = () => RunInstallProcess("winget", $"install {args}")
                });
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🌐 Search and download installer for '{args}' from Web",
                    DESCRIPTION = $"Opens Google search for '{args} download setup'",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "install", "suite", "devsuite", "developer suite") + 5.8 * 0.01),
                    EXECUTE = () => Process.Start(new ProcessStartInfo
                    {
                        FileName = $"https://www.google.com/search?q={Uri.EscapeDataString(args + " download windows setup msi")}",
                        UseShellExecute = true
                    })
                });
            }

            return suggestions;
        }

        private void RunInstallProcess(string command, string args, bool runAsAdmin = false)
        {
            try
            {
                TextOverlay.Show($"📥 Executing package installer: {command} {args}...", 3000);
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    UseShellExecute = runAsAdmin,
                    Verb = runAsAdmin ? "runas" : "",
                    CreateNoWindow = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Installation Failed: {ex.Message}", 4000);
            }
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("install winget [pkg]", "Install a package silently using winget command line", "install winget sideloadly"),
                new CommandDesc("install npm [pkg]", "Install global NPM package dependency", "install npm vite"),
                new CommandDesc("install [url]", "Scrape and download/run installer from target webpage", "install https://sideloadly.io/index.html")
            };
        }
    }
}
