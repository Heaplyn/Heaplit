// Developer: heaplyn
// Date: 2026-08-09
// Summary: Detects active network interfaces and displays local IPv4 addresses. Copy-pastes to clipboard on click.

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HeaplitLauncher
{
    public class LocalIpCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return SearchUtil.IsClose(query, "ip") || query == "net" || query == "network" || query == "wifi";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();
            double similarity = SearchUtil.GetSimilarity(query, "ip");

            var ipList = GetActiveIpv4Addresses();

            if (ipList.Count > 0)
            {
                foreach (var item in ipList)
                {
                    string ipAddress = item.Item2;
                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"{item.Item1}: {ipAddress}",
                        DESCRIPTION = "Click to copy IP address to clipboard",
                        EXECUTE = () => CopyToClipboard(ipAddress),
                        SIMILARITY = similarity
                    });
                }
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "IP: Disconnected",
                    DESCRIPTION = "No active IPv4 interfaces found",
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }

            return suggestions;
        }

        private static List<Tuple<string, string>> GetActiveIpv4Addresses()
        {
            var list = new List<Tuple<string, string>>();
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && 
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                list.Add(new Tuple<string, string>(ni.Name, ip.Address.ToString()));
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fail-safe
            }
            return list;
        }

        private static void CopyToClipboard(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                TextOverlay.Show($"📋 Copied IP Address to clipboard:\n{text}", 2000);
            }
            catch
            {
                // Fail-safe
            }
        }
    }
}
