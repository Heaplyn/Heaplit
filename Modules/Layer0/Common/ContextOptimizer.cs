// Developer: heaplyn
// Date: 2026-08-15
// Summary: Context Health & Integrity Monitor.
//          Filters and prunes dynamic AI context to prevent "Context Rot" (stale/irrelevant info).
//          Ensures system prompts stay high-signal and within token efficiency limits.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HeaplitLauncher
{
    public static class ContextOptimizer
    {
        private const int MAX_TOTAL_CONTEXT_CHARS = 12000; // ~3k-4k tokens safety limit

        public static string PruneAndOptimize(string rawContext)
        {
            if (string.IsNullOrWhiteSpace(rawContext)) return rawContext;

            // 1. Remove duplicate lines (common in recursive file scans)
            var lines = rawContext.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(l => l.Trim())
                                  .Distinct()
                                  .ToList();

            // 2. Filter out stale activity indicators (anything older than current date/hour if timestamped)
            // (Handled internally by managers now, but this is a secondary pass)

            // 3. Dynamic Trimming: If context is too large, prune the middle (usually less relevant)
            if (rawContext.Length > MAX_TOTAL_CONTEXT_CHARS)
            {
                int keepHead = MAX_TOTAL_CONTEXT_CHARS / 3;
                int keepTail = (MAX_TOTAL_CONTEXT_CHARS / 3) * 2;

                string head = rawContext.Substring(0, keepHead);
                string tail = rawContext.Substring(rawContext.Length - (MAX_TOTAL_CONTEXT_CHARS - keepHead));

                return head + "\n\n... [PRUNED STALE CONTEXT TO PREVENT ROT] ...\n\n" + tail;
            }

            return string.Join("\n", lines);
        }

        public static string ScrubRedundantMetadata(string text)
        {
            // Remove excessive whitespace and redundant action tags that might have leaked
            string cleaned = Regex.Replace(text, @"\n{3,}", "\n\n");
            cleaned = Regex.Replace(cleaned, @"\[SPEECH:.*?\]", "");
            return cleaned.Trim();
        }
    }
}
