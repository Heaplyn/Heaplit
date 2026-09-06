// Developer: heaplyn
// Date: 2026-08-15
// Summary: Universal persistence layer for overlay-specific state, configurations, and histories.
//          Allows any module to save/load arbitrary state objects as JSON without bloating the main settings.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HeaplitLauncher
{
    public static class PersistentStateManager
    {
        private static string StateDir => Path.Combine(PathHandler.GetDataDirectory(), "ModuleState");

        static PersistentStateManager()
        {
            if (!Directory.Exists(StateDir)) Directory.CreateDirectory(StateDir);
        }

        public static void SaveState<T>(string moduleName, T state)
        {
            try
            {
                string path = Path.Combine(StateDir, $"{moduleName}.json");
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Persistence-Error", $"Failed to save state for {moduleName}: {ex.Message}");
            }
        }

        public static T? LoadState<T>(string moduleName) where T : class, new()
        {
            try
            {
                string path = Path.Combine(StateDir, $"{moduleName}.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<T>(json);
                }
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Persistence-Error", $"Failed to load state for {moduleName}: {ex.Message}");
            }
            return new T();
        }

        public static void SaveHistory(string moduleName, string historyItem)
        {
            try
            {
                string path = Path.Combine(StateDir, $"{moduleName}_History.log");
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {historyItem}{Environment.NewLine}";
                File.AppendAllText(path, line);
            }
            catch { }
        }

        public static List<string> GetHistory(string moduleName, int limit = 50)
        {
            try
            {
                string path = Path.Combine(StateDir, $"{moduleName}_History.log");
                if (File.Exists(path))
                {
                    var lines = File.ReadAllLines(path).ToList();
                    lines.Reverse();
                    return lines.Take(limit).ToList();
                }
            }
            catch { }
            return new List<string>();
        }
    }
}
