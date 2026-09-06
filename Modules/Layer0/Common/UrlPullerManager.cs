// Developer: heaplyn
// Date: 2026-08-21
// Summary: High-Performance layer 0 web puller service supporting custom headers, cookies, methods, and payload.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class PullRequestConfig
    {
        public string Url { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, string> Cookies { get; set; } = new();
        public string Method { get; set; } = "GET";
        public string Body { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/json";
    }

    public static class UrlPullerManager
    {
        public static async Task<string> PullAsync(PullRequestConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Url))
            {
                return "Error: Target URL cannot be empty.";
            }

            try
            {
                var handler = new HttpClientHandler();
                if (config.Cookies != null && config.Cookies.Count > 0)
                {
                    handler.CookieContainer = new System.Net.CookieContainer();
                    var uri = new Uri(config.Url);
                    foreach (var pair in config.Cookies)
                    {
                        handler.CookieContainer.Add(uri, new System.Net.Cookie(pair.Key, pair.Value));
                    }
                }

                using var client = new HttpClient(handler);
                var req = new HttpRequestMessage(new HttpMethod(config.Method.ToUpper()), config.Url);

                if (config.Headers != null)
                {
                    foreach (var pair in config.Headers)
                    {
                        req.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
                    }
                }

                if (!string.IsNullOrEmpty(config.Body) && (config.Method.ToUpper() == "POST" || config.Method.ToUpper() == "PUT" || config.Method.ToUpper() == "PATCH"))
                {
                    req.Content = new StringContent(config.Body, Encoding.UTF8, config.ContentType);
                }

                var resp = await client.SendAsync(req);
                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"Error executing pull request: {ex.Message}";
            }
        }
    }
}
