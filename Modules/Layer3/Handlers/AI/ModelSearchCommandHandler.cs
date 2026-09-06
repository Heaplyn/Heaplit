// Developer: heaplyn
// Date: 2026-09-01
// Summary: HUD command to search AI models (OpenRouter gateway + local Ollama/LM Studio) and
//          one-click auto-configure the router to use one. Search runs async in the background
//          and results stream into the palette as selectable rows (the palette itself is sync).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class ModelSearchCommandHandler : ICommandHandler
    {
        private static volatile List<ModelInfo> _cache = new();
        private static string _lastQuery = "\0";
        private static bool _searching;

        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "model", "findmodel", "detect ai", "detect local");
        }

        public List<CommandDesc> GetCommandDescriptions() => new()
        {
            new CommandDesc { COMMAND_NAME = "model <name>", COMMAND_DESCRIPTION = "Search AI models (cloud + local) and switch to one", COMMAND_EXAMPLE = "model claude" },
            new CommandDesc { COMMAND_NAME = "detect local ai", COMMAND_DESCRIPTION = "Auto-detect running Ollama / LM Studio engines", COMMAND_EXAMPLE = "detect local ai" },
        };

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string raw = query.Trim();
            string q = raw.ToLower();

            if (q.StartsWith("detect ai") || q.StartsWith("detect local"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔎 Detect local AI engines",
                    DESCRIPTION = "Probe for Ollama / LM Studio and report what's available",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "model", "findmodel", "detect ai", "detect local") + 9.5 * 0.01),
                    EXECUTE = () => _ = DetectAsync()
                });
                return suggestions;
            }

            // "model <query>"
            string term = raw.Length > 5 ? raw.Substring(5).Trim() : "";

            // Kick off (or refresh) the async search when the term changes.
            if (term != _lastQuery && !_searching)
            {
                _lastQuery = term;
                _ = RefreshAsync(term);
            }

            if (_searching && _cache.Count == 0)
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "⏳ Searching models…",
                    DESCRIPTION = $"Looking for '{term}' across OpenRouter + local engines",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "model", "findmodel", "detect ai", "detect local") + 10.0 * 0.01),
                    EXECUTE = () => { }
                });
                return suggestions;
            }

            if (_cache.Count == 0)
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "Type a model name to search",
                    DESCRIPTION = "e.g. 'model claude', 'model llama', 'model gpt-4', 'model qwen'",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "model", "findmodel", "detect ai", "detect local") + 8.0 * 0.01),
                    EXECUTE = () => { }
                });
                return suggestions;
            }

            double sim = 9.8;
            foreach (var m in _cache.Take(12))
            {
                var model = m; // capture
                string icon = model.IsLocal ? "🖥️" : "☁️";
                suggestions.Add(new CommandResult
                {
                    TITLE = $"{icon} {model.Id}",
                    DESCRIPTION = $"[{model.Provider}] {model.Detail} — click to switch Heaplit to this model",
                    SIMILARITY = sim,
                    EXECUTE = () =>
                    {
                        string status = ModelDiscoveryService.ApplyModel(model);
                        try { TextOverlay.Show(status, 4000); } catch { }
                        try { DebugConsoleOverlay.Log("Model", status); } catch { }
                    }
                });
                sim -= 0.1;
            }
            return suggestions;
        }

        private static async Task RefreshAsync(string term)
        {
            _searching = true;
            try { _cache = await ModelDiscoveryService.SearchAsync(term); }
            catch { _cache = new List<ModelInfo>(); }
            finally { _searching = false; }
        }

        private static async Task DetectAsync()
        {
            try
            {
                string status = await ModelDiscoveryService.AutoDetectLocalProvidersAsync();
                TextOverlay.Show(status, 5000);
                DebugConsoleOverlay.Log("Model", status);
            }
            catch { }
        }
    }
}
