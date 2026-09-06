// Developer: heaplyn
// Date: 2026-08-14
// Summary: Workspace Code Memory Manager.
//          Saves and loads code contexts the user is actively writing to Data/WorkspaceMemory.json.
//          Enables Heaplit AI Companion to retain full context of the user's active code edits.

using System;
using System.IO;
using System.Text.Json;

namespace HeaplitLauncher
{
    public class WorkspaceMemory
    {
        public string ActiveFilePath { get; set; } = string.Empty;
        public string ActiveFileName { get; set; } = string.Empty;
        public string ActiveCodeSnippet { get; set; } = string.Empty;
        public string ActiveProgrammingLanguage { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public static class WorkspaceMemoryManager
    {
        private static WorkspaceMemory _currentMemory = new WorkspaceMemory();
        private static readonly object _lock = new object();

        static WorkspaceMemoryManager()
        {
            Load();
        }

        private static string GetFilePath()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            return Path.Combine(dataDir, "WorkspaceMemory.json");
        }

        public static void Load()
        {
            lock (_lock)
            {
                try
                {
                    string path = GetFilePath();
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        _currentMemory = JsonSerializer.Deserialize<WorkspaceMemory>(json) ?? new WorkspaceMemory();

                        // Auto-refresh the code snippet from the physical file on disk if it still exists
                        if (!string.IsNullOrEmpty(_currentMemory.ActiveFilePath) && File.Exists(_currentMemory.ActiveFilePath))
                        {
                            try
                            {
                                string content = File.ReadAllText(_currentMemory.ActiveFilePath);
                                _currentMemory.ActiveCodeSnippet = content.Length > 4000 ? content.Substring(0, 4000) + "\n// [Truncated...]" : content;
                                _currentMemory.LastUpdated = DateTime.Now;
                                // Save the refreshed memory to WorkspaceMemory.json
                                string refreshedJson = JsonSerializer.Serialize(_currentMemory, new JsonSerializerOptions { WriteIndented = true });
                                File.WriteAllText(path, refreshedJson);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to auto-refresh workspace file: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        _currentMemory = new WorkspaceMemory();
                    }
                }
                catch
                {
                    _currentMemory = new WorkspaceMemory();
                }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    string path = GetFilePath();
                    string json = JsonSerializer.Serialize(_currentMemory, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(path, json);
                }
                catch { }
            }
        }

        public static void UpdateActiveCode(string filePath, string code, string language)
        {
            lock (_lock)
            {
                _currentMemory.ActiveFilePath = filePath;
                _currentMemory.ActiveFileName = Path.GetFileName(filePath);
                
                // Limit code snippet to the first 4000 characters to keep LLM context clean and compact
                _currentMemory.ActiveCodeSnippet = code.Length > 4000 ? code.Substring(0, 4000) + "\n// [Truncated...]" : code;
                _currentMemory.ActiveProgrammingLanguage = language;
                _currentMemory.LastUpdated = DateTime.Now;
                Save();
            }
            DebugConsoleOverlay.Log("WorkspaceMemory", $"Updated context for {Path.GetFileName(filePath)} ({language}).");
        }

        public static WorkspaceMemory GetCurrent()
        {
            lock (_lock)
            {
                return _currentMemory;
            }
        }

        public static string GetWorkspaceContextPrompt()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_currentMemory.ActiveCodeSnippet))
                {
                    return string.Empty;
                }

                // If memory is older than 3 hours, treat it as stale
                if ((DateTime.Now - _currentMemory.LastUpdated).TotalHours > 3)
                {
                    return string.Empty;
                }

                return $"\n\n[WORKSPACE CODE CONTEXT]\n" +
                       $"Active File: {_currentMemory.ActiveFileName}\n" +
                       $"Language: {_currentMemory.ActiveProgrammingLanguage}\n" +
                       $"Code Content:\n" +
                       $"```\n{_currentMemory.ActiveCodeSnippet}\n```\n" +
                       $"Please keep this code in memory. If the user asks questions or makes requests, refer to this active code context.";
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _currentMemory = new WorkspaceMemory();
                Save();
                try
                {
                    string path = GetFilePath();
                    if (File.Exists(path)) File.Delete(path);
                }
                catch { }
            }
            DebugConsoleOverlay.Log("WorkspaceMemory", "Workspace code memory cleared.");
        }
    }
}
