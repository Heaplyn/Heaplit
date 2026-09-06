// Developer: heaplyn
// Date: 2026-08-17
// Summary: Deep Project Context Manager.
//          Indexes project structure and runs AI-powered file analysis to build a comprehensive system map.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class ProjectContextManager : IProjectContextService
    {
        private string _rootPath = string.Empty;
        private readonly List<FileSummary> _summaries = new();
        private readonly string[] _targetExts = { ".cs", ".xaml", ".bat", ".md", ".json", ".bat", ".bat", ".bat" };
        private readonly string[] _ignoredDirs = { "bin", "obj", ".git", ".vs", "node_modules", "publish" };

        public List<FileSummary> GetFileSummaries() => _summaries;

        public async Task RefreshIndexAsync(string rootPath)
        {
            _rootPath = rootPath;
            if (!Directory.Exists(rootPath)) return;

            // Basic Indexing (Structural)
            _ = ProjectSymbolIndexer.IndexProjectAsync(rootPath);
        }

        public async Task<string> GetProjectSummaryAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## HEAPLIT SYSTEM KNOWLEDGE: CURRENT PROJECT");
            sb.AppendLine($"Project Root: {_rootPath}");
            sb.AppendLine(ProjectMapManager.BuildProjectTree(_rootPath, 2));

            if (_summaries.Any())
            {
                sb.AppendLine("\n## MODULE ANALYSIS");
                foreach (var s in _summaries.OrderByDescending(x => x.Size).Take(30))
                {
                    sb.AppendLine($"- {Path.GetFileName(s.FilePath)}: {s.Summary}");
                }
            }
            else {
                sb.AppendLine("\n(Deep analysis not yet performed. AI has structural context only.)");
            }

            return sb.ToString();
        }

        public async Task RunDeepAnalysisAsync(Action<string, double> progressCallback)
        {
            if (string.IsNullOrEmpty(_rootPath)) return;

            var files = Directory.GetFiles(_rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => _targetExts.Contains(Path.GetExtension(f).ToLower()))
                .Where(f => !_ignoredDirs.Any(d => f.Contains($"\\{d}\\") || f.Contains($"/{d}/")))
                .ToList();

            _summaries.Clear();
            int processed = 0;
            int total = files.Count;

            // Use a semaphore to control parallelism (prevent hitting LLM rate limits or crashing system)
            var semaphore = new System.Threading.SemaphoreSlim(4);
            var tasks = files.Select(async file =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string content = await File.ReadAllTextAsync(file);
                    if (content.Length > 20000) content = content.Substring(0, 20000);

                    string prompt = $"TASK: Provide a ONE-SENTENCE technical summary of this file's purpose in the project.\nFILE: {Path.GetFileName(file)}\nCONTENT:\n{content}";

                    string summary = await CoreRegistry.Intelligence.Llm.AskAsync(prompt);

                    lock (_summaries)
                    {
                        _summaries.Add(new FileSummary { FilePath = file, Summary = summary.Trim(), Size = new FileInfo(file).Length });
                        processed++;
                        double percent = (double)processed / total * 100;
                        progressCallback?.Invoke($"Analyzing {Path.GetFileName(file)}...", percent);
                    }
                }
                catch (Exception ex)
                {
                    DebugConsoleOverlay.Log("ProjectContext", $"Failed to analyze {file}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            progressCallback?.Invoke("Deep Analysis Complete.", 100);

            // Save deep map to local file for persistence
            try {
                string mapPath = Path.Combine(PathHandler.GetDataDirectory(), "project_deep_map.json");
                File.WriteAllText(mapPath, System.Text.Json.JsonSerializer.Serialize(_summaries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            } catch { }
        }
    }
}
