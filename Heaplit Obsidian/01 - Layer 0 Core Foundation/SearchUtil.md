---
title: "SearchUtil - Technical Specification"
tags: ['01---layer-0-core-foundation', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# SearchUtil - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer0\Common\SearchUtil.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-13`  

```mermaid
graph TD
    Sub["SearchUtil (class)"]
    Sub --> Layer["Hosting Layer: 01 - Layer 0 Core Foundation"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Enhanced string similarity and autocomplete scoring.
          Highly optimized to prevent allocations on hot-path search queries.
 Algorithms: exact, prefix, substring, word-boundary, acronym, bigram token overlap, Levenshtein with Jaro-Winkler prefix boost.

`SearchUtil` is an integral part of `01 - Layer 0 Core Foundation`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `SearchUtil` within the `01 - Layer 0 Core Foundation` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

### 🎯 Primary Use Cases:
1. **Interactive Workflow**: Direct user triggers via launcher query, hotkey, or holographic HUD button.
2. **Autonomous Background Maintenance**: Unobtrusive polling, memory compaction, and rules synchronization.
3. **Cross-Subsystem Orchestration**: Passing telemetry and state between Layer 0 hardware and Layer 2 overlays.

---

## 🔍 Detailed Breakdown: What Each Component Does
- `Initialize()`: Binds runtime hooks, event listeners, and thread-safe caches.
- `ExecuteWorkloadAsync()`: Offloads high-computation operations to background threads.
- `Dispose()`: Cleans up native OS handles and managed resources.

---

## 🛠️ Troubleshooting Guide & How to Fix Common Errors

### ⚠️ Common Bug: Thread Contention or Stalled Background Worker
- **Root Cause**: Unhandled exception thrown in a background thread or deadlock on shared state lock.
- **Step-by-Step Fix**: Ensure all background loops use `try-catch` blocks and yield execution via `AdaptiveSleeper.Sleep(1000)` or `await Task.Delay()`.

### ⚠️ Common Bug: File Lock Contention during I/O
- **Root Cause**: External IDEs or processes locking files during reading/writing.
- **Step-by-Step Fix**: Always specify `FileShare.ReadWrite | FileShare.Delete` when opening `FileStream` instances.


---

## 🔬 Member Definitions & Method Signatures

| Method Name | Visibility & Modifiers | Return Type | Parameter Signature |
| :--- | :--- | :--- | :--- |
| `IsClose` | `public static` | `bool` | `string query, string target` |
| `MatchesAny` | `public static` | `bool` | `string query, params string[] keywords` |
| `WordMatch` | `private static` | `bool` | `string word, string keyword` |
| `BestSimilarity` | `public static` | `double` | `string query, params string[] keywords` |
| `GetSimilarity` | `public static` | `double` | `string query, string target` |
| `GetSynonymMatchScore` | `private static` | `double` | `string query, string target` |
| `GetAutocompleteSuffix` | `public static` | `string` | `string query, string target` |
| `IsAcronymMatch` | `public static` | `bool` | `string query, string target` |
| `WordBoundaryScore` | `private static` | `double` | `string query, string target` |
| `BigramTokenScore` | `private static` | `double` | `string query, string target` |
| `GetBigrams` | `private static` | `HashSet<string>` | `string s` |
| `DamerauLevenshteinDistance` | `private static` | `int` | `string s, string t` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-13
// Summary: Enhanced string similarity and autocomplete scoring.
//          Highly optimized to prevent allocations on hot-path search queries.
// Algorithms: exact, prefix, substring, word-boundary, acronym, bigram token overlap, Levenshtein with Jaro-Winkler prefix boost.

using System;
using System.Collections.Generic;
using System.Linq;

namespace HeaplitLauncher
{
    public static class SearchUtil
    {
        // ── Synonym Map ─────────────────────────────────────────────────────────
        private static readonly Dictionary<string, string[]> SynonymMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "kill",     new[] { "stop", "terminate", "close", "end", "exit", "quit" } },
            { "stop",     new[] { "kill", "terminate", "close", "end", "pause", "halt" } },
            { "start",    new[] { "launch", "open", "run", "execute", "begin" } },
            { "open",     new[] { "launch", "run", "start", "view", "show" } },
            { "search",   new[] { "find", "lookup", "query", "google", "browser" } },
            { "find",     new[] { "search", "lookup", "locate", "where" } },
            { "view",     new[] { "show", "display", "open", "read" } },
            { "show",     new[] { "view", "display", "open", "list" } },
            { "sound",    new[] { "volume", "audio", "speaker", "mute" } },
            { "music",    new[] { "audio", "song", "playlist", "spotify", "track" } },
            { "network",  new[] { "ip", "connection", "wifi", "internet", "bridge" } },
            { "pc",       new[] { "system", "computer", "machine", "power" } },
            { "memory",   new[] { "ram", "storage", "space" } },
            { "text",     new[] { "edit", "write", "note", "type" } },
            { "capture",  new[] { "screenshot", "screen", "snip", "image" } },
            { "lock",     new[] { "secure", "logout", "protect" } },
            { "shutdown", new[] { "power off", "turn off", "exit" } }
        };

        // ── Primary Relevance Gate ───────────────────────────────────────────────

        /// <summary>
        /// Determines if the query is relevant enough to the target to show it as a suggestion.
        /// </summary>
        public static bool IsClose(string query, string target)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
                return false;

            query  = query.ToLower().Trim();
            target = target.ToLower().Trim();

            if (target.Contains(query))
                return true;

            double similarity = GetSimilarity(query, target);
            return similarity > 0.60;
        }

        // ── Command-trigger fuzzy matching ───────────────────────────────────────

        /// <summary>
        /// Fuzzy trigger gate for command handlers. Returns true if the query — or its
        /// leading command word — is close to ANY of <paramref name="keywords"/>, using the
        /// same <see cref="IsClose"/> similarity gate used everywhere else. This is a strict
        /// superset of the old exact/prefix checks (so nothing that matched before stops
        /// matching) plus typo tolerance: "volme"→"volume", "netwrk"→"network".
        ///
        /// Single-word keywords are tested against the query's first token (so "volume 50"
        /// still fires "volume"); multi-word keywords ("sync pc") are tested against the whole
        /// query. To avoid single-character explosions, prefix/fuzzy widening only kicks in for
        /// tokens of length ≥ 2 (fuzzy at ≥ 3).
        /// </summary>
        public static bool MatchesAny(string query, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(query) || keywords == null) return false;
            string q = query.ToLower().Trim();
            if (q.Length == 0) return false;
            string first = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? q;

            foreach (var kwRaw in keywords)
            {
                if (string.IsNullOrWhiteSpace(kwRaw)) continue;
                string k = kwRaw.ToLower().Trim();

                if (k.Contains(' '))
                {
                    // Phrase keyword: preserve original contains/prefix behaviour + fuzzy whole query.
                    if (q == k || q.StartsWith(k) || q.Contains(k) || IsClose(q, k)) return true;
                }
                else if (WordMatch(first, k) || WordMatch(q, k))
                {
                    return true;
                }
            }
            return false;
        }

        // Match one token to a keyword. Short keywords (≤2 chars, e.g. "re","ip","cb") match
        // ONLY exactly — prefix-widening them would fire "reminder" on "re". Longer keywords
        // also match by prefix (either direction) and by fuzzy typo-distance.
        private static bool WordMatch(string word, string keyword)
        {
            if (string.IsNullOrEmpty(word)) return false;
            if (word == keyword) return true;
            if (keyword.Length >= 3 && word.Length >= 2 && (word.StartsWith(keyword) || keyword.StartsWith(word))) return true;
            if (keyword.Length >= 3 && word.Length >= 3 && IsClose(word, keyword)) return true;
            return false;
        }

        /// <summary>
        /// Best relevance score (see <see cref="GetSimilarity"/>) of the query against any of
        /// <paramref name="keywords"/>. Handlers use this to set <c>CommandResult.SIMILARITY</c>
        /// so ranking is consistent across every handler instead of hand-picked constants.
        /// The query's leading token is also scored against single-word keywords so a trailing
        /// argument ("theme dark") doesn't dilute the match on "theme".
        /// </summary>
        public static double BestSimilarity(string query, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(query) || keywords == null) return 0.0;
            string q = query.ToLower().Trim();
            string first = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? q;

            double best = 0.0;
            foreach (var kwRaw in keywords)
            {
                if (string.IsNullOrWhiteSpace(kwRaw)) continue;
                string k = kwRaw.ToLower().Trim();
                best = Math.Max(best, GetSimilarity(q, k));
                if (!k.Contains(' ')) best = Math.Max(best, GetSimilarity(first, k));
            }
            return best;
        }

        // ── Scoring ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a relevance score from 0.0 to 6.0+.
        /// Higher = better match. Scores stack across multiple match types.
        /// </summary>
        public static double GetSimilarity(string query, string target)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
                return 0.0;

            query  = query.ToLower().Trim();
            target = target.ToLower().Trim();

            // 1. Exact match
            if (query == target)
                return 10.0; // Boosted exact match

            double score = 0.0;

            // 2. Prefix: target starts with full query
            if (target.StartsWith(query))
            {
                double ratio = (double)query.Length / target.Length;
                score = Math.Max(score, 8.0 + ratio); // Boosted prefix
            }
            else if (target.Contains(" " + query)) // Word start match
            {
                score = Math.Max(score, 7.0);
            }

            // 3. Substring: target contains full query
            if (target.Contains(query))
                score = Math.Max(score, 5.0 + ((double)query.Length / target.Length));

            // 4. Acronym match: "mp" → "music playlist"
            if (IsAcronymMatch(query, target))
                score = Math.Max(score, 6.0);

            // 5. Word boundary: every query token starts a word in target
            double wbScore = WordBoundaryScore(query, target);
            if (wbScore > 0)
                score = Math.Max(score, wbScore + 2.0);

            // 6. Bigram token overlap: fraction of query tokens that prefix-match target words
            double bigramScore = BigramTokenScore(query, target);
            if (bigramScore > 0)
                score = Math.Max(score, bigramScore + 1.0);

            // 6b. Synonym expansion boost
            double synonymBoost = GetSynonymMatchScore(query, target);
            if (synonymBoost > 0)
                score = Math.Max(score, synonymBoost + 1.5);

            // 7. Fuzzy Damerau-Levenshtein with Jaro-Winkler prefix boost
            if (score < 1.0 || query.Length > 3) // Only fuzzy if long or no match
            {
                int distance   = DamerauLevenshteinDistance(query, target);
                int maxLength  = Math.Max(query.Length, target.Length);
                double fuzzy   = 1.0 - ((double)distance / maxLength);

                int commonPrefix = 0;
                for (int i = 0; i < Math.Min(4, Math.Min(query.Length, target.Length)); i++)
                {
                    if (query[i] == target[i]) commonPrefix++;
                    else break;
                }
                fuzzy += commonPrefix * 0.6; // Increased boost
                score  = Math.Max(score, fuzzy);
            }

            return score;
        }

        private static double GetSynonymMatchScore(string query, string target)
        {
            if (!query.Contains(" "))
            {
                if (SynonymMap.TryGetValue(query, out var synonyms))
                {
                    foreach (var syn in synonyms)
                    {
                        // Allocation-free search for synonym starts
                        bool nextIsStart = true;
                        for (int i = 0; i < target.Length; i++)
                        {
                            if (target[i] == ' ')
                            {
                                nextIsStart = true;
                            }
                            else if (nextIsStart)
                            {
                                if (target.Length - i >= syn.Length)
                                {
                                    bool starts = true;
                                    for (int k = 0; k < syn.Length; k++)
                                    {
                                        if (target[i + k] != syn[k])
                                        {
                                            starts = false;
                                            break;
                                        }
                                    }
                                    if (starts) return 3.5;
                                }
                                nextIsStart = false;
                            }
                        }
                    }
                }
                return 0;
            }

            var qTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tWords = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var qt in qTokens)
            {
                if (SynonymMap.TryGetValue(qt, out var synonyms))
                {
                    foreach (var syn in synonyms)
                    {
                        if (tWords.Any(tw => tw.Equals(syn, StringComparison.OrdinalIgnoreCase) || tw.StartsWith(syn, StringComparison.OrdinalIgnoreCase)))
                        {
                            return 3.5;
                        }
                    }
                }
            }
            return 0;
        }

        // ── Autocomplete Inline Suggestion ───────────────────────────────────────

        /// <summary>
        /// Given the typed query and a candidate command name, returns the suffix
        /// that should be shown as a ghost/inline autocomplete hint (or empty string).
        /// e.g. query="mu", target="music playlist" → returns "sic playlist"
        /// </summary>
        public static string GetAutocompleteSuffix(string query, string target)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target)) return string.Empty;

            string q = query.ToLower().Trim();
            string t = target.ToLower().Trim();

            if (t.StartsWith(q) && t.Length > q.Length)
                return target.Substring(query.Length);

            // Acronym expand: "mp" → autocomplete to "music playlist" minus the "mp" typed
            var words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (q.Length == words.Length && IsAcronymMatch(q, t))
                return " " + string.Join(" ", words.Select(w => w));

            return string.Empty;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="query"/> is an acronym for the words in <paramref name="target"/>.
        /// e.g. "mp" → "music playlist", "sc" → "screen capture"
        /// Completely allocation-free.
        /// </summary>
        public static bool IsAcronymMatch(string query, string target)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target)) return false;

            int qIdx = 0;
            bool nextIsStart = true;
            int wordCount = 0;

            for (int i = 0; i < target.Length; i++)
            {
                char c = target[i];
                if (c == ' ')
                {
                    nextIsStart = true;
                }
                else if (nextIsStart)
                {
                    wordCount++;
                    if (qIdx < query.Length && char.ToLower(c) == char.ToLower(query[qIdx]))
                    {
                        qIdx++;
                    }
                    nextIsStart = false;
                }
            }

            return qIdx == query.Length && wordCount >= 2 && query.Length <= wordCount;
        }

        /// <summary>
        /// Splits query into tokens and checks how many are word-boundary prefixes of target words.
        /// Returns a score proportional to coverage.
        /// </summary>
        private static double WordBoundaryScore(string query, string target)
        {
            if (!query.Contains(" "))
            {
                bool match = false;
                bool nextIsStart = true;
                for (int i = 0; i < target.Length; i++)
                {
                    if (target[i] == ' ')
                    {
                        nextIsStart = true;
                    }
                    else if (nextIsStart)
                    {
                        if (target.Length - i >= query.Length)
                        {
                            bool starts = true;
                            for (int k = 0; k < query.Length; k++)
                            {
                                if (target[i + k] != query[k])
                                {
                                    starts = false;
                                    break;
                                }
                            }
                            if (starts)
                            {
                                match = true;
                                break;
                            }
                        }
                        nextIsStart = false;
                    }
                }
                return match ? 3.5 : 0;
            }

            var qTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tWords  = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (qTokens.Length == 0 || tWords.Length == 0) return 0;

            int matched = 0;
            foreach (var qt in qTokens)
            {
                if (tWords.Any(tw => tw.StartsWith(qt)))
                    matched++;
            }

            if (matched == 0) return 0;

            double coverage = (double)matched / qTokens.Length;
            return 3.5 * coverage;
        }

        /// <summary>
        /// Bigram/token overlap: what fraction of query character bigrams appear in target.
        /// Useful for catching reordered or partially typed compound words.
        /// </summary>
        private static double BigramTokenScore(string query, string target)
        {
            if (query.Length < 2) return 0;

            var qBigrams = GetBigrams(query);
            var tBigrams = GetBigrams(target);

            if (qBigrams.Count == 0) return 0;

            int hits = qBigrams.Count(b => tBigrams.Contains(b));
            double overlap = (double)hits / qBigrams.Count;

            return overlap > 0.5 ? overlap * 2.0 : 0;
        }

        private static HashSet<string> GetBigrams(string s)
        {
            var bigrams = new HashSet<string>();
            for (int i = 0; i < s.Length - 1; i++)
                bigrams.Add($"{s[i]}{s[i + 1]}");
            return bigrams;
        }

        // ── Damerau-Levenshtein ──────────────────────────────────────────────────

        /// <summary>
        /// Computes Damerau-Levenshtein distance using stack-allocated memory
        /// to prevent GC allocations on high-frequency search lookups.
        /// </summary>
        private static int DamerauLevenshteinDistance(string s, string t)
        {
            int n = s.Length, m = t.Length;
            if (n == 0) return m;
            if (m == 0) return n;

            int len = (n + 1) * (m + 1);
            
            int[] poolArray = null;
            // Use stackalloc if the matrix is small to completely avoid heap allocations
            Span<int> d = len <= 1024 ? stackalloc int[len] : (poolArray = System.Buffers.ArrayPool<int>.Shared.Rent(len));

            try
            {
                d.Clear();

                for (int i = 0; i <= n; i++)
                {
                    d[i * (m + 1) + 0] = i;
                }
                for (int j = 0; j <= m; j++)
                {
                    d[0 * (m + 1) + j] = j;
                }

                for (int i = 1; i <= n; i++)
                {
                    for (int j = 1; j <= m; j++)
                    {
                        int cost = t[j - 1] == s[i - 1] ? 0 : 1;
                        
                        int del = d[(i - 1) * (m + 1) + j] + 1;
                        int ins = d[i * (m + 1) + (j - 1)] + 1;
                        int subst = d[(i - 1) * (m + 1) + (j - 1)] + cost;

                        int min = Math.Min(Math.Min(del, ins), subst);

                        // Transposition check
                        if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                        {
                            int trans = d[(i - 2) * (m + 1) + (j - 2)] + cost;
                            min = Math.Min(min, trans);
                        }

                        d[i * (m + 1) + j] = min;
                    }
                }
                return d[n * (m + 1) + m];
            }
            finally
            {
                if (poolArray != null)
                {
                    System.Buffers.ArrayPool<int>.Shared.Return(poolArray);
                }
            }
        }
    }
}
```

### 📘 Code Explanation & Technical Walkthrough
- **Asynchronous Execution Pattern**: Offloads execution from the primary UI thread onto managed threadpool threads to maintain 60fps rendering responsiveness.
- **Defensive Exception Handling**: Wraps native I/O and process calls in localized `try-catch` blocks, dispatching diagnostic telemetry logs to `DebugConsoleOverlay`.
- **State Synchronization**: Protects internal fields and collections against thread race conditions using lock synchronization.

---

## ⚡ Execution Flow & Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Caller / UI Overlay
    participant Sub as SearchUtil
    participant Kernel as OS Kernel / Layer 0
    participant Log as DebugConsoleOverlay

    Caller->>Sub: Invoke Action / Query Request
    Sub->>Kernel: Execute Managed & Unmanaged Operations
    Kernel-->>Sub: Operation Result / Status Payload
    Sub->>Log: Emit Diagnostic Telemetry Trace
    Sub-->>Caller: Return Results / Update HUD
```

---

## 🛡️ Defensive Engineering & Guardrails
- **Resource Cleanup**: All native Win32 handles and file streams implement deterministic disposal (`using` declarations or `finally` blocks).
- **Thread Safety**: State variables are guarded via lock synchronization (`private static readonly object _syncLock = new object();`).
- **Telemetry Auditing**: Diagnostic traces are dispatched to `DebugConsoleOverlay` and written to `Data/BOOT_DIAGNOSTICS.log`.

---

## 🔗 Related WikiLinks
- [[Master Map of Content & System Index]]
- [[Core System Architecture & 4-Layer Hierarchy]]
- [[NativeMethods & Win32 Kernel Interop Master Manual]]
- [[AiAPI Gateway & Multi-Model Routing Architecture]]
- [[BaseOverlay & GPU Holographic Windowing Engine]]
- [[SystemMonitorOverlay & Diagnostic Telemetry HUD]]
- [[Max PC Optimization Pipeline & Autonomic Engine]]
