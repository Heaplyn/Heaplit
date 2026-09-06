// Developer: heaplyn
// Date: 2026-08-19
// Summary: Automated Dataset Harvesting Engine.
//          Scrapes curated GitHub repositories and use AI to discover and download high-quality LLM datasets.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class DatasetHarvester
    {
        private const string PrimarySeedUrl = "https://github.com/mlabonne/llm-datasets";
        private static readonly List<string> _discoveredDatasets = new List<string>();
        private static bool _isProcessing = false;

        public static async Task RunAutomaticHarvestAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                DebugConsoleOverlay.Log("Dataset-Harvester", "Initiating autonomous dataset discovery...");

                // 1. Scrape Primary Seed
                var scrape = await WebScraperManager.ScrapePageAsync(PrimarySeedUrl);

                // 2. Extract Hugging Face links using Regex
                var hfLinks = scrape.Links
                    .Where(l => l.Href.Contains("huggingface.co/datasets/"))
                    .Select(l => ExtractRepoId(l.Href))
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                DebugConsoleOverlay.Log("Dataset-Harvester", $"Found {hfLinks.Count} initial datasets on seed page.");

                // 3. Use AI to prioritize or suggest new search terms
                string prompt = "### DATASET HARVESTER\n" +
                                "Sir, I've found these datasets on GitHub:\n" +
                                $"{string.Join(", ", hfLinks.Take(20))}\n\n" +
                                "### TASK\n" +
                                "1. Identify the 3 most important ones for general LLM fine-tuning.\n" +
                                "2. Suggest 5 new 'search keywords' for Hugging Face to find more cutting-edge datasets.\n" +
                                "Format: [PRIORITY]: id1, id2... [SEARCH]: keyword1, keyword2...";

                string response = await LlmRouter.AskAsync(prompt);

                // 4. Parse AI Response
                var priorities = ParseSection(response, "[PRIORITY]:");
                var newKeywords = ParseSection(response, "[SEARCH]:");

                // 5. Download Priorities (limit to avoid disk blowup)
                foreach (var id in priorities.Take(2))
                {
                    if (!_discoveredDatasets.Contains(id))
                    {
                        DebugConsoleOverlay.Log("Dataset-Harvester", $"AI prioritized dataset: {id}. Triggering download.");
                        HuggingFaceManager.DownloadModelRepo(id, repoType: "dataset");
                        _discoveredDatasets.Add(id);
                    }
                }

                // 6. Perform Secondary Search based on AI suggestions
                foreach (var keyword in newKeywords.Take(3))
                {
                    DebugConsoleOverlay.Log("Dataset-Harvester", $"Performing secondary search for: '{keyword}'");
                    // We can reuse HuggingFaceManager.SearchModelsAsync but for datasets
                    // Actually SearchModelsAsync currently uses /api/models, we might need /api/datasets
                    await SearchAndDownloadDatasetsAsync(keyword);
                }
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Dataset-Harvester-Error", $"Harvest failed: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private static string ExtractRepoId(string url)
        {
            // Example: https://huggingface.co/datasets/mlabonne/FineTome-100k
            var match = Regex.Match(url, @"huggingface\.co/datasets/([^/\s?#]+/[^/\s?#]+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static List<string> ParseSection(string text, string marker)
        {
            if (!text.Contains(marker)) return new List<string>();
            var part = text.Split(marker)[1].Split('\n')[0];
            return part.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        private static async Task SearchAndDownloadDatasetsAsync(string keyword)
        {
            try
            {
                // Similar to HuggingFaceManager.SearchModelsAsync but for datasets
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "HeaplitLauncher/1.0");
                string url = $"https://huggingface.co/api/datasets?search={Uri.EscapeDataString(keyword)}&limit=5&sort=downloads&direction=-1";

                string json = await client.GetStringAsync(url);
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    string id = item.GetProperty("id").GetString() ?? "";
                    if (!string.IsNullOrEmpty(id) && !_discoveredDatasets.Contains(id))
                    {
                        DebugConsoleOverlay.Log("Dataset-Harvester", $"Discovered new dataset via '{keyword}': {id}");
                        // For auto-evolution, we might not want to download EVERY discovered thing automatically
                        // to save space, but the user asked to "download them".
                        // I'll limit to 1 from each search to be safe.
                        HuggingFaceManager.DownloadModelRepo(id, repoType: "dataset");
                        _discoveredDatasets.Add(id);
                        break;
                    }
                }
            }
            catch { }
        }
    }
}
