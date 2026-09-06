// Developer: heaplyn
// Date: 2026-08-20
// Summary: Command handler that opens the code editor assistant overlay.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class EditorAssistantCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "editor", "assistant", "imports", "boilerplate", "snippets", "insert");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = SearchUtil.BestSimilarity(query, "editor", "assistant", "imports", "boilerplate", "snippets", "insert");
            if (query == "editor" || query == "assistant" || query == "imports") similarity = 3.0;
            else if (query == "boilerplate" || query == "snippets" || query == "insert") similarity = 2.5;

            suggestions.Add(new CommandResult
            {
                TITLE = "💻 Open Code Editor Assistant",
                DESCRIPTION = "Paste state boilerplate, setup configs, and imports directly into active IDEs",
                SIMILARITY = similarity + 0.5,
                EXECUTE = () => EditorAssistantOverlay.Open()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("editor", "Open Heaplit Code Editor Assistant", "editor"),
                new CommandDesc("imports", "Quickly paste programming imports/headers", "imports"),
                new CommandDesc("boilerplate", "Quickly paste boilerplate code", "boilerplate")
            };
        }
    }
}
