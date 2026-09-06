using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Management;

namespace HeaplitLauncher.AiTools
{
    public class HardwareMetricsTool : IAiTool
    {
        public string Tag => "HW";
        public string RegexPattern => @"@hw_info";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            if (!executedTags.Add("HW")) return Task.FromResult("");
            try {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                var cpu = searcher.Get().Cast<ManagementBaseObject>().First();
                string load = cpu["LoadPercentage"]?.ToString() ?? "??";

                var mem = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem").Get().Cast<ManagementBaseObject>().First();
                long free = long.Parse(mem["FreePhysicalMemory"].ToString()!);
                long total = long.Parse(mem["TotalVisibleMemorySize"].ToString()!);

                return Task.FromResult($"[CPU LOAD: {load}%] [RAM FREE: {free/1024}MB / {total/1024}MB]\n");
            } catch { return Task.FromResult("[ERROR: WMI metrics failed]\n"); }
        }
    }

    public class VolumeTool : IAiTool
    {
        public string Tag => "VOL";
        public string RegexPattern => @"@vol\{(?<v>\d+)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            int vol = int.Parse(m.Groups["v"].Value);
            CommandParser.ExecuteFirstSuggestion($"volume {vol}");
            return Task.FromResult($"[VOLUME SET: {vol}%]\n");
        }
    }

    public class NetDiagTool : IAiTool
    {
        public string Tag => "NET";
        public string RegexPattern => @"@net_diag";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            if (!executedTags.Add("NET")) return Task.FromResult("");
            string output = AgentExecutor.ExecutePowerShellDirect("Test-NetConnection -ComputerName google.com -InformationLevel Quiet; Get-NetIPAddress -AddressFamily IPv4 | Select-Object -ExpandProperty IPAddress");
            return Task.FromResult($"[NET DIAG]:\n{output}\n");
        }
    }
}
