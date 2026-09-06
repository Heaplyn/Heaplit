using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class NoteTool : IAiTool
    {
        public string Tag => "NOTE";
        public string RegexPattern => @"@note\{(?<t>.*?)\}\{(?<c>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string t = m.Groups["t"].Value;
            string c = m.Groups["c"].Value;
            if (!executedTags.Add("NOTE:" + t + c.GetHashCode())) return "";
            await ContextNotesManager.SyncMemoryToNotesAsync(new MemoryNode {
                Content = c,
                Category = "Knowledge",
                SubCategory = t
            });
            return $"[NOTE PERSISTED: {t}]\n";
        }
    }

    public class AddTrackedFileTool : IAiTool
    {
        public string Tag => "TRACK";
        public string RegexPattern => @"@track\{(?<p>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            if (!executedTags.Add("TRACK:" + p)) return Task.FromResult("");
            SemanticMemoryManager.AddTrackedFile(p);
            return Task.FromResult($"[FILE TRACKED: {p}]\n");
        }
    }

    public class NukeMemoryTool : IAiTool
    {
        public string Tag => "NUKE";
        public string RegexPattern => @"@nuke_memory\{(?<c>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string category = m.Groups["c"].Value.Trim();
            if (!executedTags.Add("NUKE:" + category)) return Task.FromResult("");
            int removed = SemanticMemoryManager.NukeMemory(category);
            return Task.FromResult($"[MEMORY PURGE: {removed} nodes removed from {category}]\n");
        }
    }
}
