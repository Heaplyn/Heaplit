// Developer: heaplyn
// Date: 2026-08-16
// Summary: High-performance project-wide symbol indexer.
//          Scans the active project directory for C# classes, methods, and types to provide IDE-grade autocomplete.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class ProjectSymbol
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty; // Class, Method, Variable
        public string FilePath { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty; // Parent class
    }

    public static class ProjectSymbolIndexer
    {
        private static readonly ConcurrentDictionary<string, ProjectSymbol> _symbolCache = new ConcurrentDictionary<string, ProjectSymbol>(StringComparer.OrdinalIgnoreCase);
        private static bool _isIndexing = false;
        private static readonly AsyncCSharpFileLoader _loader = new AsyncCSharpFileLoader();

        public static List<ProjectSymbol> Symbols => _symbolCache.Values.ToList();

        public static async Task IndexProjectAsync(string rootPath)
        {
            if (_isIndexing) return;
            _isIndexing = true;

            try
            {
                var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                                     .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));

                foreach (var file in files)
                {
                    var outline = await _loader.LoadFileOutlineAsync(file);
                    foreach (var type in outline.Types)
                    {
                        AddSymbol(type.Name, "Class", file);
                        foreach (var method in type.Methods)
                        {
                            AddSymbol(method.Name, "Method", file, type.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Indexing Error: {ex.Message}");
            }
            finally
            {
                _isIndexing = false;
            }
        }

        private static void AddSymbol(string name, string kind, string path, string parent = "")
        {
            string key = $"{kind}:{name}:{parent}";
            _symbolCache.TryAdd(key, new ProjectSymbol
            {
                Name = name,
                Kind = kind,
                FilePath = path,
                TypeName = parent
            });
        }

        public static List<AutocompleteSuggestion> GetProjectSuggestions(string wordPrefix)
        {
            return _symbolCache.Values
                .Where(s => s.Name.StartsWith(wordPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(s => new AutocompleteSuggestion
                {
                    Text = s.Name,
                    Description = $"{s.Kind} in {Path.GetFileName(s.FilePath)}",
                    Icon = s.Kind == "Class" ? "📦" : "m",
                    Score = 0.9
                })
                .OrderByDescending(x => x.Score)
                .Take(20)
                .ToList();
        }
    }
}
