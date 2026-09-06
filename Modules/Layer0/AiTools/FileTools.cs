using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class ReadFileTool : IAiTool
    {
        public string Tag => "RF";
        public string RegexPattern => @"@rf\{(?<p>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            if (!executedTags.Add("RF:" + p)) return "";
            if (!AiPathJail.TryResolve(p, out string full, out string err)) return err;
            if (File.Exists(full)) {
                string content = await File.ReadAllTextAsync(full);
                return $"[FILE: {p}]\n{(content.Length > 3000 ? content.Substring(0, 3000) + "... [Truncated]" : content)}\n[END]\n";
            }
            return $"[ERROR: File {p} not found]\n";
        }
    }

    public class WriteFileTool : IAiTool
    {
        public string Tag => "WF";
        public string RegexPattern => @"@wf\{(?<p>.*?)\}\{(?<c>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            string c = m.Groups["c"].Value;
            if (!executedTags.Add("WF:" + p + c.GetHashCode())) return "";
            if (!AiPathJail.TryResolve(p, out string full, out string err)) return err;
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? AiPathJail.Root);
            await File.WriteAllTextAsync(full, c);
            SemanticMemoryManager.AddTrackedFile(full);
            return $"[WRITTEN: {p}]\n";
        }
    }

    public class ListFilesTool : IAiTool
    {
        public string Tag => "LS";
        public string RegexPattern => @"@ls\{(?<p>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            if (!AiPathJail.TryResolve(p, out string full, out string err)) return Task.FromResult(err);
            if (Directory.Exists(full)) {
                var entries = Directory.GetFileSystemEntries(full).Select(Path.GetFileName).Take(50);
                return Task.FromResult($"[DIR {p}]:\n{string.Join("\n", entries)}\n");
            }
            return Task.FromResult($"[ERROR: Dir {p} not found]\n");
        }
    }

    public class ReadBinaryTool : IAiTool
    {
        public string Tag => "RF_B";
        public string RegexPattern => @"@rf_b\{(?<p>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            if (!executedTags.Add("RF_B:" + p)) return "";
            if (!AiPathJail.TryResolve(p, out string full, out string err)) return err;
            if (File.Exists(full)) {
                byte[] data = await File.ReadAllBytesAsync(full);
                string b64 = Convert.ToBase64String(data);
                return $"[BINARY FILE: {p}]\n[BASE64]: {b64}\n[END]\n";
            }
            return $"[ERROR: File {p} not found]\n";
        }
    }

    public class WriteBinaryTool : IAiTool
    {
        public string Tag => "WF_B";
        public string RegexPattern => @"@wf_b\{(?<p>.*?)\}\{(?<b>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            string b = m.Groups["b"].Value.Trim();
            if (!executedTags.Add("WF_B:" + p)) return "";
            if (!AiPathJail.TryResolve(p, out string full, out string err)) return err;
            try {
                byte[] data = Convert.FromBase64String(b);
                Directory.CreateDirectory(Path.GetDirectoryName(full) ?? AiPathJail.Root);
                await File.WriteAllBytesAsync(full, data);
                return $"[WRITTEN BINARY: {p}]\n";
            } catch (Exception ex) { return $"[ERROR WF_B]: {ex.Message}\n"; }
        }
    }
}
