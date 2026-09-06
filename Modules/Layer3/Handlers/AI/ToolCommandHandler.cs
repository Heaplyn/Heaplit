// Developer: heaplyn
// Date: 2026-08-19
// Summary: Command handler for AI Tool Orchestrator and Registry.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class ToolCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "tools", "orchestrator", "registry", "tool manager");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            return new List<CommandResult>
            {
                new CommandResult
                {
                    TITLE = "🛠️ Open AI Tool Orchestrator",
                    DESCRIPTION = "Manage and register autonomous AI tools and capabilities.",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "tools", "orchestrator", "registry", "tool manager") + 1.0 * 0.01),
                    EXECUTE = () => ToolManagerOverlay.ShowOverlay()
                }
            };
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("tools", "Open AI Tool Orchestrator", "tools")
            };
        }
    }
}
