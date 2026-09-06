// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to check system statistics, hardware specs (CPU, GPU, RAM, OS), and displays them inside the terminal overlay.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HeaplitLauncher
{
    public class SysInfoCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "sysinfo" || query == "specs" || query == "systeminfo" || query == "system specs";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = Math.Max(
                SearchUtil.GetSimilarity(query, "specs"), 
                SearchUtil.GetSimilarity(query, "sysinfo")
            );

            suggestions.Add(new CommandResult
            {
                TITLE       = "System Specifications",
                DESCRIPTION = "Display detailed OS, CPU, GPU, and RAM specifications",
                SIMILARITY  = similarity + 0.5,
                EXECUTE     = () => ShowSpecs()
            });

            return suggestions;
        }

        private static void ShowSpecs()
        {
            var sb = new StringBuilder();
            sb.AppendLine("===================================================");
            sb.AppendLine("              SYSTEM SPECIFICATIONS REPORT         ");
            sb.AppendLine("===================================================");
            sb.AppendLine();

            sb.AppendLine($"OS Version:       {Environment.OSVersion}");
            sb.AppendLine($"Architecture:     {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
            sb.AppendLine($"Machine Name:     {Environment.MachineName}");
            sb.AppendLine($"User Domain:      {Environment.UserDomainName}");
            sb.AppendLine($"System Directory: {Environment.SystemDirectory}");
            sb.AppendLine();

            sb.AppendLine("--- HARDWARE DETAILED ---");
            sb.AppendLine($"Processor count:  {Environment.ProcessorCount} Cores");
            sb.AppendLine($"CPU Model:        {GetCpuName()}");
            sb.AppendLine($"GPU Model:        {GetGpuName()}");
            sb.AppendLine($"Physical RAM:     {GetRamInfo()}");
            sb.AppendLine();

            CliOutputOverlay.Show("System Specifications", sb.ToString());
        }

        private static string GetCpuName()
        {
            try
            {
                object? val = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "");
                return val?.ToString()?.Trim() ?? "Unknown CPU";
            }
            catch { return "Unknown CPU"; }
        }

        private static string GetGpuName()
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    string path = $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}";
                    object? val = Microsoft.Win32.Registry.GetValue(path, "DriverDesc", null);
                    if (val != null)
                    {
                        return val.ToString() ?? "Unknown GPU";
                    }
                }
            }
            catch { }
            return "Unknown GPU";
        }

        private static string GetRamInfo()
        {
            var memStatus = new NativeMethods.MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
            if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
            {
                double totalGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                return $"{totalGb:F1} GB Total ({availGb:F1} GB Available)";
            }
            return "Unknown RAM";
        }
    }
}
