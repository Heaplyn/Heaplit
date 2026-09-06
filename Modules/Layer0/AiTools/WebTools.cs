using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class WebSearchTool : IAiTool
    {
        public string Tag => "WS";
        public string RegexPattern => @"@web_search\{(?<q>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string q = m.Groups["q"].Value;
            if (!executedTags.Add("WS:" + q)) return "";
            string res = await CoreRegistry.System.Web.SearchWebAsync(q);
            return $"[WEB SEARCH RESULT]:\n{res}\n";
        }
    }

    public class WebFetchTool : IAiTool
    {
        public string Tag => "WFH";
        public string RegexPattern => @"@web_fetch\{(?<u>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string u = m.Groups["u"].Value;
            if (!executedTags.Add("WFH:" + u)) return "";
            string res = await CoreRegistry.System.Web.ScrapeWebpageAsync(u);
            return $"[WEB FETCH CONTENT]:\n{res}\n";
        }
    }

    public class DownloadTool : IAiTool
    {
        public string Tag => "DL";
        public string RegexPattern => @"@download\{(?<u>.*?)\}\{(?<d>.*?)\}";
        public async Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string u = m.Groups["u"].Value;
            string d = m.Groups["d"].Value;
            if (!executedTags.Add("DL:" + u)) return "";
            string res = await CoreRegistry.System.Web.DownloadFileAsync(u, d);
            return $"[DOWNLOAD STATUS]: {res}\n";
        }
    }
}
