using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class DiscordScraperSkeleton
    {
        private const string ApiBase = "https://discord.com/api/v10";
        private static readonly HttpClient _client = new HttpClient();

        // Replace with your token
        private static string Token = SettingsManager.Current.DISCORD_BOT_TOKEN; 

        public static async Task<List<Dictionary<string, object>>> GetMessagesAsync(string channelId, int limit = 50)
        {
            var url = $"{ApiBase}/channels/{channelId}/messages?limit={limit}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bot", Token);
            
            var resp = await _client.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
            var messages = new List<Dictionary<string, object>>();
            foreach (var msg in doc.RootElement.EnumerateArray())
            {
                messages.Add(new Dictionary<string, object>
                {
                    { "author", msg.GetProperty("author").GetProperty("username").GetString() },
                    { "content", msg.GetProperty("content").GetString() },
                    { "timestamp", msg.GetProperty("timestamp").GetString() }
                });
            }
            
            // Discord returns newest first; reverse for chronological order
            messages.Reverse();
            return messages;
        }

        public static async Task<string> SaveMessagesToFileAsync(string channelId, string channelName, int limit = 50)
        {
            var messages = await GetMessagesAsync(channelId, limit);
            var sb = new StringBuilder();
            sb.AppendLine($"# {channelName} ({channelId})\n");

            foreach (var msg in messages)
            {
                sb.AppendLine($"**[{msg["timestamp"]}] {msg["author"]}**: {msg["content"]}\n");
            }

            var path = $@"C:\temp\discord_{channelName}_{DateTime.Now:yyyyMMdd}.md";
            await File.WriteAllTextAsync(path, sb.ToString());
            return path;
        }
    }
}
