// Developer: heaplyn
// Date: 2026-08-14
// Summary: Handles CLI/HUD command suggestions for starting the C# to iOS compilation GUI.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class IpaCompilerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "compile ipa", "compile ios", "ipa", "ipa compiler", "ipa studio");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();

            suggestions.Add(new CommandResult
            {
                TITLE = "🍎 Open C# to iOS IPA Compiler Studio",
                DESCRIPTION = "Select a C# project, build into an IPA, and download directly to your connected phone",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "compile ipa", "compile ios", "ipa", "ipa compiler", "ipa studio") + 7.0 * 0.01),
                EXECUTE = () => IpaCompilerOverlay.ShowOverlay()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("compile ipa", "Open iOS compilation studio to build C# projects into IPA packages", "compile ipa")
            };
        }
    }
}
