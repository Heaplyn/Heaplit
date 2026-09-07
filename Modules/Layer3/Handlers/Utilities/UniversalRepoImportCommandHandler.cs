// Developer: heaplyn
// Date: 2026-09-07
// Summary: Command handler for Universal Repository Import & Smart Project Setup.
//          Handles queries for cloning/importing repositories from GitHub, GitLab, Bitbucket, Git URLs, and local directories.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class UniversalRepoImportCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            string trimmed = query.Trim().ToLowerInvariant();
            return SearchUtil.MatchesAny(query, "import", "clone", "github", "gitlab", "bitbucket", "repo", "repository", "smart setup") ||
                   trimmed.StartsWith("git clone", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Contains("github.com") ||
                   trimmed.Contains("gitlab.com") ||
                   trimmed.Contains("bitbucket.org");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string trimmed = query.Trim().ToLowerInvariant();
            string raw = query.Trim();

            string targetArg = "";
            if (raw.StartsWith("import repo ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(12).Trim();
            else if (raw.StartsWith("setup repo ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(11).Trim();
            else if (raw.StartsWith("import github ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(14).Trim();
            else if (raw.StartsWith("git clone ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(10).Trim();
            else if (raw.StartsWith("import ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(7).Trim();
            else if (raw.StartsWith("clone ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(6).Trim();
            else if (raw.StartsWith("github ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(7).Trim();
            else if (raw.StartsWith("gitlab ", StringComparison.OrdinalIgnoreCase)) targetArg = raw.Substring(7).Trim();
            else if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("git@", StringComparison.OrdinalIgnoreCase)) targetArg = raw;

            // Main GUI Overlay Launch Suggestion
            if (string.IsNullOrEmpty(targetArg) || trimmed == "import" || trimmed == "clone" || trimmed == "github" || trimmed == "repo" || trimmed == "repository" || trimmed == "importer")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🚀 Open Universal Repository Importer & Smart Setup",
                    DESCRIPTION = "Clone/import any Git repository (GitHub/GitLab/Bitbucket/Local), auto-detect stack, synthesize configs, and install packages.",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "import", "clone", "github", "repo") + 12.0 * 0.01),
                    EXECUTE = () => UniversalRepoImporterOverlay.ShowOverlay()
                });
            }

            // Direct Target Execution Suggestion
            if (!string.IsNullOrEmpty(targetArg))
            {
                var (cleanUrl, repoName, hostType) = UniversalRepoImporterManager.ParseRepoTarget(targetArg);

                suggestions.Add(new CommandResult
                {
                    TITLE = $"⚡ Smart Import & Setup [{repoName}] ({hostType})",
                    DESCRIPTION = $"Clones {cleanUrl}, detects stack, synthesizes .env/.vscode, and provisions dependencies.",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "import", "clone", "github", "setup") + 15.0 * 0.01),
                    EXECUTE = () =>
                    {
                        UniversalRepoImporterOverlay.ShowOverlay(cleanUrl);
                    }
                });

                suggestions.Add(new CommandResult
                {
                    TITLE = $"📥 Quick Background Clone & Setup [{repoName}]",
                    DESCRIPTION = $"Runs automated non-blocking setup for {cleanUrl} in background.",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "import", "clone", "github", "setup") + 10.0 * 0.01),
                    EXECUTE = () =>
                    {
                        Task.Run(async () =>
                        {
                            TextOverlay.Show($"🚀 Starting background import of [{repoName}]...", 4000);
                            var progress = new Progress<string>(msg => TextOverlay.Show(msg, 3500));
                            var res = await UniversalRepoImporterManager.ImportAndSetupRepoAsync(cleanUrl, null, true, progress);
                            if (res.Success)
                            {
                                TextOverlay.Show($"🎉 [{repoName}] setup complete! Ready to run: {res.Stack.RunCommand}", 5000);
                            }
                            else
                            {
                                TextOverlay.Show($"❌ Import failed: {res.ErrorMessage}", 5000);
                            }
                        });
                    }
                });
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("import [repo_url]", "Universal Smart Repo Importer & Config Synthesizer (GitHub/GitLab/Bitbucket/Local)", "import https://github.com/facebook/react"),
                new CommandDesc("clone [repo_url]", "Clone and auto-configure developer stack and dependencies", "clone https://github.com/shadcn-ui/ui"),
                new CommandDesc("setup repo [path/url]", "Smart analyze and setup existing local repo or remote project", "setup repo C:\\Projects\\MyApp")
            };
        }
    }
}