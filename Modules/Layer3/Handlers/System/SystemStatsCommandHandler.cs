// Developer: heaplyn
// Date: 2026-08-09
// Summary: Periodically polls system performance stats (CPU, RAM) using native Win32 APIs, showing live metrics in suggestions.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace HeaplitLauncher
{
    public class SystemStatsCommandHandler : ICommandHandler
    {
        private static double _cpuUsage = 0;
        private static double _ramUsagePercentage = 0;
        private static string _ramDetails = "";
        private static bool _isPolling = false;

        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "cpu", "ram", "sys", "stats", "system");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();
            double similarity = SearchUtil.BestSimilarity(query, "cpu", "ram", "sys", "stats", "system"); // High priority matching for stats keywords

            if (query == "cpu")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"CPU Usage: {_cpuUsage:F1}%",
                    DESCRIPTION = "Live system processor utilization",
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }
            else if (query == "ram")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"RAM Usage: {_ramUsagePercentage:F1}%",
                    DESCRIPTION = _ramDetails,
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }
            else
            {
                // General "sys" or "stats" keyword
                suggestions.Add(new CommandResult
                {
                    TITLE = $"CPU: {_cpuUsage:F1}% | RAM: {_ramUsagePercentage:F1}%",
                    DESCRIPTION = $"Details: {_ramDetails}",
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }

            return suggestions;
        }

        public void OnStart()
        {
            if (_isPolling) return;
            _isPolling = true;

            // Start low-priority background thread to update stats every 1 second
            var thread = new Thread(PollSystemStats)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            thread.Start();
        }

        private static void PollSystemStats()
        {
            System.Runtime.InteropServices.ComTypes.FILETIME prevIdleTime, prevKernelTime, prevUserTime;
            if (!NativeMethods.GetSystemTimes(out prevIdleTime, out prevKernelTime, out prevUserTime))
            {
                _isPolling = false;
                return;
            }

            while (_isPolling)
            {
                AdaptiveSleeper.Sleep(1000);

                // 1. Calculate CPU Usage
                System.Runtime.InteropServices.ComTypes.FILETIME currIdleTime, currKernelTime, currUserTime;
                if (NativeMethods.GetSystemTimes(out currIdleTime, out currKernelTime, out currUserTime))
                {
                    ulong prevIdle = FileTimeToUInt64(prevIdleTime);
                    ulong prevKernel = FileTimeToUInt64(prevKernelTime);
                    ulong prevUser = FileTimeToUInt64(prevUserTime);

                    ulong currIdle = FileTimeToUInt64(currIdleTime);
                    ulong currKernel = FileTimeToUInt64(currKernelTime);
                    ulong currUser = FileTimeToUInt64(currUserTime);

                    ulong idleDiff = currIdle - prevIdle;
                    ulong kernelDiff = currKernel - prevKernel;
                    ulong userDiff = currUser - prevUser;
                    ulong totalSystemDiff = kernelDiff + userDiff;

                    if (totalSystemDiff > 0)
                    {
                        ulong totalUsedDiff = totalSystemDiff - idleDiff;
                        _cpuUsage = (double)(totalUsedDiff * 100) / totalSystemDiff;
                    }

                    prevIdleTime = currIdleTime;
                    prevKernelTime = currKernelTime;
                    prevUserTime = currUserTime;
                }

                // 2. Calculate RAM Usage
                var memStatus = new NativeMethods.MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
                if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
                {
                    double totalGB = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    double availGB = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    double usedGB = totalGB - availGB;

                    _ramUsagePercentage = memStatus.dwMemoryLoad; // Percentage directly loaded
                    _ramDetails = $"{usedGB:F2} GB used of {totalGB:F2} GB total";
                }
            }
        }

        private static ulong FileTimeToUInt64(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            return ((ulong)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        }
    }
}
