// Developer: heaplyn
// Date: 2026-08-14
// Summary: Power User Command Suite - 60+ commands for System, Dev, Network, Productivity, and Web Scraping.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HeaplitLauncher
{
    public class ExpandedCommandsHandler : ICommandHandler
    {
        [DllImport("user32.dll")] private static extern bool LockWorkStation();
        [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public bool CanHandle(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            string q = query.Trim().ToLower().Split(' ')[0];
            string[] verbs = { "sleep", "shutdown", "reboot", "restart", "lock", "wifi", "netstat", "specs", "md5", "sha256", "guid", "temp", "vol", "mute", "ping", "port", "wordcount", "res", "gmail", "json", "base64", "url", "lorem", "unix", "uptime", "battery", "gpu", "cpu", "whoami", "kill", "ip", "speedtest", "upper", "lower", "titlecase", "reverse", "sort", "unique", "weather", "stock", "crypto", "define", "news", "trash", "shred", "process", "network", "translate", "analyze", "os" };
            return verbs.Any(v => q.StartsWith(v));
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string raw = query.Trim();
            string[] parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;
            string cmd = parts[0].ToLower();
            string arg = parts.Length > 1 ? raw.Substring(parts[0].Length).Trim() : "";

            // ── Extra Requested Commands ─────────────────────────────────────────
            if (cmd == "process" || cmd == "procs")
                suggestions.Add(new CommandResult { TITLE = "📊 Process Manager", DESCRIPTION = "Open visual task manager with resource usage", EXECUTE = () => CommandParser.ExecuteFirstSuggestion("procs"), SIMILARITY = 5.0 });

            if (cmd == "network" || cmd == "netstat")
                suggestions.Add(new CommandResult { TITLE = "📶 Network Diagnostics", DESCRIPTION = "Show active connections and interface stats", EXECUTE = () => CommandParser.ExecuteFirstSuggestion("diagnostics"), SIMILARITY = 5.0 });

            if (cmd == "translate" && !string.IsNullOrEmpty(arg))
                suggestions.Add(new CommandResult { TITLE = $"🌍 Translate: {arg}", DESCRIPTION = "Use AI to translate clipboard text", EXECUTE = async () => { string t = Clipboard.GetText(); string res = await LlmRouter.AskAsync($"Translate this to {arg}: {t}"); CliOutputOverlay.Show("Translation", res); }, SIMILARITY = 4.5 });

            if (cmd == "analyze" && !string.IsNullOrEmpty(arg))
                suggestions.Add(new CommandResult { TITLE = $"🧠 Analyze: {Path.GetFileName(arg)}", DESCRIPTION = "Perform deep AI analysis on file content", EXECUTE = async () => { if (File.Exists(arg)) { string c = File.ReadAllText(arg); string res = await LlmRouter.AskAsync($"Perform a deep technical analysis of this file:\n\n{c}"); CliOutputOverlay.Show("Deep Analysis", res); } }, SIMILARITY = 4.5 });

            if (cmd == "os")
                suggestions.Add(new CommandResult { TITLE = "🖥️ HeaplitOS Info", DESCRIPTION = "Show information about the active OS development project", EXECUTE = () => { string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\HeaplitOS")); if (Directory.Exists(root)) TextOverlay.Show($"HeaplitOS Project detected at {root}", 4000); else TextOverlay.Show("HeaplitOS project folder not found.", 3000); }, SIMILARITY = 5.0 });

            // ── 1. GMAIL & WEB SCRAPING ──────────────────────────────────────────
            if (cmd == "gmail")
            {
                suggestions.Add(new CommandResult { TITLE = "📬 Gmail: Inbox Summary", DESCRIPTION = "Scrape recent emails from your linked Google account", EXECUTE = async () => CliOutputOverlay.Show("Gmail Inbox", await GmailManager.GetInboxSummaryAsync()), SIMILARITY = 5.0 });
                if (!string.IsNullOrEmpty(arg))
                    suggestions.Add(new CommandResult { TITLE = $"🔍 Gmail Search: {arg}", DESCRIPTION = $"Search for '{arg}' in your emails", EXECUTE = async () => CliOutputOverlay.Show("Gmail Search", await GmailManager.SearchEmailsAsync(arg)), SIMILARITY = 4.5 });
            }

            // ── 2. DEVELOPER TOOLKIT ─────────────────────────────────────────────
            if (cmd == "json")
                suggestions.Add(new CommandResult { TITLE = "📜 JSON Prettify", DESCRIPTION = "Format clipboard JSON with indentation", EXECUTE = () => { try { string json = Clipboard.GetText(); var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json); string formatted = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }); Clipboard.SetText(formatted); TextOverlay.Show("✅ JSON Formatted to Clipboard", 2000); } catch { TextOverlay.Show("❌ Invalid JSON in Clipboard", 2500); } }, SIMILARITY = 5.0 });
            if (cmd == "base64")
            {
                suggestions.Add(new CommandResult { TITLE = "🔗 Base64 Encode", DESCRIPTION = "Encode clipboard text", EXECUTE = () => { string t = Clipboard.GetText(); Clipboard.SetText(Convert.ToBase64String(Encoding.UTF8.GetBytes(t))); TextOverlay.Show("✅ Encoded", 1500); }, SIMILARITY = 4.0 });
                suggestions.Add(new CommandResult { TITLE = "🔓 Base64 Decode", DESCRIPTION = "Decode clipboard text", EXECUTE = () => { try { string t = Clipboard.GetText(); Clipboard.SetText(Encoding.UTF8.GetString(Convert.FromBase64String(t))); TextOverlay.Show("✅ Decoded", 1500); } catch { TextOverlay.Show("❌ Not Base64", 2000); } }, SIMILARITY = 4.0 });
            }
            if (cmd == "unix") suggestions.Add(new CommandResult { TITLE = "⌚ Unix Timestamp", DESCRIPTION = "Copy current epoch time", EXECUTE = () => { string ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(); Clipboard.SetText(ts); TextOverlay.Show($"📋 {ts}", 2000); }, SIMILARITY = 5.0 });
            if (cmd == "guid" || cmd == "uuid") suggestions.Add(new CommandResult { TITLE = "🎲 New GUID", DESCRIPTION = "Generate unique identifier", EXECUTE = () => { string g = Guid.NewGuid().ToString(); Clipboard.SetText(g); TextOverlay.Show($"📋 {g}", 2000); }, SIMILARITY = 5.0 });

            // ── 3. SYSTEM & HARDWARE ─────────────────────────────────────────────
            if (cmd == "uptime") suggestions.Add(new CommandResult { TITLE = "⏱️ System Uptime", DESCRIPTION = "Show how long the PC has been running", EXECUTE = () => { var up = TimeSpan.FromMilliseconds(Environment.TickCount64); TextOverlay.Show($"⏱️ Uptime: {up.Days}d {up.Hours}h {up.Minutes}m", 4000); }, SIMILARITY = 5.0 });
            if (cmd == "stopwatch") suggestions.Add(new CommandResult { TITLE = "⏱️ Stopwatch", DESCRIPTION = "Launch system stopwatch", EXECUTE = () => RunProcess("explorer.exe", "shell:Appsfolder\\Microsoft.WindowsAlarms_8wekyb3d8bbwe!App"), SIMILARITY = 5.0 });
            if (cmd == "timer") suggestions.Add(new CommandResult { TITLE = "⏲️ Timer", DESCRIPTION = "Launch system timer", EXECUTE = () => CommandParser.ExecuteFirstSuggestion("timer 10m"), SIMILARITY = 4.0 });
            if (cmd == "whoami") suggestions.Add(new CommandResult { TITLE = "👤 Current User", DESCRIPTION = "Show Windows username and machine", EXECUTE = () => TextOverlay.Show($"{Environment.UserName} @ {Environment.MachineName}", 3000), SIMILARITY = 5.0 });
            if (cmd == "kill" && !string.IsNullOrEmpty(arg)) suggestions.Add(new CommandResult { TITLE = $"🛑 Kill Process: {arg}", DESCRIPTION = $"Force terminate {arg}", EXECUTE = () => { foreach (var p in Process.GetProcessesByName(arg)) p.Kill(); TextOverlay.Show($"✅ Terminated {arg}", 2000); }, SIMILARITY = 4.5 });

            // ── 4. NETWORK & IP ──────────────────────────────────────────────────
            if (cmd == "ip")
            {
                suggestions.Add(new CommandResult { TITLE = "🌐 Local IP Address", DESCRIPTION = "Show LAN IP", EXECUTE = () => { string output = ExecuteProcessOutput("hostname", "-I"); TextOverlay.Show($"📍 {output.Trim()}", 3000); }, SIMILARITY = 5.0 });
                suggestions.Add(new CommandResult { TITLE = "🌍 Public IP Address", DESCRIPTION = "Fetch WAN IP from API", EXECUTE = async () => { try { var client = new System.Net.Http.HttpClient(); string ip = await client.GetStringAsync("https://api.ipify.org"); TextOverlay.Show($"🌍 {ip}", 4000); } catch { } }, SIMILARITY = 4.5 });
            }
            if (cmd == "mac") suggestions.Add(new CommandResult { TITLE = "🆔 MAC Address", DESCRIPTION = "Show network hardware ID", EXECUTE = () => { string output = ExecuteProcessOutput("getmac", "/v /fo list"); CliOutputOverlay.Show("MAC Addresses", output); }, SIMILARITY = 5.0 });

            // ── 5. PRODUCTIVITY & TEXT ───────────────────────────────────────────
            if (cmd == "upper") suggestions.Add(new CommandResult { TITLE = "🔠 UPPERCASE", DESCRIPTION = "Capitalize clipboard text", EXECUTE = () => { Clipboard.SetText(Clipboard.GetText().ToUpper()); TextOverlay.Show("✅ UPPERED", 1500); }, SIMILARITY = 5.0 });
            if (cmd == "lower") suggestions.Add(new CommandResult { TITLE = "🔡 lowercase", DESCRIPTION = "Lowercase clipboard text", EXECUTE = () => { Clipboard.SetText(Clipboard.GetText().ToLower()); TextOverlay.Show("✅ LOWERED", 1500); }, SIMILARITY = 5.0 });
            if (cmd == "reverse") suggestions.Add(new CommandResult { TITLE = "↩️ Reverse Text", DESCRIPTION = "Reverse clipboard string", EXECUTE = () => { string t = Clipboard.GetText(); Clipboard.SetText(new string(t.Reverse().ToArray())); TextOverlay.Show("✅ REVERSED", 1500); }, SIMILARITY = 5.0 });
            if (cmd == "wordcount") suggestions.Add(new CommandResult { TITLE = "📝 Word Count", DESCRIPTION = "Count words in clipboard", EXECUTE = () => { string t = Clipboard.GetText(); int c = t.Length; int w = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length; TextOverlay.Show($"📊 Words: {w} | Chars: {c}", 3000); }, SIMILARITY = 5.0 });
            if (cmd == "sort") suggestions.Add(new CommandResult { TITLE = "🔡 Sort Clipboard", DESCRIPTION = "Sort lines alphabetically", EXECUTE = () => { string t = Clipboard.GetText(); var lines = t.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).OrderBy(l => l); Clipboard.SetText(string.Join("\n", lines)); TextOverlay.Show("✅ Sorted", 1500); }, SIMILARITY = 5.0 });
            if (cmd == "unique") suggestions.Add(new CommandResult { TITLE = "💎 Unique Lines", DESCRIPTION = "Remove duplicate lines in clipboard", EXECUTE = () => { string t = Clipboard.GetText(); var lines = t.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Distinct(); Clipboard.SetText(string.Join("\n", lines)); TextOverlay.Show("✅ Duplicates Removed", 1500); }, SIMILARITY = 5.0 });
            if (cmd == "url") suggestions.Add(new CommandResult { TITLE = "🔗 URL Encode", DESCRIPTION = "Encode clipboard URL", EXECUTE = () => { Clipboard.SetText(Uri.EscapeDataString(Clipboard.GetText())); TextOverlay.Show("✅ URL Encoded", 1500); }, SIMILARITY = 5.0 });
            if (cmd == "speedtest") suggestions.Add(new CommandResult { TITLE = "🚀 Speedtest", DESCRIPTION = "Open internet speed test", EXECUTE = () => Process.Start(new ProcessStartInfo { FileName = "https://www.speedtest.net", UseShellExecute = true }), SIMILARITY = 5.0 });
            if (cmd == "lorem") suggestions.Add(new CommandResult { TITLE = "📜 Lorem Ipsum", DESCRIPTION = "Copy placeholder text to clipboard", EXECUTE = () => { string t = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."; Clipboard.SetText(t); TextOverlay.Show("📋 Lorem Ipsum Copied", 2000); }, SIMILARITY = 5.0 });
            if (cmd == "titlecase") suggestions.Add(new CommandResult { TITLE = "🔠 Title Case", DESCRIPTION = "Convert clipboard to Title Case", EXECUTE = () => { string t = Clipboard.GetText(); var ti = System.Globalization.CultureInfo.CurrentCulture.TextInfo; Clipboard.SetText(ti.ToTitleCase(t.ToLower())); TextOverlay.Show("✅ Title Cased", 1500); }, SIMILARITY = 5.0 });

            // ── 6. WEB QUICK FETCH ───────────────────────────────────────────────
            if (cmd == "weather" && !string.IsNullOrEmpty(arg)) suggestions.Add(new CommandResult { TITLE = $"🌤️ Weather: {arg}", DESCRIPTION = "Search weather for city", EXECUTE = () => Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?q=weather+{arg}", UseShellExecute = true }), SIMILARITY = 4.5 });
            if (cmd == "crypto" && !string.IsNullOrEmpty(arg)) suggestions.Add(new CommandResult { TITLE = $"🪙 Crypto: {arg}", DESCRIPTION = "Check current coin price", EXECUTE = () => Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?q={arg}+price", UseShellExecute = true }), SIMILARITY = 4.5 });
            if (cmd == "stock" && !string.IsNullOrEmpty(arg)) suggestions.Add(new CommandResult { TITLE = $"📈 Stock: {arg}", DESCRIPTION = "Check ticker price", EXECUTE = () => Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?q=stock+{arg}", UseShellExecute = true }), SIMILARITY = 4.5 });
            if (cmd == "define" && !string.IsNullOrEmpty(arg)) suggestions.Add(new CommandResult { TITLE = $"📖 Define: {arg}", DESCRIPTION = "Lookup word definition", EXECUTE = () => Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?q=define+{arg}", UseShellExecute = true }), SIMILARITY = 4.5 });
            if (cmd == "news") suggestions.Add(new CommandResult { TITLE = "📰 Latest News", DESCRIPTION = "Open top headlines", EXECUTE = () => Process.Start(new ProcessStartInfo { FileName = "https://news.google.com", UseShellExecute = true }), SIMILARITY = 5.0 });

            // ── 7. FILE & CLEANUP ────────────────────────────────────────────────
            if (cmd == "trash" || cmd == "empty") suggestions.Add(new CommandResult { TITLE = "🗑️ Empty Recycle Bin", DESCRIPTION = "Permanent delete all trash", EXECUTE = () => CommandParser.ExecuteFirstSuggestion("recycle bin"), SIMILARITY = 5.0 });
            if (cmd == "temp") suggestions.Add(new CommandResult { TITLE = "🧹 Clear Temp Files", DESCRIPTION = "Purge Windows %TEMP% folder", EXECUTE = () => { int d = PurgeTempFolder(); TextOverlay.Show($"🧹 Cleared {d} files", 2500); }, SIMILARITY = 5.0 });
            if (cmd == "shred" && !string.IsNullOrEmpty(arg)) suggestions.Add(new CommandResult { TITLE = $"💥 Shred File: {Path.GetFileName(arg)}", DESCRIPTION = "Securely wipe file from disk", EXECUTE = () => { if (File.Exists(arg)) { File.WriteAllBytes(arg, new byte[new FileInfo(arg).Length]); File.Delete(arg); TextOverlay.Show("💥 File Shredded", 2000); } }, SIMILARITY = 4.0 });

            // ── 8. SYSTEM POWER & STATS ──────────────────────────────────────────
            if (cmd == "lock") suggestions.Add(new CommandResult { TITLE = "🔒 Lock Workstation", DESCRIPTION = "Lock screen instantly", EXECUTE = () => LockWorkStation(), SIMILARITY = 8.0 });
            if (cmd == "sleep") suggestions.Add(new CommandResult { TITLE = "💤 Sleep", DESCRIPTION = "Suspend system", EXECUTE = () => RunProcess("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0"), SIMILARITY = 5.0 });
            if (cmd == "battery") suggestions.Add(new CommandResult { TITLE = "🔋 Battery Status", DESCRIPTION = "Show charge and health", EXECUTE = () => { var power = System.Windows.Forms.SystemInformation.PowerStatus; TextOverlay.Show($"🔋 {power.BatteryLifePercent * 100}% | {power.PowerLineStatus}", 4000); }, SIMILARITY = 5.0 });
            if (cmd == "cpu") suggestions.Add(new CommandResult { TITLE = "🧠 CPU Usage", DESCRIPTION = "Show active processor load", EXECUTE = () => CommandParser.ExecuteFirstSuggestion("system stats"), SIMILARITY = 5.0 });
            if (cmd == "gpu") suggestions.Add(new CommandResult { TITLE = "🎮 GPU Info", DESCRIPTION = "Open graphics settings", EXECUTE = () => RunProcess("control.exe", "desk.cpl,,3"), SIMILARITY = 5.0 });

            return suggestions;
        }

        private static string ExecuteProcessOutput(string f, string a) { try { var p = Process.Start(new ProcessStartInfo { FileName = f, Arguments = a, CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true }); return p?.StandardOutput.ReadToEnd() ?? ""; } catch { return ""; } }
        private static void RunProcess(string f, string a) { try { Process.Start(new ProcessStartInfo { FileName = f, Arguments = a, CreateNoWindow = true, UseShellExecute = false }); } catch { } }
        private static int PurgeTempFolder() { int d = 0; try { foreach (var f in Directory.GetFiles(Path.GetTempPath())) { try { File.Delete(f); d++; } catch { } } } catch { } return d; }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc> {
                new CommandDesc("gmail", "Read Gmail inbox summary", "gmail"),
                new CommandDesc("json prettify", "Format clipboard JSON", "json"),
                new CommandDesc("base64 encode/decode", "Base64 processing", "base64"),
                new CommandDesc("guid", "Generate unique ID", "guid"),
                new CommandDesc("uptime", "PC run time", "uptime"),
                new CommandDesc("whoami", "User info", "whoami"),
                new CommandDesc("kill <process>", "Terminate app", "kill notepad"),
                new CommandDesc("ip", "Show LAN and Public IP", "ip"),
                new CommandDesc("upper / lower", "Change text case", "upper"),
                new CommandDesc("weather <city>", "Check forecast", "weather london"),
                new CommandDesc("temp clear", "Purge temp files", "temp"),
                new CommandDesc("lock", "Lock computer", "lock")
            };
        }
    }
}
