// Developer: heaplyn
// Date: 2026-08-13
// Summary: Lightweight frequency-based ML model that records (query → chosen result) pairs
// and boosts future suggestion scores for commonly-selected entries. Persists to Data/usage_model.json.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HeaplitLauncher
{
    public static class QueryLearner
    {
        private static readonly HashSet<string> InvalidTitles = new(StringComparer.OrdinalIgnoreCase)
        {
            "open", "start", "run", "launch", "search", "show", "play", "kill", "run: open", "run: start"
        };

        // key: "normalizedQuery|resultTitle"  →  value: hit count
        private static Dictionary<string, int> _model = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string _modelPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data", "usage_model.json");

        private static bool _loaded = false;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Records that the user selected <paramref name="resultTitle"/> after typing <paramref name="query"/>.
        /// Call this every time the user executes a suggestion from the results list.
        /// </summary>
        public static void RecordSelection(string query, string resultTitle)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(resultTitle)) return;

            string cleanTitle = CleanTitle(resultTitle);
            if (InvalidTitles.Contains(cleanTitle)) return;

            string key = MakeKey(query, cleanTitle);
            _model.TryGetValue(key, out int count);
            _model[key] = count + 1;

            // Also record a prefix-agnostic key so partial matches learn from full queries
            var tokens = NormalizeQuery(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length >= 2 && token != NormalizeQuery(query))
                {
                    string prefixKey = MakeKey(token, cleanTitle);
                    _model.TryGetValue(prefixKey, out int pc);
                    _model[prefixKey] = pc + 1;
                }
            }

            SaveAsync();
        }

        /// <summary>
        /// Returns a score boost [0.0 – 3.0] for a result based on past usage frequency.
        /// </summary>
        public static double GetBoost(string query, string resultTitle)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(resultTitle)) return 0.0;

            string cleanTitle = CleanTitle(resultTitle);
            if (InvalidTitles.Contains(cleanTitle)) return 0.0;

            string key = MakeKey(query, cleanTitle);
            _model.TryGetValue(key, out int count);

            return count > 0 ? Math.Min(3.0, Math.Sqrt(count) * 0.5) : 0.0;
        }

        /// <summary>
        /// Returns the top-N most frequently chosen results for any query prefix.
        /// </summary>
        public static List<(string ResultTitle, string OriginalQuery, int Count)> GetTopResults(string queryPrefix, int topN = 5)
        {
            EnsureLoaded();
            string prefix = NormalizeQuery(queryPrefix);
            var hits = new List<(string, string, int)>();

            foreach (var kvp in _model)
            {
                var parts = kvp.Key.Split('\0');
                if (parts.Length == 2 && parts[0].StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && kvp.Value > 0)
                {
                    string title = parts[1];
                    string origQuery = parts[0];
                    if (!InvalidTitles.Contains(title))
                    {
                        hits.Add((title, origQuery, kvp.Value));
                    }
                }
            }

            hits.Sort((a, b) => b.Item3.CompareTo(a.Item3));
            return hits.Count > topN ? hits.GetRange(0, topN) : hits;
        }

        // ── Internals ────────────────────────────────────────────────────────────

        private static string CleanTitle(string t)
        {
            string clean = t.Trim();
            if (clean.StartsWith("⭐ ")) clean = clean.Substring(2).Trim();
            if (clean.StartsWith("Run: ")) clean = clean.Substring(5).Trim();
            return clean;
        }

        private static string MakeKey(string query, string resultTitle)
            => $"{NormalizeQuery(query)}\0{CleanTitle(resultTitle).ToLowerInvariant()}";

        private static string NormalizeQuery(string q)
            => q.Trim().ToLowerInvariant();

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (File.Exists(_modelPath))
                {
                    string json = File.ReadAllText(_modelPath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                    if (loaded != null)
                    {
                        // Purge invalid entries
                        _model = loaded.Where(kvp =>
                        {
                            var parts = kvp.Key.Split('\0');
                            if (parts.Length == 2 && InvalidTitles.Contains(parts[1])) return false;
                            return true;
                        }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            catch { /* corrupt model — start fresh */ }
        }

        private static void SaveAsync()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string? dir = Path.GetDirectoryName(_modelPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_modelPath, json);
                }
                catch { }
            });
        }
    }
}
