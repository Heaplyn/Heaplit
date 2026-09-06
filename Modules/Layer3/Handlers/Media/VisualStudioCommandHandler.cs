using System;
using System.Collections.Generic;
using System.Linq;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class VisualStudioCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.IsClose(query, "visual studio") ||
                   SearchUtil.IsClose(query, "visual editor") ||
                   SearchUtil.IsClose(query, "edit theme") ||
                   SearchUtil.IsClose(query, "vs visuals") ||
                   SearchUtil.IsClose(query, "suite") ||
                   SearchUtil.IsClose(query, "visual suite");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            return new List<CommandResult> {
                new CommandResult {
                    TITLE = "🎨 Open Heaplit Visuals",
                    DESCRIPTION = "Unified suite for colors, typography, motion, and system aesthetics.",
                    SIMILARITY = Math.Max(SearchUtil.GetSimilarity(query, "visual studio"), SearchUtil.GetSimilarity(query, "suite")),
                    EXECUTE = () => HeaplitVisualsOverlay.ShowOverlay()
                }
            };
        }
    }
}
