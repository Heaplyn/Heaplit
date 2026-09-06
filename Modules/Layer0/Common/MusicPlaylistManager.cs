// Developer: heaplyn
// Date: 2026-08-09
// Summary: Data models and persistence manager for music playlists, custom folders, track metadata, and online stream links inside Data/MusicPlaylists.json.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HeaplitLauncher
{
    public class MusicTrack
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = "Unknown Artist";
        public string PathOrUrl { get; set; } = string.Empty; // Local .mp3/.wav/.flac path OR web stream URL
        public bool IsStreamUrl { get; set; } = false;
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class MusicFolder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FolderName { get; set; } = "Default Playlist";
        public List<MusicTrack> Tracks { get; set; } = new List<MusicTrack>();
    }

    public class MusicLibraryData
    {
        public List<MusicFolder> Folders { get; set; } = new List<MusicFolder>();
        public string LastActiveFolderId { get; set; } = string.Empty;
    }

    public static class MusicPlaylistManager
    {
        private static string GetFilePath()
        {
            string dataDir = PathHandler.GetDataDirectory();
            return Path.Combine(dataDir, "MusicPlaylists.json");
        }

        public static MusicLibraryData LoadLibrary()
        {
            try
            {
                string p = GetFilePath();
                if (File.Exists(p))
                {
                    string json = File.ReadAllText(p);
                    var data = JsonSerializer.Deserialize<MusicLibraryData>(json);
                    if (data != null && data.Folders.Count > 0)
                    {
                        DebugConsoleOverlay.Log("Music", $"Loaded playlist library from {p} ({data.Folders.Sum(f => f.Tracks.Count)} tracks)");
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Music-Error", $"Failed to load playlist library: {ex.Message}");
            }

            // Default initial setup
            DebugConsoleOverlay.Log("Music", "Creating new default playlist library.");
            var defaultLibrary = new MusicLibraryData();
            var allSongsFolder = new MusicFolder { FolderName = "🎵 All Songs" };
            var defaultFolder = new MusicFolder { FolderName = "Favorites" };
            
            defaultLibrary.Folders.Add(allSongsFolder);
            defaultLibrary.Folders.Add(defaultFolder);
            defaultLibrary.LastActiveFolderId = allSongsFolder.Id;
            SaveLibrary(defaultLibrary);
            return defaultLibrary;
        }

        public static void AddTrackToFolderAndAllSongs(MusicLibraryData library, MusicFolder targetFolder, MusicTrack track)
        {
            // 1. Add track to target folder
            if (!targetFolder.Tracks.Any(t => t.PathOrUrl == track.PathOrUrl))
            {
                targetFolder.Tracks.Add(track);
            }

            // 2. Add track to "🎵 All Songs" folder automatically
            var allSongsFolder = library.Folders.FirstOrDefault(f => f.FolderName == "🎵 All Songs");
            if (allSongsFolder == null)
            {
                allSongsFolder = new MusicFolder { FolderName = "🎵 All Songs" };
                library.Folders.Insert(0, allSongsFolder);
            }

            if (!allSongsFolder.Tracks.Any(t => t.PathOrUrl == track.PathOrUrl))
            {
                var clone = new MusicTrack
                {
                    Title = track.Title,
                    Artist = track.Artist,
                    PathOrUrl = track.PathOrUrl,
                    IsStreamUrl = track.IsStreamUrl,
                    AddedAt = track.AddedAt
                };
                allSongsFolder.Tracks.Add(clone);
            }

            SaveLibrary(library);
        }

        public static void SaveLibrary(MusicLibraryData data)
        {
            if (data == null) return;
            try
            {
                string p = GetFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);

                // Write to a temporary file first to prevent corruption
                string tempPath = p + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(p)) File.Delete(p);
                File.Move(tempPath, p);

                DebugConsoleOverlay.Log("Music-System", $"Playlist library successfully persisted to disk ({data.Folders.Count} folders).");
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Music-Error", $"Failed to save playlist library: {ex.Message}");
            }
        }
    }
}
