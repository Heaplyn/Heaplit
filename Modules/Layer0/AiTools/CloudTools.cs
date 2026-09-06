using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class GcsUploadTool : IAiTool
    {
        public string Tag => "GUP";
        public string RegexPattern => @"@gcs_up\{(?<p>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string p = m.Groups["p"].Value.Trim().Trim('"', '\'');
            if (!executedTags.Add("GCSUP:" + p)) return "";
            bool ok = await GoogleCloudManager.UploadToBucketAsync(p);
            return ok ? $"[GCS SUCCESS: Uploaded {p}]\n" : "[GCS FAIL: Upload failed]\n";
        }
    }

    public class CloudAssistTool : IAiTool
    {
        public string Tag => "ASSIST";
        public string RegexPattern => @"@assist\{(?<q>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string q = m.Groups["q"].Value;
            if (!executedTags.Add("ASSIST:" + q.GetHashCode())) return "";
            string res = await GoogleCloudManager.AskCloudAssistAsync(q);
            return $"[CLOUD ASSIST RESPONSE]:\n{res}\n";
        }
    }
}
