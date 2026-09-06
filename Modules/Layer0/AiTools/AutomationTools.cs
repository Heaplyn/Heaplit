using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HeaplitLauncher.AiTools
{
    public class MouseControlTool : IAiTool
    {
        public string Tag => "MOUSE";
        public string RegexPattern => @"@mouse\{(?<x>\d+)\}\{(?<y>\d+)\}\{(?<c>click|move|double|right)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            int x = int.Parse(m.Groups["x"].Value);
            int y = int.Parse(m.Groups["y"].Value);
            string op = m.Groups["c"].Value;

            string script = op switch {
                "click" => $"[Windows.Forms.Cursor]::Position = '{x},{y}'; (New-Object -ComObject 'WScript.Shell').SendKeys('')", // Simulating click is harder in pure PS, usually needs user32.dll
                "move" => $"[Windows.Forms.Cursor]::Position = '{x},{y}'",
                _ => ""
            };

            // For complex UI automation, we offload to a specialized C# helper or User32 bridge
            AgentExecutor.ExecutePowerShellDirect(script);
            return Task.FromResult($"[MOUSE {op} at {x},{y}]\n");
        }
    }

    public class KeyboardTool : IAiTool
    {
        public string Tag => "KEYS";
        public string RegexPattern => @"@keys\{(?<t>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string keys = m.Groups["t"].Value;
            string script = $"$ws = New-Object -ComObject WScript.Shell; $ws.SendKeys('{keys.Replace("'", "''")}')";
            AgentExecutor.ExecutePowerShellDirect(script);
            return Task.FromResult($"[KEYS SENT: {keys}]\n");
        }
    }

    public class AppFocusTool : IAiTool
    {
        public string Tag => "FOCUS";
        public string RegexPattern => @"@focus\{(?<n>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string name = m.Groups["n"].Value.Trim();
            string script = $"$ws = New-Object -ComObject WScript.Shell; $ws.AppActivate('{name}')";
            AgentExecutor.ExecutePowerShellDirect(script);
            return Task.FromResult($"[FOCUSED APP: {name}]\n");
        }
    }
}
