using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class PowerShellTool : IAiTool
    {
        public string Tag => "PS";
        public string RegexPattern => @"@ps\{(?<c>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string cmd = m.Groups["c"].Value.Trim();
            if (!executedTags.Add("PS:" + cmd.GetHashCode())) return Task.FromResult("");
            // Agent Mode gate: only runs when the user has enabled PC control.
            if (!CoreRegistry.Data.Settings.Current.ENABLE_PC_CONTROL)
                return Task.FromResult("[BLOCKED: enable Agent Mode (PC control) in Settings to let Heaplit run commands]\n");
            // Confirm clearly-destructive commands before running.
            string lc = cmd.ToLowerInvariant();
            bool risky = lc.Contains("remove-item") || lc.Contains("del ") || lc.Contains("format ") ||
                         lc.Contains("shutdown") || lc.Contains("stop-process") || lc.Contains("rmdir") ||
                         lc.Contains("rd /s") || lc.Contains("rm -");
            if (risky && !HumanConfirm.Ask($"Heaplit (AI) wants to run a shell command:\n\n{cmd}\n\nAllow?"))
                return Task.FromResult("[DENIED: user declined the command]\n");
            string output = AgentExecutor.ExecutePowerShellDirect(cmd);
            return Task.FromResult($"[PS OUTPUT]:\n{output}\n");
        }
    }

    public class ProcessListTool : IAiTool
    {
        public string Tag => "PL";
        public string RegexPattern => @"@proc_list";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            if (!executedTags.Add("PL")) return Task.FromResult("");
            var procs = Process.GetProcesses().Select(p => p.ProcessName).Distinct().OrderBy(n => n).Take(100);
            return Task.FromResult($"[PROCESSES]:\n{string.Join(", ", procs)}\n");
        }
    }

    public class ProcessKillTool : IAiTool
    {
        public string Tag => "PK";
        public string RegexPattern => @"@proc_kill\{(?<n>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string name = m.Groups["n"].Value.Trim().Trim('"', '\'');
            if (!executedTags.Add("PK:" + name)) return Task.FromResult("");
            // SECURITY: model-initiated process termination requires explicit human confirmation.
            if (!HumanConfirm.Ask($"Heaplit (AI) wants to force-kill all '{name}' processes. Allow?"))
                return Task.FromResult($"[DENIED: user declined to kill {name}]\n");
            int killed = 0;
            foreach (var p in Process.GetProcessesByName(name)) { try { p.Kill(); killed++; } catch { } }
            return Task.FromResult($"[KILLED: {name} ({killed} instances)]\n");
        }
    }
}
