// Developer: heaplyn
// Date: 2026-09-01
// Summary: Injects live web context into AI chat prompts (read-only, safe). Two triggers:
//          (1) any URL the user pastes is scraped and its content added; (2) an explicit web
//          search ("/web X", "search the web for X", "search: X") runs and its results added.
//          Conservative on purpose — normal chats don't hit the network, so latency stays low.

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class WebContextInjector
    {
        private static readonly Regex UrlRx =
            new(@"https?://[^\s\)\]\}""']+", RegexOptions.Compiled);
        private static readonly Regex SearchRx =
            new(@"(?:^/web\s+|search the web for\s+|^search:\s*)(?<q>.+)", RegexOptions.IgnoreCase);

        private const int MaxChunk = 4000;

        /// <summary>Returns a web-context block to prepend, or "" if nothing to fetch.</summary>
        public static async Task<string> MaybeFetchAsync(string prompt, CancellationToken ct = default)
        {
            try
            {
                var sb = new StringBuilder();

                // 1) Scrape up to 2 URLs present in the prompt.
                var urls = UrlRx.Matches(prompt).Select(m => m.Value).Distinct().Take(2).ToList();
                foreach (var u in urls)
                {
                    try
                    {
                        var r = await WebScraperManager.ScrapePageAsync(u);
                        string report = WebScraperManager.FormatReport(r);
                        if (!string.IsNullOrWhiteSpace(report))
                        {
                            if (report.Length > MaxChunk) report = report.Substring(0, MaxChunk) + " …[truncated]";
                            sb.AppendLine($"[WEB PAGE: {u}]\n{report}\n");
                        }
                    }
                    catch { }
                }

                // 2) Explicit web search.
                var m = SearchRx.Match(prompt);
                if (m.Success)
                {
                    string term = m.Groups["q"].Value.Trim();
                    try
                    {
                        string res = await WebOperationManager.SearchWebAsync(term);
                        if (!string.IsNullOrWhiteSpace(res))
                        {
                            if (res.Length > MaxChunk) res = res.Substring(0, MaxChunk) + " …[truncated]";
                            sb.AppendLine($"[WEB SEARCH: {term}]\n{res}\n");
                        }
                    }
                    catch { }
                }

                if (sb.Length == 0) return "";
                return "[SYSTEM: LIVE WEB CONTEXT — use this to inform your answer, cite the source URL]\n" + sb;
            }
            catch { return ""; }
        }
    }
}
