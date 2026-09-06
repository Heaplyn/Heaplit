// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to open interactive Music Player & Playlist Manager GUI (`music`, `playlist`, `player`, `songs`).

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class MusicPlaylistCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            
            bool IsClose =  SearchUtil.IsClose(query,"music") ||
            SearchUtil.IsClose(query,"playlist") ||
            SearchUtil.IsClose(query,"player") ||
            SearchUtil.IsClose(query,"songs") ||
            SearchUtil.IsClose(query,"song") ||
             query == "music" ||
              query == "playlist" ||
               query == "player" ||
                query == "songs" ||
                 query == "song" || query.StartsWith("download") || query.StartsWith("dl ");
                Console.WriteLine(IsClose);
                return IsClose;
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string raw = query.Trim();
            string lower = raw.ToLower();

            if (lower.StartsWith("download ") || lower.StartsWith("dl "))
            {
                string targetUrl = raw.Substring(raw.IndexOf(' ') + 1).Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE = $"📥 Download Media to Playlist: {targetUrl}",
                    DESCRIPTION = "Downloads URL via DownloadMediaRunner TS engine directly to active playlist folder",
                    SIMILARITY = 6.0,
                    EXECUTE = () => MusicPlaylistOverlay.DownloadTrackFromUrl(targetUrl)
                });
                return suggestions;
            }
            else if (lower == "download" || lower == "dl")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "📥 Download Media to Playlist (Prompt)...",
                    DESCRIPTION = "Prompt for music/video URL (Spotify, YouTube, Deezer, etc.)",
                    SIMILARITY = 6.0,
                    EXECUTE = () => MusicPlaylistOverlay.DownloadTrackFromUrl(string.Empty)
                });
                return suggestions;
            }

            suggestions.Add(new CommandResult
            {
                TITLE       = "🎵 Open Music Player & Playlist Manager",
                DESCRIPTION = "Manage song folders, add audio files/links, and play music",
                SIMILARITY  = 8.0,
                EXECUTE     = () => MusicPlaylistOverlay.OpenPlayer()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("music / playlist", "Open Music Player & Playlist Manager GUI", "music"),
                new CommandDesc("download <url>", "Download music/audio link to playlist folder", "download https://...")
            };
        }
    }
}
