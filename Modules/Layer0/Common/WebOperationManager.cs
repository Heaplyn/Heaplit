// Developer: heaplyn
// Date: 2026-08-17
// Summary: Web Operations Service implementation.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class WebOperationManager : IWebOperationService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public WebOperationManager()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        async Task<string> IWebOperationService.SearchWebAsync(string query)
        {
            try {
                string url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
                string html = await _httpClient.GetStringAsync(url);
                var matches = Regex.Matches(html, @"<a class=""result__snippet""[^>]*href=""(?<url>[^""]+)""[^>]*>(?<desc>.*?)</a>", RegexOptions.Singleline);
                var sb = new StringBuilder();
                foreach (Match m in matches.Take(5)) {
                    string u = m.Groups["url"].Value;
                    if (u.Contains("uddg=")) u = Uri.UnescapeDataString(u.Substring(u.IndexOf("uddg=") + 5).Split('&')[0]);
                    sb.AppendLine($"- {u}: {Regex.Replace(m.Groups["desc"].Value, @"<[^>]*?>", "").Trim()}");
                }
                return sb.ToString();
            } catch (Exception ex) { return "Search Error: " + ex.Message; }
        }

        async Task<string> IWebOperationService.ScrapeWebpageAsync(string url)
        {
            try {
                string html = await _httpClient.GetStringAsync(url);
                string text = Regex.Replace(html, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"<[^>]*?>", "", RegexOptions.Singleline);
                var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 5).Take(200);
                return await CoreRegistry.Intelligence.Llm.AskAsync($"Summarize: {url}\n{string.Join("\n", lines)}");
            } catch (Exception ex) { return "Scrape Error: " + ex.Message; }
        }

        async Task<string> IWebOperationService.DownloadFileAsync(string url, string? destPath)
        {
            try {
                string path = destPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                using var resp = await _httpClient.GetAsync(url);
                string name = Path.GetFileName(new Uri(url).LocalPath);
                using var fs = new FileStream(Path.Combine(path, name), FileMode.Create);
                await resp.Content.CopyToAsync(fs);
                return $"Downloaded to: {Path.Combine(path, name)}";
            } catch (Exception ex) { return "Download Error: " + ex.Message; }
        }

        async Task<string> IWebOperationService.IngestDocumentationAsync(string url)
        {
            string scraped = await ((IWebOperationService)this).ScrapeWebpageAsync(url);
            SemanticMemoryManager.AddMemory($"Documentation: {url}\n{scraped}", "Knowledge", "Web", 0.9);
            return "Documentation ingested.";
        }

        public static async Task<string> SearchAiEndpointsAsync(string query)
        {
            try {
                // Scrape/Search specifically for AI provider status or new endpoints
                string searchRes = await CoreRegistry.System.Web.SearchWebAsync("list of public openai compatible llm endpoints " + query);
                return $"## AUTO-DISCOVERED AI ENDPOINTS\n{searchRes}";
            } catch { return "Discovery failed."; }
        }

        public static Task<string> SearchWebAsync(string query) => CoreRegistry.System.Web.SearchWebAsync(query);
        public static Task<string> ScrapeWebpageAsync(string url) => CoreRegistry.System.Web.ScrapeWebpageAsync(url);
        public static Task<string> DownloadFileAsync(string url, string? destPath = null) => CoreRegistry.System.Web.DownloadFileAsync(url, destPath);
        public static Task<string> IngestDocumentationAsync(string url) => CoreRegistry.System.Web.IngestDocumentationAsync(url);

        public static Task<string> DownloadListAsync(string listUrl) => Task.FromResult("Deprecated");
        public static Task<string> DiscoverAndDownloadMediaAsync(string url, string type) => Task.FromResult("Deprecated");
        public static Task<string> SearchRegistryAsync(string type, string query) => Task.FromResult("Deprecated");
        public static Task<string> ProcessDataFineAsync(string mode, string op, string data) => Task.FromResult("Deprecated");
    }
}
