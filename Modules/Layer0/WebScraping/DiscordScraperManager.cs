// Developer: copilot
// Date: 2026-08-12
// Summary: Discord server reader using the official Discord Bot API (Bot token only).
// NOTE: Requires a real bot application registered at discord.com/developers, invited to your own
// server with "Read Message History" permission. Automating a personal user account ("self-botting")
// violates Discord's Terms of Service and is intentionally NOT supported here.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using NetCord;

namespace HeaplitLauncher
{
    public class DiscordGuildInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class DiscordChannelInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
    }

    public class DiscordMessageInfo
    {
        public string Author { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    public static class DiscordScraperManager
    {
        private const string ApiBase = "https://discord.com/api/v10";
        private static readonly HttpClient _client = new HttpClient();

        public static bool HasToken => !string.IsNullOrWhiteSpace(SettingsManager.Current.DISCORD_BOT_TOKEN);

        public static void SaveBotToken(string token)
        {
            SettingsManager.Current.DISCORD_BOT_TOKEN = token.Trim();
            SettingsManager.Save();
        }

        private static HttpRequestMessage BuildRequest(string path)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}{path}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bot", SettingsManager.Current.DISCORD_BOT_TOKEN.Trim());
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return req;
        }

        private static async Task<JsonElement> SendAsync(string path)
        {
            if (!HasToken) throw new InvalidOperationException("No Discord bot token configured. Use 'discord token <token>' first.");

            using var resp = await _client.SendAsync(BuildRequest(path));
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Discord API error {(int)resp.StatusCode}: {body}");

            return JsonDocument.Parse(body).RootElement.Clone();
        }

        public static async Task<List<DiscordGuildInfo>> GetGuildsAsync()
        {
            var root = await SendAsync("/users/@me/guilds");
            var guilds = new List<DiscordGuildInfo>();
            foreach (var g in root.EnumerateArray())
            {
                guilds.Add(new DiscordGuildInfo
                {
                    Id = g.GetProperty("id").GetString() ?? "",
                    Name = g.GetProperty("name").GetString() ?? ""
                });
            }
            return guilds;
        }

        public static async Task<List<DiscordChannelInfo>> GetChannelsAsync(string guildId)
        {
            var root = await SendAsync($"/guilds/{guildId}/channels");
            var channels = new List<DiscordChannelInfo>();
            foreach (var c in root.EnumerateArray())
            {
                int type = c.TryGetProperty("type", out var t) ? t.GetInt32() : -1;
                // Type 0 = GUILD_TEXT, 5 = GUILD_ANNOUNCEMENT
                if (type != 0 && type != 5) continue;

                channels.Add(new DiscordChannelInfo
                {
                    Id = c.GetProperty("id").GetString() ?? "",
                    Name = c.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    Type = type
                });
            }
            return channels;
        }

        public static async Task<List<DiscordMessageInfo>> GetRecentMessagesAsync(string channelId, int limit = 25)
        {
            limit = Math.Clamp(limit, 1, 100);
            var root = await SendAsync($"/channels/{channelId}/messages?limit={limit}");
            var messages = new List<DiscordMessageInfo>();

            foreach (var m in root.EnumerateArray())
            {
                string author = m.TryGetProperty("author", out var a) && a.TryGetProperty("username", out var u) ? u.GetString() ?? "Unknown" : "Unknown";
                string content = m.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                string timestamp = m.TryGetProperty("timestamp", out var ts) ? ts.GetString() ?? "" : "";
                messages.Add(new DiscordMessageInfo { Author = author, Content = content, Timestamp = timestamp });
            }

            messages.Reverse(); // Discord returns newest-first; show chronological order
            return messages;
        }

        public static async Task<List<DiscordChannelInfo>> GetDMsAsync()
        {
            var root = await SendAsync("/users/@me/channels");
            var channels = new List<DiscordChannelInfo>();
            foreach (var c in root.EnumerateArray())
            {
                // Type 1 = DM, 3 = GROUP_DM
                int type = c.TryGetProperty("type", out var t) ? t.GetInt32() : -1;
                string recipientsName = "";

                if (c.TryGetProperty("recipients", out var recs) && recs.ValueKind == JsonValueKind.Array)
                {
                    var names = new List<string>();
                    foreach (var r in recs.EnumerateArray())
                    {
                        if (r.TryGetProperty("username", out var u))
                        {
                            names.Add(u.GetString() ?? "");
                        }
                    }
                    recipientsName = string.Join(", ", names);
                }

                channels.Add(new DiscordChannelInfo
                {
                    Id = c.GetProperty("id").GetString() ?? "",
                    Name = string.IsNullOrEmpty(recipientsName) ? "DM Channel" : recipientsName,
                    Type = type
                });
            }
            return channels;
        }

        public static async Task<string> ExportChannelMessagesToFileAsync(string channelId, string channelName, int limit = 100)
        {
            var messages = await GetRecentMessagesAsync(channelId, limit);
            string downloadDir = SettingsManager.Current.DOWNLOAD_DIRECTORY;
            if (string.IsNullOrEmpty(downloadDir))
            {
                downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            Directory.CreateDirectory(downloadDir);

            string safeName = string.Join("_", channelName.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(downloadDir, $"Discord_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.md");

            var sb = new StringBuilder();
            sb.AppendLine($"# Discord Chat Logs: {channelName}");
            sb.AppendLine($"Exported At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Channel ID: {channelId}");
            sb.AppendLine($"Message Count: {messages.Count}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            foreach (var msg in messages)
            {
                string cleanTime = msg.Timestamp;
                if (DateTime.TryParse(msg.Timestamp, out var dt))
                {
                    cleanTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
                sb.AppendLine($"**[{cleanTime}] {msg.Author}:** {msg.Content}");
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
            return filePath;
        }
    }
}