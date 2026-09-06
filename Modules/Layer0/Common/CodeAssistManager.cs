// Developer: heaplyn
// Date: 2026-08-13
// Summary: Continuous Real-Time Code Assist & Vision Advisor Engine.
// Periodically captures screen layouts, reads active project files, and queries Gemini Vision AI for refactoring/layout assistance.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class CodeAssistManager
    {
        private static System.Threading.Timer? _assistTimer;
        private static readonly object _lock = new object();
        private static bool _isRunning = false;

        public static bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnStateChanged?.Invoke(value);
            }
        }

        public static string ActiveCodebasePath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public static string CurrentCodeAdvice { get; private set; } = "Code Assist is idle. Say 'turn on code assist' to start.";
        public static string LastAnalyzedFiles { get; private set; } = string.Empty;

        public static event Action<bool>? OnStateChanged;
        public static event Action<string>? OnAdviceUpdated;

        static CodeAssistManager()
        {
            // Auto-detect project folder
            string checkDir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                if (Directory.Exists(Path.Combine(checkDir, "Modules")) || File.Exists(Path.Combine(checkDir, "HeaplitLauncher.csproj")))
                {
                    ActiveCodebasePath = checkDir;
                    break;
                }
                var parent = Directory.GetParent(checkDir);
                if (parent == null) break;
                checkDir = parent.FullName;
            }
        }

        public static void Start()
        {
            lock (_lock)
            {
                if (IsRunning) return;
                IsRunning = true;
                _assistTimer = new System.Threading.Timer(async _ => await CodeAssistTickAsync(), null, 0, 8000);
                DebugConsoleOverlay.Log("Code Assist", "Code Assist Engine STARTED (8s sampling loop)");
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                if (!IsRunning) return;
                IsRunning = false;
                _assistTimer?.Dispose();
                _assistTimer = null;
                DebugConsoleOverlay.Log("Code Assist", "Code Assist Engine STOPPED");
            }
        }

        public static void Toggle()
        {
            if (IsRunning) Stop();
            else Start();
        }

        private static async Task CodeAssistTickAsync()
        {
            try
            {
                // 1. Capture current screen
                string screenshotPath = ScreenMonitorEngine.CapturePrimaryScreen();
                if (string.IsNullOrEmpty(screenshotPath) || !File.Exists(screenshotPath)) return;

                ScreenMonitorEngine.UpdateActiveWindowInfo();
                string activeWinTitle = ScreenMonitorEngine.ActiveWindowTitle;

                // 2. Fetch and read active / recently modified code files (limit to top 3 files)
                var sourceFilesContent = GetRecentSourceFilesContext();

                // 3. Assemble prompt
                var prompt = new StringBuilder();
                prompt.AppendLine("You are an expert real-time code assistant looking at the user's screen and code files.");
                prompt.AppendLine($"Active window on user's desktop: '{activeWinTitle}'");
                prompt.AppendLine("Below are the contents of the relevant open project files:");
                prompt.AppendLine("--------------------------------------------------");
                prompt.AppendLine(sourceFilesContent);
                prompt.AppendLine("--------------------------------------------------");
                prompt.AppendLine("Analyze the screen layout (UI alignments, formatting, console compilation errors) and code files.");
                prompt.AppendLine("Provide brief, clear, direct bullets on what the user should edit, refactor, or fix next. Keep recommendations under 4 bullets.");

                byte[] imageBytes = File.ReadAllBytes(screenshotPath);
                string base64Image = Convert.ToBase64String(imageBytes);

                // 4. Query Gemini Vision
                string advice = await AiAPI.AnalyzeImageBase64Async(prompt.ToString(), base64Image);
                CurrentCodeAdvice = advice;
                OnAdviceUpdated?.Invoke(advice);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Code Assist Tick Error: {ex.Message}");
            }
        }

        private static string GetRecentSourceFilesContext()
        {
            if (!Directory.Exists(ActiveCodebasePath)) return "No active codebase path found.";

            try
            {
                var files = Directory.GetFiles(ActiveCodebasePath, "*.cs", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(ActiveCodebasePath, "*.json", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(ActiveCodebasePath, "*.xaml", SearchOption.AllDirectories))
                    .Select(f => new FileInfo(f))
                    .Where(fi => !fi.FullName.Contains("bin") && !fi.FullName.Contains("obj") && !fi.FullName.Contains(".vs") && !fi.FullName.Contains(".git"))
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .Take(3)
                    .ToList();

                if (files.Count == 0) return "No source files found in active workspace.";

                var sb = new StringBuilder();
                var fileNamesList = new List<string>();

                foreach (var file in files)
                {
                    fileNamesList.Add(file.Name);
                    sb.AppendLine($"File: {file.FullName}");
                    sb.AppendLine("```csharp");
                    string content = File.ReadAllText(file.FullName);
                    // Take last 120 lines of the file to save token budget
                    var lines = content.Split('\n');
                    if (lines.Length > 120)
                    {
                        sb.AppendLine("// ... (truncated starting lines) ...");
                        sb.AppendLine(string.Join("\n", lines.TakeLast(120)));
                    }
                    else
                    {
                        sb.AppendLine(content);
                    }
                    sb.AppendLine("```\n");
                }

                LastAnalyzedFiles = string.Join(", ", fileNamesList);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error reading source files: {ex.Message}";
            }
        }
    }
}
