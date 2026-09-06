// Developer: heaplyn
// Date: 2026-08-20
// Summary: Command handler that opens the Discord message scraper/exporter overlay.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class DiscordScraperCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return SearchUtil.IsClose(query, "discord") ||
                   SearchUtil.IsClose(query, "discord scraper") ||
                   SearchUtil.IsClose(query, "discord log") ||
                   SearchUtil.IsClose(query, "discord export") ||
                   SearchUtil.IsClose(query, "scrape dms") ||
                   SearchUtil.IsClose(query, "discord dms");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = 0;
            if (SearchUtil.IsClose(query, "discord") ||
                SearchUtil.IsClose(query, "discord scraper") ||
                SearchUtil.IsClose(query, "discord dms"))
            {
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "discord"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "discord scraper"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "discord dms"));
                if (similarity < 5.0) similarity = 5.0;
            }
            else if (SearchUtil.IsClose(query, "discord log") ||
                     SearchUtil.IsClose(query, "discord export") ||
                     SearchUtil.IsClose(query, "scrape dms"))
            {
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "discord log"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "discord export"));
                similarity = Math.Max(similarity, SearchUtil.GetSimilarity(query, "scrape dms"));
                if (similarity < 4.0) similarity = 4.0;
            }

            suggestions.Add(new CommandResult
            {
                TITLE = "💬 Open Discord Chat Exporter",
                DESCRIPTION = "Configure Bot token, load server text channels or private DMs, and save chats to files",
                SIMILARITY = similarity + 1,
                EXECUTE = () => DiscordScraperOverlay.Open()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("discord", "Open Discord message scraper/exporter UI", "discord"),
                new CommandDesc("discord dms", "Open Discord DM scraper/exporter UI", "discord dms")
            };
        }
    }
}
