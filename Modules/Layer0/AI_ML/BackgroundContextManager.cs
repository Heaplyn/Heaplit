// Developer: heaplyn
// Date: 2026-08-14
// Summary: Background Context Manager and Prefetch Optimizer.
//          Periodically gathers environment metrics, active files, and screen context
//          and pre-analyzes them using AI to maintain a compact, pre-fetched context summary.
//          This drastically reduces final LLM prompt tokens and speeds up user query responses.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public static class BackgroundContextManager
    {
        private static bool IsRunning = false;
        private static string CachedContextSummary = "User is coding in their workspace.";
        private static DateTime LastUpdateTime = DateTime.MinValue;
        private static readonly object Lock = new object();

        public static void Start()
        {
            if (IsRunning) return;
            IsRunning = true;

            Task.Run(async () =>
            {
                // Give the system some time to fully initialize on boot
                await Task.Delay(10000);

                while (IsRunning)
                {
                    try
                    {
                        if (SettingsManager.Current.IS_HEAPLIT_ENABLED)
                        {
                            await RefreshContextSnapshotAsync();
                        }
                    }
                    catch (Exception Ex)
                    {
                        DebugConsoleOverlay.Log("Prefetch Error", Ex.Message);
                    }

                    // Run prefetch analysis every 45 seconds
                    await AdaptiveSleeper.DelayAsync(45000);
                }
            });

            DebugConsoleOverlay.Log("ContextPrefetch", "Background context prefetch manager active.");
        }

        public static void Stop()
        {
            IsRunning = false;
        }

        public static string GetActiveContextSummary()
        {
            lock (Lock)
            {
                // If summary is older than 5 minutes, return fallback to prevent stale data usage
                if ((DateTime.Now - LastUpdateTime).TotalMinutes > 5.0)
                {
                    return string.Empty;
                }
                return CachedContextSummary;
            }
        }

        private static async Task RefreshContextSnapshotAsync()
        {
            // Gather telemetry components
            string ActiveWin = CoreRegistry.Memory.GetCurrentWindowTitle();
            string ClipboardText = string.Empty;
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Clipboard.ContainsText()) ClipboardText = Clipboard.GetText();
                });
            }
            catch { }

            var WsMemory = WorkspaceMemoryManager.GetCurrent();
            string ActiveFile = WsMemory.ActiveFileName;
            string ActiveLang = WsMemory.ActiveProgrammingLanguage;
            string CodeSnippet = WsMemory.ActiveCodeSnippet;

            // Combine telemetry data into a structured prompt
            string TelemetryData = $"[TELEMETRY SNAPSHOT]\n" +
                                   $"Focused Window: {ActiveWin}\n" +
                                   $"Workspace Active File: {ActiveFile} ({ActiveLang})\n" +
                                   $"Recent Code snippet:\n{CodeSnippet}\n" +
                                   $"Clipboard text:\n{ClipboardText}\n";

            string PrefetchPrompt = $"You are a telemetry pre-analyzer. Summarize this user environment snapshot in 2-3 concise sentences. " +
                                    $"Identify the active programming language, active files, visible developer topics, and user focus. " +
                                    $"Be extremely compact. Here is the telemetry:\n\n{TelemetryData}";

            // Query LLM in background (use the fast route)
            try
            {
                string Summary = await CoreRegistry.Llm.AskAsync(PrefetchPrompt, null);
                if (!string.IsNullOrWhiteSpace(Summary) && !Summary.StartsWith("⚠️"))
                {
                    lock (Lock)
                    {
                        CachedContextSummary = Summary.Trim();
                        LastUpdateTime = DateTime.Now;
                    }
                    DebugConsoleOverlay.Log("ContextPrefetch", $"Pre-analyzed context updated (Length: {CachedContextSummary.Length} chars).");
                }
            }
            catch (Exception Ex)
            {
                DebugConsoleOverlay.Log("ContextPrefetch Note", $"Prefetch pass skipped: {Ex.Message}");
            }
        }
    }
}
