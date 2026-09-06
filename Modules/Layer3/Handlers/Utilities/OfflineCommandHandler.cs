// Developer: heaplyn
// Date: 2026-08-17
// Summary: Handles CLI commands for offline mode, pre-caching, and local environment setup.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class OfflineCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "offline", "cache", "precache", "vosk", "local", "offline mode");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim().ToLower();

            suggestions.Add(new CommandResult
            {
                TITLE = "📶 Open Offline & Pre-Caching Studio",
                DESCRIPTION = "Manage offline speech models, TTS samples, and dev tool installers",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "offline", "cache", "precache", "vosk", "local", "offline mode") + 9.5 * 0.01),
                EXECUTE = () => OfflineStudioOverlay.ShowOverlay()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "📶 Run Full Offline Pre-Cache",
                DESCRIPTION = "Download Vosk models, TTS samples, and core dependencies for Wi-Fi disconnected use",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "offline", "cache", "precache", "vosk", "local", "offline mode") + 8.5 * 0.01),
                EXECUTE = () => Task.Run(async () => {
                    TextOverlay.Show("📶 Starting background pre-caching...", 3000);
                    await OfflineCacheManager.PreCacheAllForOfflineAsync(null);
                    TextOverlay.Show("✅ Pre-caching sequence initiated.", 3000);
                })
            });

            if (q.Contains("vosk"))
            {
                suggestions.Add(new CommandResult {
                    TITLE = "🎙️ Download Vosk Model",
                    DESCRIPTION = "Force download of the offline neural speech recognition model",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "offline", "cache", "precache", "vosk", "local", "offline mode") + 9.0 * 0.01),
                    EXECUTE = () => Task.Run(async () => await VoskEngine.EnsureModelDownloadedAsync(true))
                });
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("offline", "Open Offline & Pre-Caching Studio", "offline"),
                new CommandDesc("precache", "Start full system pre-caching", "precache")
            };
        }
    }
}
