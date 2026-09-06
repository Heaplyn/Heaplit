// Developer: heaplyn
// Date: 2026-08-14
// Summary: AI-Powered, Language-Agnostic Code Teacher.
//          Queries LLM router to dynamically analyze and explain bugs, deprecated features,
//          or anti-patterns in any programming language. Persists code context into WorkspaceMemory.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class CodeTeacherManager
    {
        // Dedup: suppress identical (or near-identical) reports submitted within this window.
        private static string _lastReportSignature = string.Empty;
        private static DateTime _lastReportTime = DateTime.MinValue;
        private static readonly TimeSpan ReportCooldown = TimeSpan.FromMinutes(10);

        private static string ReportSignature(string text)
        {
            var words = new string(text.ToLowerInvariant()
                    .Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Take(18));
        }

        private static bool IsDuplicate(string report)
        {
            string sig = ReportSignature(report);
            if (sig == _lastReportSignature && DateTime.Now - _lastReportTime < ReportCooldown)
                return true;
            _lastReportSignature = sig;
            _lastReportTime = DateTime.Now;
            return false;
        }
        /// <summary>
        /// Reads a file from disk and queries AI to educational-check its contents.
        /// </summary>
        public static async Task<string> ScanFileAsync(string filePath)
        {
            if (!SettingsManager.Current.IS_TEACHER_MODE_ENABLED)
            {
                return "Teacher Mode is currently disabled in Settings.";
            }

            if (!File.Exists(filePath))
            {
                return $"Error: File '{filePath}' not found.";
            }

            try
            {
                string code = File.ReadAllText(filePath);
                string filename = Path.GetFileName(filePath);
                string extension = Path.GetExtension(filePath).ToLower();
                
                string language = extension switch
                {
                    ".cs" => "C#",
                    ".lua" => "Luau",
                    ".py" => "Python",
                    ".js" => "JavaScript",
                    ".ts" => "TypeScript",
                    ".html" => "HTML",
                    ".css" => "CSS",
                    ".cpp" => "C++",
                    ".c" => "C",
                    _ => "Generic/Unknown"
                };

                // Perform AI analysis
                string report = await ScanCodeAsync(code, filename, language);

                if (report.Trim().ToUpper() == "CLEAR")
                {
                    string cleanMsg = $"✅ Scan Complete: File '{filename}' looks clean! No issues found by AI Code Teacher.";
                    TextOverlay.Show(cleanMsg, 3000);
                    return cleanMsg;
                }

                // Update active workspace memory with this code context
                WorkspaceMemoryManager.UpdateActiveCode(filePath, code, language);

                // Suppress duplicate / near-identical reports within the cooldown window.
                if (IsDuplicate(report))
                    return "duplicate_suppressed";

                // Show notification and return
                TextOverlay.Show($"🎓 Code Teacher found issues in {filename}!", 4000);
                ChatOverlay.LogConsoleAction("AI Code Teacher Scan", $"File: {filename}\nReport generated.");

                return report;
            }
            catch (Exception ex)
            {
                return $"Error scanning file: {ex.Message}";
            }
        }

        /// <summary>
        /// Queries the central LLM dispatcher to perform educational code analysis.
        /// </summary>
        public static async Task<string> ScanCodeAsync(string codeContent, string filename, string language)
        {
            if (string.IsNullOrWhiteSpace(codeContent)) return "CLEAR";

            try
            {
                string prompt = $"You are an expert programming teacher. Analyze the following {language} code snippet ({filename}) " +
                                $"for any bugs, syntax errors, deprecation, security flaws, performance bottlenecks, or architectural anti-patterns. " +
                                $"Be completely language agnostic.\n\n" +
                                $"CRITICAL RULES:\n" +
                                $"1. If the code is correct, efficient, and has no clear issues, respond with ONLY the word 'CLEAR'.\n" +
                                $"2. If there are issues, do NOT return 'CLEAR'. Instead, write a brief, high-impact educational tutorial/explanation showing the issue, explaining the 'better method', and showing how to rewrite it.\n\n" +
                                $"Here is the code:\n" +
                                $"```\n{codeContent}\n```";

                // Query the LLM router
                string response = await LlmRouter.AskAsync(prompt, null);
                return response;
            }
            catch (Exception ex)
            {
                return $"Error during AI analysis: {ex.Message}";
            }
        }
    }
}
