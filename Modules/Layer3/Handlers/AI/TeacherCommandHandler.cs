// Developer: heaplyn
// Date: 2026-08-14
// Summary: Handles commands to toggle teacher mode and run code scans using the Code Teacher Manager.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class TeacherCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "teacher");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim();
            string lower = q.ToLower();

            // 1. Toggle Teacher Mode
            if (lower == "teacher toggle" || lower == "teacher")
            {
                bool nextState = !SettingsManager.Current.IS_TEACHER_MODE_ENABLED;
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🎓 Toggle Teacher Mode (Currently {(SettingsManager.Current.IS_TEACHER_MODE_ENABLED ? "Enabled" : "Disabled")})",
                    DESCRIPTION = $"Switch teaching assistance to {(nextState ? "Enabled" : "Disabled")}",
                    EXECUTE = () =>
                    {
                        SettingsManager.Current.IS_TEACHER_MODE_ENABLED = nextState;
                        SettingsManager.Save();
                        string msg = $"Teacher Mode is now {(nextState ? "Active" : "Inactive")}.";
                        TtsManager.Speak(msg);
                        TextOverlay.Show($"🎓 Teacher Mode: {(nextState ? "ON" : "OFF")}", 3000);
                    },
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "teacher") + 8.5 * 0.01)
                });
            }

            // 1b. Open Teacher Studio (goal-aware live tutor)
            if (lower == "teacher" || lower.Contains("studio") || lower.Contains("goal") || lower.Contains("tutor"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🎓 Open Teacher Studio",
                    DESCRIPTION = "Set a goal and let HEAPLIT generate its own triggers to tutor you live while you code",
                    EXECUTE = () => TeacherStudioOverlay.ShowOverlay(),
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "teacher") + 9.0 * 0.01)
                });
            }

            // 2. Scan File/Project
            if (lower.StartsWith("teacher scan"))
            {
                string target = q.Substring(12).Trim();

                suggestions.Add(new CommandResult
                {
                    TITLE = string.IsNullOrEmpty(target) ? "🔍 Scan Recently Changed Project Files" : $"🔍 Scan File '{target}' for Anti-Patterns",
                    DESCRIPTION = "Analyze code files for deprecated classes, bugs, or performance issues",
                    EXECUTE = () =>
                    {
                        Task.Run(async () =>
                        {
                            if (!string.IsNullOrEmpty(target))
                            {
                                string result = await CodeTeacherManager.ScanFileAsync(target);
                                if (!result.Contains("looks clean!") && !result.Contains("No issues found") && result != "duplicate_suppressed")
                                {
                                    ChatOverlay.ShowChat();
                                    await ChatOverlay.SubmitTextMessage("teacher scan report:\n" + result);
                                }
                            }
                            else
                            {
                                // Scan recently modified files (last 2 hours) in project directory
                                string checkDir = AppDomain.CurrentDomain.BaseDirectory;
                                string projectRoot = checkDir;
                                for (int i = 0; i < 5; i++)
                                {
                                    if (File.Exists(Path.Combine(checkDir, "HeaplitLauncher.csproj")))
                                    {
                                        projectRoot = checkDir;
                                        break;
                                    }
                                    var parent = Directory.GetParent(checkDir);
                                    if (parent == null) break;
                                    checkDir = parent.FullName;
                                }

                                try
                                {
                                    var files = Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                                        .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                                        .Select(f => new FileInfo(f))
                                        .Where(fInfo => (DateTime.Now - fInfo.LastWriteTime).TotalHours <= 2.0)
                                        .ToList();

                                    if (files.Count == 0)
                                    {
                                        TextOverlay.Show("🔍 Scan Complete: No modified files to scan.", 3000);
                                        return;
                                    }

                                    foreach (var fInfo in files)
                                    {
                                        string result = await CodeTeacherManager.ScanFileAsync(fInfo.FullName);
                                        if (!result.Contains("looks clean!") && !result.Contains("No issues found") && result != "duplicate_suppressed")
                                        {
                                            ChatOverlay.ShowChat();
                                            await ChatOverlay.SubmitTextMessage($"teacher scan report for {fInfo.Name}:\n" + result);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    TextOverlay.Show($"❌ Error scanning directory: {ex.Message}", 3000);
                                }
                            }
                        });
                    },
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "teacher") + 8.5 * 0.01)
                });
            }

            return suggestions;
        }
    }
}
