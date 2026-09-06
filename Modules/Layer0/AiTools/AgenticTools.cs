// Developer: heaplyn
// Date: 2026-09-02
// Summary: Agentic tools the model can invoke: surgical file edits (path-jailed) and
//          self-configuration (changing Heaplit's own settings, with human confirmation).
//          Web search / fetch / download live in WebTools.cs.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    // @edit{path}{find}{replace} — replace the first occurrence of <find> with <replace> in a file.
    public class EditFileTool : IAiTool
    {
        public string Tag => "EDIT";
        public string RegexPattern => @"@edit\{(?<p>.*?)\}\{(?<f>.*?)\}\{(?<r>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            string find = m.Groups["f"].Value;
            string repl = m.Groups["r"].Value;
            if (!executedTags.Add("EDIT:" + p + find.GetHashCode())) return "";
            if (!AiPathJail.TryResolve(p, out string full, out string err)) return err;
            if (!File.Exists(full)) return $"[ERROR: file {p} not found]\n";
            string content = await File.ReadAllTextAsync(full);
            int idx = content.IndexOf(find, StringComparison.Ordinal);
            if (idx < 0) return $"[ERROR: text to replace not found in {p}]\n";
            content = content.Substring(0, idx) + repl + content.Substring(idx + find.Length);
            await File.WriteAllTextAsync(full, content);
            return $"[EDITED: {p}]\n";
        }
    }

    // @set{SETTING_NAME}{value} — Heaplit changes its own configuration (with human confirmation).
    public class SettingsControlTool : IAiTool
    {
        public string Tag => "SET";
        public string RegexPattern => @"@set\{(?<k>.*?)\}\{(?<v>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string key = m.Groups["k"].Value.Trim();
            string val = m.Groups["v"].Value.Trim();
            if (!executedTags.Add("SET:" + key)) return Task.FromResult("");

            var prop = typeof(SystemSettings).GetProperty(key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null || !prop.CanWrite) return Task.FromResult($"[ERROR: no writable setting '{key}']\n");

            if (!HumanConfirm.Ask($"Heaplit (AI) wants to change setting:\n\n{prop.Name} = {val}\n\nAllow?"))
                return Task.FromResult($"[DENIED: user declined to change {prop.Name}]\n");

            try
            {
                object converted = prop.PropertyType == typeof(bool)
                    ? (val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1")
                    : Convert.ChangeType(val, prop.PropertyType);
                prop.SetValue(SettingsManager.Current, converted);
                SettingsManager.Save();
                return Task.FromResult($"[SETTING CHANGED: {prop.Name} = {val}]\n");
            }
            catch (Exception ex) { return Task.FromResult($"[ERROR setting {prop.Name}: {ex.Message}]\n"); }
        }
    }
}
