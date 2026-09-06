// Developer: heaplyn
// Date: 2026-08-13
// Summary: Continuous Real-Time Screen Monitoring & Context Engine.
// Captures background screen snapshots, monitors active window changes, and provides AI Vision context.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public static class ScreenMonitorEngine
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private static readonly string ScreenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Screenshots");
        private static readonly string LatestScreenshotPath = Path.Combine(ScreenshotDir, "LatestScreen.png");
        private static System.Threading.Timer? _monitorTimer;
        private static readonly object _lock = new();

        public static bool IsMonitoring { get; private set; } = false;
        public static int IntervalSeconds { get; set; } = 5;
        public static string ActiveWindowTitle { get; private set; } = string.Empty;
        public static string ActiveProcessName { get; private set; } = string.Empty;
        public static DateTime LastCaptureTime { get; private set; } = DateTime.MinValue;
        public static string LastAiSummary { get; private set; } = string.Empty;

        public static event Action<string, string>? OnScreenCaptured;

        static ScreenMonitorEngine()
        {
            Directory.CreateDirectory(ScreenshotDir);
        }

        public static void Start(int intervalSec = 5)
        {
            lock (_lock)
            {
                IntervalSeconds = Math.Max(1, intervalSec);
                IsMonitoring = true;
                _monitorTimer?.Dispose();
                _monitorTimer = new System.Threading.Timer(OnMonitorTick, null, 0, IntervalSeconds * 1000);
                DebugConsoleOverlay.Log("Screen Monitor", $"Continuous Screen Monitoring STARTED ({IntervalSeconds}s interval)");
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                IsMonitoring = false;
                _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _monitorTimer?.Dispose();
                _monitorTimer = null;
                DebugConsoleOverlay.Log("Screen Monitor", "Continuous Screen Monitoring STOPPED");
            }
        }

        public static void Toggle()
        {
            if (IsMonitoring) Stop();
            else Start(IntervalSeconds);
        }

        private static int _aiAnalysisCounter = 0;
        private static void OnMonitorTick(object? state)
        {
            try
            {
                string capturePath = CapturePrimaryScreen();
                UpdateActiveWindowInfo();

                if (!string.IsNullOrEmpty(capturePath))
                {
                    OnScreenCaptured?.Invoke(capturePath, ActiveWindowTitle);
                }

                // Periodically perform deep AI analysis (every 3 mins)
                _aiAnalysisCounter++;
                if (_aiAnalysisCounter >= 36) // 36 * 5s = 180s = 3m
                {
                    _aiAnalysisCounter = 0;
                    Task.Run(async () => {
                        string summary = await AnalyzeScreenWithAiAsync();
                        ChronoLogManager.LogEvent("Vision", $"Screen Analysis: {summary}");
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Screen Monitor Tick Error: {ex.Message}");
            }
        }

        public static string CapturePrimaryScreen()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(ScreenshotDir);

                    // Use VirtualScreen to capture ALL monitors if present, or at least full resolution
                    var virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;
                    int screenWidth = virtualScreen.Width;
                    int screenHeight = virtualScreen.Height;
                    int left = virtualScreen.Left;
                    int top = virtualScreen.Top;

                    using var bmp = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(left, top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                    }

                    bmp.Save(LatestScreenshotPath, ImageFormat.Png);
                    LastCaptureTime = DateTime.Now;
                    return LatestScreenshotPath;
                }
                catch (Exception ex)
                {
                    DebugConsoleOverlay.Log("Screen Capture Error", ex.Message);
                    return string.Empty;
                }
            }
        }

        public static void UpdateActiveWindowInfo()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    var sb = new StringBuilder(256);
                    if (GetWindowText(hwnd, sb, 256) > 0)
                    {
                        ActiveWindowTitle = sb.ToString();
                    }

                    GetWindowThreadProcessId(hwnd, out uint pid);
                    if (pid > 0)
                    {
                        var proc = Process.GetProcessById((int)pid);
                        ActiveProcessName = proc.ProcessName;
                    }
                }
            }
            catch { }
        }

        public static async Task<string> AnalyzeScreenWithAiAsync(string customPrompt = "")
        {
            string screenPath = CapturePrimaryScreen();
            if (string.IsNullOrEmpty(screenPath) || !File.Exists(screenPath))
            {
                return "⚠️ Failed to capture screen snapshot.";
            }

            UpdateActiveWindowInfo();

            string prompt = string.IsNullOrWhiteSpace(customPrompt)
                ? $"Analyze this computer screen. The active window is '{ActiveWindowTitle}' ({ActiveProcessName}). Describe what key applications, text, code, or visual components are open and highlight any notable status or actions."
                : $"Screen Analysis Request: \"{customPrompt}\". Active Window: '{ActiveWindowTitle}' ({ActiveProcessName}).";

            DebugConsoleOverlay.Log("AI Vision Analysis", $"Analyzing screen image ({new FileInfo(screenPath).Length / 1024} KB)...");

            try
            {
                byte[] imageBytes = File.ReadAllBytes(screenPath);
                string base64 = Convert.ToBase64String(imageBytes);

                // Query Gemini 1.5 Flash Vision Model with image base64
                string aiResponse = await AiAPI.AnalyzeImageBase64Async(prompt, base64, "image/png");
                LastAiSummary = aiResponse;
                return aiResponse;
            }
            catch (Exception ex)
            {
                return $"⚠️ AI Screen Vision Error: {ex.Message}";
            }
        }
    }
}
