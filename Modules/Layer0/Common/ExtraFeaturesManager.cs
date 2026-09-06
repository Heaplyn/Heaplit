// Developer: heaplyn
// Date: 2026-08-09
// Summary: Manages text snippets, application shortcuts, and system monitor overlay data structures.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HeaplitLauncher
{
    public class SnippetItem
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class AppShortcutItem
    {
        public string Name { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public string IconEmoji { get; set; } = "🚀";
    }

    public static class ExtraFeaturesManager
    {
        private static string GetFilePath(string fileName)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDir))
            {
                string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Data"));
                if (Directory.Exists(devPath))
                {
                    dataDir = devPath;
                }
                else
                {
                    Directory.CreateDirectory(dataDir);
                }
            }
            return Path.Combine(dataDir, fileName);
        }

        // --- SNIPPETS ---
        public static List<SnippetItem> LoadSnippets()
        {
            try
            {
                string path = GetFilePath("Snippets.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<List<SnippetItem>>(json) ?? new List<SnippetItem>();
                }
            }
            catch { }
            return new List<SnippetItem>();
        }

        public static void SaveSnippets(List<SnippetItem> items)
        {
            try
            {
                string path = GetFilePath("Snippets.json");
                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public static void AddSnippet(string name, string content)
        {
            var snippets = LoadSnippets();
            snippets.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            snippets.Add(new SnippetItem { Name = name, Content = content, CreatedAt = DateTime.Now });
            SaveSnippets(snippets);
            TextOverlay.Show($"✂️ Snippet '{name}' saved!", 2500);
        }

        public static void DeleteSnippet(string name)
        {
            var snippets = LoadSnippets();
            int removed = snippets.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                SaveSnippets(snippets);
                TextOverlay.Show($"🗑️ Snippet '{name}' deleted!", 2500);
            }
        }

        // --- APP SHORTCUTS ---
        public static List<AppShortcutItem> LoadAppShortcuts()
        {
            try
            {
                string path = GetFilePath("AppShortcuts.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<List<AppShortcutItem>>(json) ?? GetDefaultApps();
                }
            }
            catch { }
            return GetDefaultApps();
        }

        public static void SaveAppShortcuts(List<AppShortcutItem> items)
        {
            try
            {
                string path = GetFilePath("AppShortcuts.json");
                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public static void AddAppShortcut(string name, string targetPath)
        {
            var apps = LoadAppShortcuts();
            apps.RemoveAll(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            apps.Add(new AppShortcutItem { Name = name, TargetPath = targetPath, IconEmoji = "🚀" });
            SaveAppShortcuts(apps);
            TextOverlay.Show($"📱 App shortcut '{name}' registered!", 2500);
        }

        private static List<AppShortcutItem> GetDefaultApps()
        {
            return new List<AppShortcutItem>
            {
                new AppShortcutItem { Name = "notepad", TargetPath = "notepad.exe", IconEmoji = "📝" },
                new AppShortcutItem { Name = "calc", TargetPath = "calc.exe", IconEmoji = "🧮" },
                new AppShortcutItem { Name = "cmd", TargetPath = "cmd.exe", IconEmoji = "💻" },
                new AppShortcutItem { Name = "explorer", TargetPath = "explorer.exe", IconEmoji = "📁" },
                new AppShortcutItem { Name = "chrome", TargetPath = "chrome.exe", IconEmoji = "🌐" }
            };
        }
    }
}
