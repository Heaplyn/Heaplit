// Developer: heaplyn
// Date: 2026-08-13
// Summary: Command handler for animation preferences, motion speeds, and visual options overlay.

using System.Collections.Generic;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class AnimationCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "anim", "animation", "animations", "fx", "visuals", "motion");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var results = new List<CommandResult>();

            results.Add(new CommandResult
            {
                TITLE = "✨ Open Heaplit Visuals",
                DESCRIPTION = "Unified suite for motion, typography, and visual effects.",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "anim", "animation", "animations", "fx", "visuals", "motion") + 5.5 * 0.01),
                EXECUTE = () => HeaplitVisualsOverlay.ShowOverlay()
            });

            return results;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("anim", "Configure HUD animations & motion presets", "anim"),
                new CommandDesc("visuals", "Adjust visual effects, glow, & opacity options", "visuals")
            };
        }

        public void OnStart() { }
    }
}
