using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class GitHubManager
    {
        private static readonly HttpClient _client = new HttpClient();

        static GitHubManager()
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "Heaplit-PC-Assistant");
        }

        public static async Task<string> GetRepoInfoAsync(string ownerRepo)
        {
            try
            {
                string url = $"https://api.github.com/repos/{ownerRepo}";
                string json = await _client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var sb = new StringBuilder();
                sb.AppendLine($"GitHub Repository: {ownerRepo}");
                sb.AppendLine($"Description: {root.GetProperty("description").GetString() ?? "No description"}");
                sb.AppendLine($"Stars: {root.GetProperty("stargazers_count").GetInt32()}");
                sb.AppendLine($"Language: {root.GetProperty("language").GetString() ?? "Unknown"}");
                sb.AppendLine($"URL: {root.GetProperty("html_url").GetString() ?? ""}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error fetching GitHub repo info: {ex.Message}";
            }
        }

        public static async Task<string> ListRepoContentsAsync(string ownerRepo, string path = "")
        {
            try
            {
                string url = $"https://api.github.com/repos/{ownerRepo}/contents/{path}";
                string json = await _client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var sb = new StringBuilder();
                sb.AppendLine($"Contents of {ownerRepo}/{path}:");
                foreach (var item in root.EnumerateArray())
                {
                    string type = (item.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "dir") ? "[DIR]" : "[FILE]";
                    string name = item.TryGetProperty("name", out var nameProp) ? (nameProp.GetString() ?? "unknown") : "unknown";
                    sb.AppendLine($"{type} {name}");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error listing GitHub contents: {ex.Message}";
            }
        }

        public static async Task<string> ReadGitHubFileAsync(string ownerRepo, string filePath)
        {
            try
            {
                // We use raw.githubusercontent.com for easier text retrieval
                string url = $"https://raw.githubusercontent.com/{ownerRepo}/main/{filePath}";

                // Try main branch first, then master if it fails
                var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    url = $"https://raw.githubusercontent.com/{ownerRepo}/master/{filePath}";
                    response = await _client.GetAsync(url);
                }

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    return content.Length > 5000 ? content.Substring(0, 5000) + "\n... (truncated)" : content;
                }

                return $"Error reading GitHub file: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Error reading GitHub file: {ex.Message}";
            }
        }
    }
}
