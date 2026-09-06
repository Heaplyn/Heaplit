// Developer: heaplyn
// Date: 2026-08-19
// Summary: Autonomous Tool Synthesis Engine.
//          Allows the AI to design, verify, and register its own tools at runtime.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class DynamicScriptTool : IAiTool
    {
        public string Tag { get; }
        public string RegexPattern { get; }
        public string PowerShellScriptTemplate { get; }
        public bool IsVerified { get; set; } = false;

        public DynamicScriptTool(string tag, string pattern, string script)
        {
            Tag = tag;
            RegexPattern = pattern;
            PowerShellScriptTemplate = script;
        }

        public Task<string> ExecuteAsync(Match match, HashSet<string> executedTags)
        {
            if (!executedTags.Add(Tag + ":" + match.Value.GetHashCode())) return Task.FromResult("");
            if (!CoreRegistry.Data.Settings.Current.ENABLE_PC_CONTROL)
                return Task.FromResult("[BLOCKED: enable Agent Mode to run synthesized tools]\n");
            string script = PowerShellScriptTemplate;
            foreach (string g in match.Groups.Keys)
            {
                if (int.TryParse(g, out _)) continue;
                script = script.Replace($"${{{g}}}", match.Groups[g].Value);
            }
            string result = AgentExecutor.ExecutePowerShellDirect(script);
            return Task.FromResult($"[DYNAMIC TOOL {Tag}]:\n{result}\n");
        }
    }

    public static class SelfEvolvingToolEngine
    {
        // Agent Mode only. Each synthesized tool is confirmed by the user before it is registered,
        // because it runs arbitrary PowerShell. This is how the model builds a reusable capability
        // for a repeated complex task instead of redoing it each turn.
        public static Task<string> ProcessToolSynthesisAsync(string aiResponse)
        {
            if (!CoreRegistry.Data.Settings.Current.ENABLE_PC_CONTROL) return Task.FromResult(string.Empty);

            var rx = new Regex(@"@new_tool\{(?<tag>.*?)\}\{(?<regex>.*?)\}\{(?<script>.*?)\}", RegexOptions.Singleline);
            int created = 0;
            foreach (Match m in rx.Matches(aiResponse))
            {
                string tag = m.Groups["tag"].Value.Trim();
                string pattern = m.Groups["regex"].Value.Trim();
                string script = m.Groups["script"].Value.Trim();
                if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(pattern)) continue;

                if (!HumanConfirm.Ask($"Heaplit (AI) wants to CREATE a reusable tool '{tag}' that runs this script:\n\n{script}\n\nAllow?"))
                    continue;

                AiToolRegistry.Register(new DynamicScriptTool(tag, pattern, script) { IsVerified = true });
                created++;
                try { DebugConsoleOverlay.Log("Tool-Evolution", $"Synthesized & registered tool: {tag}"); } catch { }
            }
            return Task.FromResult(created > 0 ? $"[SYSTEM]: {created} new tool(s) created.\n" : "");
        }
    }
}
