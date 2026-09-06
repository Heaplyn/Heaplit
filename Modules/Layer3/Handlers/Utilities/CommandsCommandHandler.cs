// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles 'commands' and 'help' queries by listing all available system command keywords, descriptions, and examples.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace HeaplitLauncher
{
    public class CommandsCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "commands" || query == "help" || query == "?" ||
                   query == "commands categories" || query == "categories";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();
            double similarity = SearchUtil.GetSimilarity(query, "commands");

            suggestions.Add(new CommandResult
            {
                TITLE       = "📂 Browse Commands by Category",
                DESCRIPTION = "Open an overlay grouping all commands into topic categories (System, Media, AI, etc.)",
                SIMILARITY  = similarity + 0.2,
                EXECUTE     = () => Application.Current.Dispatcher.Invoke(() => CommandCategoriesOverlay.ShowOverlay())
            });

            suggestions.Add(new CommandResult
            {
                TITLE       = "View System Commands",
                DESCRIPTION = "List all available Heaplit command actions, shortcuts, and parameter guidelines",
                SIMILARITY  = similarity,
                EXECUTE     = ShowCommandsList
            });

            return suggestions;
        }
        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("commands / help / ?", "List all supported commands", "commands"),
                new CommandDesc("commands categories", "Open categorized command browser overlay", "categories")
            };
        }

        private static void AddCmd(StringBuilder sb, string command, string description, string example)
        {
            sb.AppendLine($"{command,-24} {description,-38} {example}");
        }

        private static void ShowCommandsList()
        {
            var allDescs = CommandParser.GetAllCommandDescriptions();

            var sb = new StringBuilder();
            sb.AppendLine("=========================================================================================");
            sb.AppendLine("                           HEAPLIT LAUNCHER COMMAND HANDBOOK                              ");
            sb.AppendLine("=========================================================================================");
            sb.AppendLine(string.Format("{0,-24} {1,-38} {2}", "COMMAND", "DESCRIPTION", "EXAMPLE"));
            sb.AppendLine("-----------------------------------------------------------------------------------------");
            
            foreach (var cd in allDescs)
            {
                if (cd != null && cd.SHOW)
                {
                    AddCmd(sb, cd.COMMAND_NAME, cd.COMMAND_DESCRIPTION, cd.COMMAND_EXAMPLE);
                }
            }

            sb.AppendLine("-----------------------------------------------------------------------------------------");
            sb.AppendLine("💡 Tips:");
            sb.AppendLine("- You can press 'Enter' on any suggestion to execute it immediately.");
            sb.AppendLine("- Running 'push' automatically cleans build directories and resolves");
            sb.AppendLine("  Git index conflicts / credentials leak attempts dynamically.");
            sb.AppendLine("=========================================================================================");

            string output = sb.ToString();

            // Run on UI Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                CliOutputOverlay.Show("Command Handbook", output);
            });
        }
    }
}
