// Developer: heaplyn
// Date: 2026-08-17
// Summary: Handles CLI commands for network diagnostics, IP discovery, and connection monitoring.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class NetworkCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "net", "network", "ping", "ip", "port", "wifi", "netstat", "speedtest", "flushdns");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim().ToLower();
            var parts = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            suggestions.Add(new CommandResult
            {
                TITLE = "📶 Network Diagnostics",
                DESCRIPTION = "Analyze local interfaces, gateways, and connection status",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "net", "network", "ping", "ip", "port", "wifi", "netstat", "speedtest", "flushdns") + 9.0 * 0.01),
                EXECUTE = () => RunNetworkAudit()
            });

            if (q.StartsWith("ping"))
            {
                string host = parts.Length > 1 ? parts[1] : "google.com";
                suggestions.Add(new CommandResult {
                    TITLE = $"📡 Ping {host}",
                    DESCRIPTION = "Measure round-trip latency to remote host",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "net", "network", "ping", "ip", "port", "wifi", "netstat", "speedtest", "flushdns") + 9.5 * 0.01),
                    EXECUTE = () => RunPing(host)
                });
            }

            suggestions.Add(new CommandResult
            {
                TITLE = "🌐 Show IP Addresses",
                DESCRIPTION = "Display both Local (LAN) and Public (WAN) IP information",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "net", "network", "ping", "ip", "port", "wifi", "netstat", "speedtest", "flushdns") + 8.5 * 0.01),
                EXECUTE = () => RunIpDiscovery()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "⚡ Flush DNS Cache",
                DESCRIPTION = "Purge Windows resolver cache to fix DNS resolution issues",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "net", "network", "ping", "ip", "port", "wifi", "netstat", "speedtest", "flushdns") + 8.0 * 0.01),
                EXECUTE = () => RunFlushDns()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "🚀 Run Speedtest",
                DESCRIPTION = "Open Ookla speedtest in browser",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "net", "network", "ping", "ip", "port", "wifi", "netstat", "speedtest", "flushdns") + 7.5 * 0.01),
                EXECUTE = () => Process.Start(new ProcessStartInfo { FileName = "https://www.speedtest.net", UseShellExecute = true })
            });

            return suggestions;
        }

        private void RunNetworkAudit()
        {
            var sb = new StringBuilder("# Network Interface Audit\n\n");
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()) {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                sb.AppendLine($"### {ni.Name}");
                sb.AppendLine($"- **Type**: {ni.NetworkInterfaceType}");
                sb.AppendLine($"- **Status**: ✅ {ni.OperationalStatus}");
                sb.AppendLine($"- **Speed**: {ni.Speed / 1000000} Mbps");
                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses) {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        sb.AppendLine($"- **IPv4**: `{addr.Address}`");
                }
                sb.AppendLine();
            }
            ContentPreviewOverlay.Show("Network Diagnostics", sb.ToString(), "markdown");
        }

        private void RunPing(string host)
        {
            Task.Run(async () => {
                TextOverlay.Show($"📡 Pinging {host}...", 2000);
                var p = new Ping();
                try {
                    var res = await p.SendPingAsync(host, 4000);
                    TextOverlay.Show($"📡 {host}: {res.RoundtripTime}ms", 4000);
                } catch { TextOverlay.Show($"❌ Ping to {host} failed.", 3000); }
            });
        }

        private void RunIpDiscovery()
        {
            Task.Run(async () => {
                TextOverlay.Show("🌐 Fetching IP addresses...", 2000);
                string local = MobileBridgeServer.GetLocalIPAddress();
                string? publicIp = "Unknown";
                try { publicIp = await new System.Net.Http.HttpClient().GetStringAsync("https://api.ipify.org"); } catch { }
                TextOverlay.Show($"📍 LAN: {local}\n🌎 WAN: {publicIp}", 5000);
            });
        }

        private void RunFlushDns()
        {
            Process.Start(new ProcessStartInfo { FileName = "ipconfig", Arguments = "/flushdns", CreateNoWindow = true, UseShellExecute = false });
            TextOverlay.Show("⚡ DNS Cache Flushed", 3000);
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("ping <host>", "Test connection latency", "ping 1.1.1.1"),
                new CommandDesc("ip", "Show local and public IPs", "ip"),
                new CommandDesc("flushdns", "Clear system DNS cache", "flushdns"),
                new CommandDesc("network", "View network interfaces", "network")
            };
        }
    }
}
