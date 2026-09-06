// Developer: heaplyn
// Date: 2026-08-10
// Summary: Manages outbound P2P connections to peer Heaplit PCs for offloading LLM inference.
//          Supports multiple registered peers, auto-selects lowest-load peer, persists peer list.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class HeaplitPeerInfo
    {
        public string Url { get; set; } = "";
        public string Secret { get; set; } = "";
        public string Nickname { get; set; } = "";
        // Runtime status (not persisted)
        public string PcName { get; set; } = "Unknown";
        public List<string> Backends { get; set; } = new();
        public List<string> Models { get; set; } = new();
        public double CpuLoad { get; set; } = 0;
        public double RamFreeGb { get; set; } = 0;
        public long LatencyMs { get; set; } = 9999;
        public bool IsOnline { get; set; } = false;
        public DateTime LastChecked { get; set; } = DateTime.MinValue;
    }

    public static class HeaplitP2PClient
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private static List<HeaplitPeerInfo> _peers = new();
        private static string PeersPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "p2p_peers.json");

        static HeaplitP2PClient()
        {
            LoadPeers();
        }

        public static IReadOnlyList<HeaplitPeerInfo> Peers => _peers.AsReadOnly();

        public static void LoadPeers()
        {
            try
            {
                if (File.Exists(PeersPath))
                {
                    string json = File.ReadAllText(PeersPath);
                    _peers = JsonSerializer.Deserialize<List<HeaplitPeerInfo>>(json) ?? new();
                }
            }
            catch { _peers = new(); }
        }

        public static void SavePeers()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PeersPath)!);
                string json = JsonSerializer.Serialize(_peers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PeersPath, json);
            }
            catch { }
        }

        public static HeaplitPeerInfo AddPeer(string url, string secret = "", string nickname = "")
        {
            url = url.TrimEnd('/');
            var existing = _peers.FirstOrDefault(p => p.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Secret = secret;
                existing.Nickname = string.IsNullOrEmpty(nickname) ? existing.Nickname : nickname;
                SavePeers();
                return existing;
            }

            var peer = new HeaplitPeerInfo
            {
                Url = url,
                Secret = secret,
                Nickname = string.IsNullOrEmpty(nickname) ? url : nickname
            };
            _peers.Add(peer);
            SavePeers();
            return peer;
        }

        public static void RemovePeer(string url)
        {
            url = url.TrimEnd('/');
            _peers.RemoveAll(p => p.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
            SavePeers();
        }

        public static async Task<bool> ProbePeerAsync(HeaplitPeerInfo peer)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var req = BuildRequest(HttpMethod.Get, $"{peer.Url}/p2p/health", peer.Secret);
                var resp = await _http.SendAsync(req);
                sw.Stop();

                if (resp.IsSuccessStatusCode)
                {
                    peer.LatencyMs = sw.ElapsedMilliseconds;
                    peer.IsOnline = true;
                    peer.LastChecked = DateTime.Now;

                    try
                    {
                        var infoReq = BuildRequest(HttpMethod.Get, $"{peer.Url}/p2p/info", peer.Secret);
                        var infoResp = await _http.SendAsync(infoReq);
                        if (infoResp.IsSuccessStatusCode)
                        {
                            string infoJson = await infoResp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(infoJson);
                            var root = doc.RootElement;
                            peer.PcName = root.TryGetProperty("pc_name", out var pcn) ? pcn.GetString() ?? "Unknown" : "Unknown";
                            peer.CpuLoad = root.TryGetProperty("cpu_load", out var cpu) ? cpu.GetDouble() : 0;
                            peer.RamFreeGb = root.TryGetProperty("ram_free_gb", out var ram) ? ram.GetDouble() : 0;
                            peer.Backends = root.TryGetProperty("backends", out var be)
                                ? be.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new();
                            peer.Models = root.TryGetProperty("models", out var mo)
                                ? mo.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new();
                        }
                    }
                    catch { }

                    return true;
                }
            }
            catch { }

            peer.IsOnline = false;
            peer.LastChecked = DateTime.Now;
            return false;
        }

        public static async Task ProbeAllPeersAsync()
        {
            var tasks = _peers.Select(p => ProbePeerAsync(p));
            await Task.WhenAll(tasks);
        }

        public static async Task<string> AskBestPeerAsync(string prompt, List<ChatTurn>? history = null, string model = "auto")
        {
            await ProbeAllPeersAsync();

            var online = _peers
                .Where(p => p.IsOnline)
                .OrderBy(p => p.CpuLoad * 0.7 + (p.LatencyMs / 100.0) * 0.3)
                .ToList();

            if (online.Count == 0)
                throw new Exception("No P2P peers are online. Add a peer via 'llm' settings.");

            foreach (var peer in online)
            {
                try { return await AskPeerAsync(peer, prompt, history, model); }
                catch (Exception ex)
                {
                    ChatOverlay.LogConsoleAction("P2P Peer Failed", $"{peer.Nickname}: {ex.Message}");
                    peer.IsOnline = false;
                }
            }

            throw new Exception("All P2P peers failed to respond.");
        }

        public static async Task<string> AskPeerAsync(HeaplitPeerInfo peer, string prompt, List<ChatTurn>? history = null, string model = "auto")
        {
            var historyArr = history?.Select(h => new { role = h.Role, text = h.Text }).ToArray()
                             ?? Array.Empty<object>();
            var payload = JsonSerializer.Serialize(new { prompt, model, secret = peer.Secret, history = historyArr });
            var req = BuildRequest(HttpMethod.Post, $"{peer.Url}/p2p/ask", peer.Secret, payload);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var resp = await _http.SendAsync(req);
            sw.Stop();
            peer.LatencyMs = sw.ElapsedMilliseconds;

            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Peer returned {(int)resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            string response = root.TryGetProperty("response", out var r) ? r.GetString() ?? "" : body;
            string modelUsed = root.TryGetProperty("model_used", out var mu) ? mu.GetString() ?? "?" : "?";
            ChatOverlay.LogConsoleAction("P2P Response", $"From {peer.PcName} via {modelUsed} in {sw.ElapsedMilliseconds}ms");
            return response;
        }

        private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string secret, string? jsonBody = null)
        {
            var req = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(secret))
                req.Headers.Add("X-Heaplit-Secret", secret);
            if (jsonBody != null)
                req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return req;
        }
    }
}
