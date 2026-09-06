// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to launch the glassmorphic text editor for a specific local file or default scratch note.

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace HeaplitLauncher
{
    public class EditCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "edit" || query.StartsWith("edit ");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double similarity = SearchUtil.GetSimilarity(parts.Length > 0 ? parts[0].ToLower() : "", "edit");

            if (parts.Length > 1)
            {
                string targetPath = query.Substring(parts[0].Length).Trim().Trim('"', '\'');
                bool isFolder = Directory.Exists(targetPath);

                suggestions.Add(new CommandResult
                {
                    TITLE       = isFolder ? $"📁 Open Workspace: {Path.GetFileName(targetPath)}" : $"Edit: {Path.GetFileName(targetPath)}",
                    DESCRIPTION = isFolder ? $"Open folder \"{targetPath}\" as a project workspace" : $"Open \"{targetPath}\" inside the built-in Heaplit Text Editor",
                    SIMILARITY  = 9.5,
                    EXECUTE     = () => { if (isFolder) TextEditorOverlay.OpenWorkspace(targetPath); else TextEditorOverlay.OpenFile(targetPath); }
                });
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE       = "📂 Open Project/Workspace...",
                    DESCRIPTION = "Open a full directory and work on all files in Heaplit Code Studio",
                    SIMILARITY  = similarity + 0.9,
                    EXECUTE     = () => {
                        Application.Current.Dispatcher.Invoke(() => {
                            var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select Project Folder" };
                            if (dlg.ShowDialog() == true) TextEditorOverlay.OpenWorkspace(dlg.FolderName);
                        });
                    }
                });

                suggestions.Add(new CommandResult
                {
                    TITLE       = "📝 Edit Single File...",
                    DESCRIPTION = "Browse and open a single source file",
                    SIMILARITY  = similarity + 0.8,
                    EXECUTE     = () => TextEditorOverlay.PromptAndOpenFile()
                });

                suggestions.Add(new CommandResult
                {
                    TITLE       = "Open Scratch Note",
                    DESCRIPTION = "Open a blank scratch.txt notepad inside the Heaplit Text Editor",
                    SIMILARITY  = similarity + 0.3,
                    EXECUTE     = () => TextEditorOverlay.OpenFile("scratch.txt")
                });
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("edit <path>", "Open a file or folder in AI Code Studio", "edit ."),
                new CommandDesc("edit", "Browse and edit a single file", "edit"),
                new CommandDesc("workspace", "Open a project directory", "edit C:\\projects\\heaplit")
            };
        }

        public void OnStart() { }
    }
}
