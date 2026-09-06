using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class CodeModificationTool : IAiTool
    {
        public string Tag => "MOD";
        public string RegexPattern => @"@mod_code\{(?<p>.*?)\}\{(?<s>.*?)\}\{(?<r>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value;
            string s = m.Groups["s"].Value;
            string r = m.Groups["r"].Value;

            if (!executedTags.Add("MOD:" + p + s.GetHashCode())) return "";

            var res = await SelfMutationEngine.ModifyCodeAsync(p, s, r);
            return res.Success ? $"[MUTATION SUCCESS: {p}]\n" : $"[MUTATION FAIL: {res.Message}]\n";
        }
    }

    public class SystemBackupTool : IAiTool
    {
        public string Tag => "BACKUP";
        public string RegexPattern => @"@backup\{(?<r>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string reason = m.Groups["r"].Value;
            if (!executedTags.Add("BACKUP:" + reason)) return "";
            string path = await SelfBackupManager.CreateBackupAsync(reason);
            return $"[BACKUP CREATED: {path}]\n";
        }
    }
}
