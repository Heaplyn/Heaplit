// Developer: heaplyn
// Date: 2026-08-15
// Summary: Autonomous System Knowledge Harvester.
//          Periodically crawls the codebase and system directories to index class structures,
//          handler logic, and file relationships. This creates a "Self-Aware" knowledge base
//          that is injected into the AI's context.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class SystemKnowledgeManager
    {
        private static bool IsRunning = false;
        private static string _cachedKnowledgeSummary = "Indexing system structure...";
        private static readonly object _lock = new object();

        public static string GetSystemKnowledge()
        {
            lock (_lock) return _cachedKnowledgeSummary;
        }

        public static void Start()
        {
            if (IsRunning) return;
            IsRunning = true;

            Task.Run(async () =>
            {
                // Delay first scan to allow boot to finish
                await Task.Delay(15000);

                while (IsRunning)
                {
                    try
                    {
                        await RebuildKnowledgeBaseAsync();
                    }
                    catch (Exception ex)
                    {
                        DebugConsoleOverlay.Log("Knowledge-Error", ex.Message);
                    }

                    // Re-scan every 10 minutes to stay updated with code changes
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromMinutes(10));
                }
            });

            DebugConsoleOverlay.Log("Knowledge-System", "Autonomous System Harvester active.");
        }

        public static async Task ExpandAcousticDatabasesAsync(bool force = false)
        {
            try
            {
                string markerFile = Path.Combine(PathHandler.GetDataDirectory(), "acoustic_expansion_done.tag");
                if (File.Exists(markerFile) && !force) return;

                DebugConsoleOverlay.Log("Knowledge-Self-Improvement", "Initiating search for small acoustic databases...");

                // Search for datasets on GitHub or web lists
                string searchResult = await WebOperationManager.SearchWebAsync("small voice wake word datasets github list mp3 wav");

                // Use AI to extract the best dataset list from the search results
                string prompt = "From the search results below, identify a URL that points to a GitHub repository or a list containing small acoustic datasets (MP3/WAV samples for wake words or environmental sounds). " +
                                "Return ONLY the raw URL. If none look high-signal, return 'NONE'.\n\n" +
                                searchResult;

                string bestUrl = await LlmRouter.AskAsync(prompt);
                bestUrl = bestUrl.Trim();

                if (bestUrl != "NONE" && bestUrl.StartsWith("http"))
                {
                    DebugConsoleOverlay.Log("Knowledge-Self-Improvement", $"Found potential dataset: {bestUrl}. Downloading...");
                    string result = await WebOperationManager.DownloadListAsync(bestUrl);
                    DebugConsoleOverlay.Log("Knowledge-Self-Improvement", "Acoustic expansion pass complete.");

                    await File.WriteAllTextAsync(markerFile, DateTime.Now.ToString());
                    AcousticMlClassifier.RebuildAcousticIndex();
                }
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Knowledge-Error", $"Acoustic expansion failed: {ex.Message}");
            }
        }

        private static async Task RebuildKnowledgeBaseAsync()
        {
            string root = PathHandler.GetProjectRoot();
            var sb = new StringBuilder();
            sb.AppendLine("## INTERNAL SYSTEM ARCHITECTURE KNOWLEDGE");

            // 1. Map Modules and Layers
            var modulesDir = Path.Combine(root, "Modules");
            if (Directory.Exists(modulesDir))
            {
                var layers = Directory.GetDirectories(modulesDir, "Layer*");
                foreach (var layer in layers)
                {
                    string layerName = Path.GetFileName(layer);
                    sb.AppendLine($"### {layerName}");

                    var files = Directory.GetFiles(layer, "*.cs", SearchOption.AllDirectories)
                                 .Select(f => new FileInfo(f))
                                 .OrderByDescending(f => f.LastWriteTime)
                                 .Take(15);

                    foreach (var file in files) {
                        sb.AppendLine($"- {file.Name} (Updated: {file.LastWriteTime:MM/dd HH:mm})");
                    }
                }
            }

            // 2. Map Handlers
            var handlersDir = Path.Combine(root, "Modules", "Layer3", "Handlers");
            if (Directory.Exists(handlersDir)) {
                sb.AppendLine("### Command Handlers");
                foreach (var h in Directory.GetFiles(handlersDir, "*Handler.cs")) sb.AppendLine($"- {Path.GetFileNameWithoutExtension(h)}");
            }

            // 3. User Files Ingestion (Documents/Downloads)
            sb.AppendLine("### USER SYSTEM SNAPSHOT");
            try {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var recentDocs = Directory.GetFiles(docs, "*.*").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).Take(10);
                foreach (var f in recentDocs) sb.AppendLine($"- Recent Doc: {f.Name} ({f.Extension})");

                string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var recentDls = Directory.GetFiles(downloads, "*.*").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).Take(10);
                foreach (var f in recentDls) sb.AppendLine($"- Recent Download: {f.Name}");
            } catch { }

            lock (_lock)
            {
                _cachedKnowledgeSummary = sb.ToString();
            }

            DebugConsoleOverlay.Log("Knowledge-System", $"Self-teaching pass complete. Indexing {sb.Length} bytes of architecture data.");
            await File.WriteAllTextAsync(Path.Combine(PathHandler.GetDataDirectory(), "SystemKnowledge.md"), _cachedKnowledgeSummary);
        }

        private static string ExtractSummary(string filePath)
        {
            try
            {
                var lines = File.ReadLines(filePath).Take(10);
                foreach (var line in lines)
                {
                    if (line.Contains("Summary:"))
                    {
                        return line.Substring(line.indexOf("Summary:") + 8).Trim();
                    }
                }
            }
            catch { }
            return "Core logic module.";
        }

        // Extension helper for old .NET versions if needed
        private static int indexOf(this string source, string value) => source.IndexOf(value);
    }
}
