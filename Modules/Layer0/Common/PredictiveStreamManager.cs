// Developer: heaplyn
// Date: 2026-08-15
// Summary: Predictive Data Stream & Environment Snapshot Manager.
//          Maintains a "Continuous Data Stream" of system events and a high-level "Info Pass" for quick retrieval.
//          Uses lightweight LLM cycles to predict user intent and proactive actions based on background activity.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class SystemEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty; // e.g., "VOICE", "WINDOW", "CLIPBOARD"
        public string Data { get; set; } = string.Empty;
    }

    public static class PredictiveStreamManager
    {
        private static bool IsRunning = false;
        private static readonly List<SystemEvent> _streamBuffer = new List<SystemEvent>();
        private static string _cachedInfoPass = "System Ready.";
        private static string _currentPrediction = "Idle";
        private static readonly object _lock = new object();

        public static void Start()
        {
            if (IsRunning) return;
            IsRunning = true;

            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    try
                    {
                        await ProcessStreamCycleAsync();
                    }
                    catch (Exception ex)
                    {
                        DebugConsoleOverlay.Log("Predictive-Error", ex.Message);
                    }

                    // Process cycle every 60 seconds to avoid saturating the LLM backend
                    await AdaptiveSleeper.DelayAsync(60000);
                }
            });

            DebugConsoleOverlay.Log("Predictive-System", "Continuous Data Stream active (60s cycle).");
        }

        public static void IngestEvent(string source, string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return;
            lock (_lock)
            {
                _streamBuffer.Add(new SystemEvent { Source = source, Data = data });
                // Keep only last 50 events for context window
                if (_streamBuffer.Count > 50) _streamBuffer.RemoveAt(0);
            }
        }

        public static string GetInfoPass()
        {
            lock (_lock) return _cachedInfoPass;
        }

        public static string GetCurrentPrediction()
        {
            lock (_lock) return _currentPrediction;
        }

        private static async Task ProcessStreamCycleAsync()
        {
            List<SystemEvent> events;
            lock (_lock) events = _streamBuffer.ToList();

            if (events.Count == 0) return;

            // Build a "Continuous Stream" summary for the LLM
            var sb = new StringBuilder();
            sb.AppendLine("## CONTINUOUS BACKGROUND DATA STREAM");
            foreach (var ev in events.TakeLast(15))
            {
                sb.AppendLine($"[{ev.Timestamp:HH:mm:ss}] {ev.Source}: {ev.Data}");
            }

            string activeWindow = ScreenMonitorEngine.ActiveWindowTitle;
            sb.AppendLine($"Foreground: {activeWindow}");

            // The "Predictive" LLM pass
            string prompt = "You are the Heaplit Predictive Core. Analyze this background data stream and foreground state.\n" +
                            "1. Generate a 2-sentence 'INFO PASS' (A quick summary of what the user is currently doing).\n" +
                            "2. Generate a 'PREDICTION' (What is the user likely to do next or need help with?).\n\n" +
                            "DATA STREAM:\n" + sb.ToString() + "\n\n" +
                            "Format your response EXACTLY as:\nINFO_PASS: <summary>\nPREDICTION: <prediction>";

            try
            {
                // Use the fastest model for predictions to avoid lag
                string response = await LlmRouter.AskAsync(prompt, null);

                var lines = response.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("INFO_PASS:", StringComparison.OrdinalIgnoreCase))
                    {
                        lock (_lock) _cachedInfoPass = line.Substring(10).Trim();
                    }
                    else if (line.StartsWith("PREDICTION:", StringComparison.OrdinalIgnoreCase))
                    {
                        lock (_lock) _currentPrediction = line.Substring(11).Trim();
                    }
                }

                DebugConsoleOverlay.Log("Predictive-Update", $"Pass: {_cachedInfoPass} | Prediction: {_currentPrediction}");
            }
            catch { }
        }
    }
}
