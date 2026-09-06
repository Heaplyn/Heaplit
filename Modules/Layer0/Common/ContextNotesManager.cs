// Developer: heaplyn
// Date: 2026-08-18
// Summary: Context Knowledge Base Manager.
//          Automatically maintains a directory of Markdown notes representing Heaplit's "External Brain".
//          Syncs memories, audio logs, chat summaries, and screen analysis into categorized files.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class ContextNotesManager
    {
        private static string GetNotesDir()
        {
            string path = CoreRegistry.Data.Settings.Current.CONTEXT_NOTES_PATH;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(PathHandler.GetDataDirectory(), "Context");
            }
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        public static void Initialize()
        {
            string dir = GetNotesDir();
            string[] coreFiles = {
                "Identity.md", "Projects.md", "System_State.md",
                "User_Preferences.md", "Chronology.md", "Instructions.md",
                "Audio_Logs.md", "Visual_Intelligence.md", "Neural_Architecture.md"
            };
            foreach (var f in coreFiles)
            {
                string fullPath = Path.Combine(dir, f);
                if (!File.Exists(fullPath))
                {
                    string header = f switch {
                        "Instructions.md" => "# Operational Instructions & Behavioral Rules\n*Core rules Heaplit MUST follow.*\n\n",
                        "Neural_Architecture.md" => "# Local Godellian Neural Schema\n*Details of the internal local neural net weights and topology.*\n\n",
                        _ => $"# {f.Replace(".md", "").Replace("_", " ")}\n*Initialized {DateTime.Now:F}*\n\n"
                    };
                    File.WriteAllText(fullPath, header);
                }
            }
            DebugConsoleOverlay.Log("ContextNotes", "Knowledge base re-initialized.");
        }

        public static async Task SyncMemoryToNotesAsync(MemoryNode memory)
        {
            if (!CoreRegistry.Data.Settings.Current.AUTO_SYNC_MEMORIES_TO_NOTES) return;

            string fileName = memory.Category switch
            {
                "Personal" => "Identity.md",
                "Project" => "Projects.md",
                "Activity" => "Chronology.md",
                "Knowledge" => "Architecture.md",
                "Audio" => "Audio_Logs.md",
                "Visual" => "Visual_Intelligence.md",
                _ => "General_Brainstorming.md"
            };

            string path = Path.Combine(GetNotesDir(), fileName);
            string entry = $"\n- [{memory.Timestamp:yyyy-MM-dd HH:mm}] {memory.Content}";

            try
            {
                await File.AppendAllTextAsync(path, entry);

                // Periodic Restructuring
                FileInfo fi = new FileInfo(path);
                if (fi.Length > 25000)
                {
                    _ = Task.Run(() => RestructureNoteAsync(path));
                }
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("ContextNotes-Error", $"Failed to sync: {ex.Message}");
            }
        }

        public static async Task AddChatSummaryAsync(string summary)
        {
            string path = Path.Combine(GetNotesDir(), "Chronology.md");
            string entry = $"\n## Chat Session Summary - {DateTime.Now:yyyy-MM-dd HH:mm}\n{summary}\n";
            try { await File.AppendAllTextAsync(path, entry); } catch { }
        }

        private static async Task RestructureNoteAsync(string path)
        {
            try
            {
                string content = await File.ReadAllTextAsync(path);
                string fileName = Path.GetFileName(path);

                string prompt = $"### TASK\nRestructure this collection of notes for '{fileName}' into a professional, clean Markdown document. " +
                                $"Keep all critical facts, group them logically under headers, and remove duplicates. " +
                                $"Return ONLY the final Markdown.\n\n### DATA\n{content}";

                string clean = await LlmRouter.AskAsync(prompt, null);
                if (!string.IsNullOrWhiteSpace(clean) && !clean.StartsWith("⚠️"))
                {
                    await File.WriteAllTextAsync(path, clean);
                    DebugConsoleOverlay.Log("ContextNotes", $"Restructured {fileName}.");
                }
            }
            catch { }
        }

        public static string GetAllNotesContext()
        {
            var sb = new StringBuilder();
            try
            {
                string dir = GetNotesDir();
                var files = Directory.GetFiles(dir, "*.md");
                foreach (var f in files.Take(12))
                {
                    string content = File.ReadAllText(f);
                    if (content.Length > 2000) content = content.Substring(0, 2000) + "... [Pruned]";
                    sb.AppendLine($"--- SOURCE: {Path.GetFileName(f)} ---");
                    sb.AppendLine(content);
                }
            }
            catch { }
            return sb.ToString();
        }
    }
}
