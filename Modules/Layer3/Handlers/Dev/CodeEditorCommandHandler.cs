using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher
{
    public class CodeEditorCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "code", "vscode", "cursor", "vs", "ide", "editors");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();
            string path = parts.Length > 1 ? query.Substring(cmd.Length).Trim() : "";

            if (cmd == "editors")
            {
                var editors = CodeEditorManager.GetInstalledEditors();
                suggestions.Add(new CommandResult
                {
                    TITLE = "Installed Code Editors",
                    DESCRIPTION = editors.Count > 0 ? string.Join(", ", editors) : "No major IDEs detected in PATH",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "code", "vscode", "cursor", "vs", "ide", "editors") + 5.0 * 0.01)
                });
                return suggestions;
            }

            if (cmd == "code" || cmd == "vscode")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"Open in VS Code: {Path.GetFileName(path)}",
                    DESCRIPTION = $"Launch Visual Studio Code for {path}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "code", "vscode", "cursor", "vs", "ide", "editors") + 4.5 * 0.01),
                    EXECUTE = () => CodeEditorManager.OpenInVSCode(path)
                });
            }
            else if (cmd == "cursor")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"Open in Cursor: {Path.GetFileName(path)}",
                    DESCRIPTION = $"Launch Cursor IDE for {path}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "code", "vscode", "cursor", "vs", "ide", "editors") + 4.5 * 0.01),
                    EXECUTE = () => CodeEditorManager.OpenInCursor(path)
                });
            }
            else if (cmd == "vs")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"Open in Visual Studio: {Path.GetFileName(path)}",
                    DESCRIPTION = $"Launch MS Visual Studio for {path}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "code", "vscode", "cursor", "vs", "ide", "editors") + 4.5 * 0.01),
                    EXECUTE = () => CodeEditorManager.OpenInVisualStudio(path)
                });
            }

            return suggestions;
        }
    }
}
