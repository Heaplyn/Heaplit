// Developer: heaplyn
// Date: 2026-09-05
// Summary: Command handler routing admin panel, data restoration, and player rollback queries.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class AdminCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "admin", "admin panel", "restore data", "restore player", "datastore restore", "rollback data", "player restore", "backup restore");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim().ToLower();

            suggestions.Add(new CommandResult
            {
                TITLE = "🛡️ Open Admin Panel & Data Restorer",
                DESCRIPTION = "Restore player DataStores, roll back versions, generate in-game recovery commands & system snapshots",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "admin", "admin panel", "restore data", "restore player", "datastore restore") + 9.5 * 0.01),
                EXECUTE = () => AdminPanelOverlay.ShowOverlay()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "🔄 Restore Player Data (Roblox DataStore)",
                DESCRIPTION = "Generate direct DataStore injection, snapshot rollback, or :restore in-game admin script",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "restore player", "restore data", "datastore", "rollback") + 9.0 * 0.01),
                EXECUTE = () => AdminPanelOverlay.ShowOverlay()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "💾 System Snapshots & Backups",
                DESCRIPTION = "Create and restore local Heaplit system state snapshots and settings",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "snapshot", "backup", "restore system", "system backup") + 8.5 * 0.01),
                EXECUTE = () => AdminPanelOverlay.ShowOverlay()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("admin", "Open admin panel & player data restorer", "admin"),
                new CommandDesc("restore data", "Restore player DataStores and roll back versions", "restore data"),
                new CommandDesc("admin panel", "Manage game admin tools and data snapshots", "admin panel")
            };
        }
    }
}
