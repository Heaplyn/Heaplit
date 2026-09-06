// Developer: heaplyn
// Date: 2026-08-18
// Summary: Robust Adaptive Resource Manager for Heaplit AI.
//          Monitors CPU, Memory, and Latency to throttle Godellian processing.
//          Prevents system lockups by dynamically adjusting complexity.

using System;
using System.Diagnostics;

namespace HeaplitLauncher
{
    public static class NeuralResourceManager
    {
        private static readonly Process CurrentProc = Process.GetCurrentProcess();
        private static DateTime _lastCpuTime = DateTime.MinValue;
        private static TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
        private static double _currentCpuLoad = 0;

        public static int MaxAllowedClusters { get; private set; } = 24;
        public static int RecursionDepth { get; private set; } = 1;
        public static bool IsThrottled { get; private set; } = false;
        public static bool GlobalAiEnable { get; set; } = true;

        public static void MonitorResources()
        {
            try
            {
                if (_lastCpuTime == DateTime.MinValue)
                {
                    _lastCpuTime = DateTime.UtcNow;
                    _lastTotalProcessorTime = CurrentProc.TotalProcessorTime;
                    return;
                }

                var currentTime = DateTime.UtcNow;
                var currentProcessorTime = CurrentProc.TotalProcessorTime;
                double cpuUsedMs = (currentProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                double totalMs = (currentTime - _lastCpuTime).TotalMilliseconds;

                if (totalMs > 100)
                    _currentCpuLoad = (cpuUsedMs / (Environment.ProcessorCount * totalMs)) * 100;

                _lastCpuTime = currentTime;
                _lastTotalProcessorTime = currentProcessorTime;

                long memUsageMb = CurrentProc.PrivateMemorySize64 / 1024 / 1024;

                // AGGRESSIVE SCALING LOGIC
                if (_currentCpuLoad > 50 || memUsageMb > 1000)
                {
                    IsThrottled = true;
                    MaxAllowedClusters = 12;
                    RecursionDepth = 1;
                }
                else if (_currentCpuLoad > 20 || memUsageMb > 600)
                {
                    IsThrottled = true;
                    MaxAllowedClusters = 20;
                    RecursionDepth = 1;
                }
                else
                {
                    IsThrottled = false;
                    MaxAllowedClusters = 48;
                    RecursionDepth = 2;
                }
            }
            catch { }
        }

        public static string GetResourceReport() => $"[SYS] CPU: {_currentCpuLoad:F1}% | RAM: {CurrentProc.PrivateMemorySize64 / 1024 / 1024}MB | Depth: {RecursionDepth}";
    }
}
