// Developer: heaplyn
// Date: 2026-08-13
// Summary: Command Handler for Model Context Protocol (MCP) Registry Studio and MCP tools enumeration.

using System;
using System.Collections.Generic;
using System.Linq;

namespace HeaplitLauncher
{
    public class McpCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "mcp", "mcpstudio", "mcpgui", "mcpservers", "mcp tools", "mcp list");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string lower = query.Trim().ToLower();

            suggestions.Add(new CommandResult
            {
                TITLE = "⚡ Open MCP Registry & Server Manager Studio",
                DESCRIPTION = "Manage Model Context Protocol servers (Roblox, Filesystem, Brave Search, Memory, GitHub)",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "mcp", "mcpstudio", "mcpgui", "mcpservers", "mcp tools", "mcp list") + 6.0 * 0.01),
                EXECUTE = () => McpStudioOverlay.ShowOverlay()
            });

            if (lower.Contains("roblox") || lower == "mcp add roblox")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🎮 Register Roblox Studio MCP Server",
                    DESCRIPTION = "claude mcp add --transport stdio Roblox_Studio -- cmd.exe /c cd /d %LOCALAPPDATA%\\Roblox && .\\mcp.bat",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "mcp", "mcpstudio", "mcpgui", "mcpservers", "mcp tools", "mcp list") + 5.5 * 0.01),
                    EXECUTE = () =>
                    {
                        McpManager.AddServer(new McpServerConfig
                        {
                            Name = "Roblox_Studio",
                            Transport = "STDIO",
                            Command = "cmd.exe",
                            Args = new List<string> { "/c", "cd /d %LOCALAPPDATA%\\Roblox && .\\mcp.bat" }
                        });
                        TextOverlay.Show("🎮 Registered Roblox Studio MCP Server!", 3000);
                    }
                });
            }

            if (lower.Contains("filesystem") || lower.Contains("files"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "📁 Register Filesystem MCP Server",
                    DESCRIPTION = "npx -y @modelcontextprotocol/server-filesystem %USERPROFILE%",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "mcp", "mcpstudio", "mcpgui", "mcpservers", "mcp tools", "mcp list") + 5.5 * 0.01),
                    EXECUTE = () =>
                    {
                        McpManager.AddServer(new McpServerConfig
                        {
                            Name = "Filesystem",
                            Transport = "STDIO",
                            Command = "npx",
                            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }
                        });
                        TextOverlay.Show("📁 Registered Filesystem MCP Server!", 3000);
                    }
                });
            }

            // List connected MCP servers
            McpManager.LoadConfig();
            foreach (var s in McpManager.Servers)
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"⚡ Test Connection: MCP Server [{s.Name}]",
                    DESCRIPTION = $"{s.Command} {string.Join(" ", s.Args)}",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "mcp", "mcpstudio", "mcpgui", "mcpservers", "mcp tools", "mcp list") + 4.0 * 0.01),
                    EXECUTE = async () =>
                    {
                        bool ok = await McpManager.TestServerConnectionAsync(s);
                        TextOverlay.Show(ok ? $"🟢 MCP Server '{s.Name}' Active!" : $"🔴 MCP Server '{s.Name}' Error!", 3000);
                    }
                });
            }

            return suggestions;
        }
    }
}
