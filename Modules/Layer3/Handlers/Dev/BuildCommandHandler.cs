// Developer: heaplyn
// Date: 2026-08-16
// Summary: Command handler for the Universal Build Studio.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class BuildCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "build", "compile", "make", "build hub");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();

            suggestions.Add(new CommandResult
            {
                TITLE = "🛠️ Open Universal Build Studio",
                DESCRIPTION = "Compile multi-language projects (C#, Python, JS, C++) with AI analysis",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "build", "compile", "make", "build hub") + 9.0 * 0.01),
                EXECUTE = () => BuildStudioOverlay.ShowOverlay()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("build", "Launch Universal Build & Compile Studio", "build")
            };
        }

        public void OnStart() { }
    }
}
