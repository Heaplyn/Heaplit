// Developer: heaplyn
// Date: 2026-08-21
// Summary: Memory Management Command Handler.
// Provides commands like 'purge', 'clean', and 'gc' to manually release system resources and force garbage collection.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class MemoryCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "gc", "optimize memory", "free memory");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            if (CanHandle(query))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🧹 Purge System Memory",
                    DESCRIPTION = "Release heavy assets, clear caches, and force Garbage Collection",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "gc", "optimize memory", "free memory") + 2.0 * 0.01),
                    EXECUTE = () =>
                    {
                        BaseOverlay.PurgeSystemMemory();
                        TextOverlay.Show("⚡ SYSTEM MEMORY PURGED & OPTIMIZED", 2500);
                    }
                });
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("purge / clean", "Optimize memory and release heavy assets", "purge")
            };
        }
    }
}
