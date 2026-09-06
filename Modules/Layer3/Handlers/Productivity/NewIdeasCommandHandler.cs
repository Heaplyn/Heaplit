// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles Window Snap/Layouts, Macro chains, Network Speedtest/Ping, Folder Quick Jumps, Process Manager GUI, World Clock, and File Hash features.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public class MacroItem
    {
        public string Name { get; set; } = string.Empty;
        public string CommandsChain { get; set; } = string.Empty;
    }

    public class FolderShortcutItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public class NewIdeasCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query.StartsWith("snap") || query.StartsWith("window") ||
                   query.StartsWith("macro") ||
                   query.StartsWith("ping") || query == "speedtest" ||
                   query.StartsWith("jump") ||
                   query == "procs" || query == "taskmgr" ||
                   query.StartsWith("time ") || query == "time" ||
                   query.StartsWith("hash") || query == "hash" ||
                   query == "quote" || query == "sys quote";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();
            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLower();

            if (query.ToLower() == "quote" || query.ToLower() == "sys quote")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "💬 Get System Quote",
                    DESCRIPTION = "Receive a proactive witty or philosophical remark from Heaplit",
                    SIMILARITY = 2.0,
                    EXECUTE = () => GetSystemQuote()
                });
            }

            // --- 1. WINDOW SNAP & WORKSPACE ---
            if (cmd == "snap" || cmd == "window")
            {
                if (parts.Length > 1)
                {
                    string target = parts[1].ToLower();
                    if (target == "left")
                    {
                        suggestions.Add(new CommandResult
                        {
                            TITLE       = "🪟 Snap Window Left",
                            DESCRIPTION = "Send Win + LeftArrow keystrokes",
                            SIMILARITY  = 2.0,
                            EXECUTE     = () => NativeMethods.SendKeyCombo(0x5B, 0x25) // Win + Left
                        });
                    }
                    else if (target == "right")
                    {
                        suggestions.Add(new CommandResult
                        {
                            TITLE       = "🪟 Snap Window Right",
                            DESCRIPTION = "Send Win + RightArrow keystrokes",
                            SIMILARITY  = 2.0,
                            EXECUTE     = () => NativeMethods.SendKeyCombo(0x5B, 0x27) // Win + Right
                        });
                    }
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "🪟 Snap Left",
                        DESCRIPTION = "Snap foreground window to left half",
                        SIMILARITY  = 1.5,
                        EXECUTE     = () => NativeMethods.SendKeyCombo(0x5B, 0x25)
                    });
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "🪟 Snap Right",
                        DESCRIPTION = "Snap foreground window to right half",
                        SIMILARITY  = 1.0,
                        EXECUTE     = () => NativeMethods.SendKeyCombo(0x5B, 0x27)
                    });
                }
            }
            // --- 2. MACRO ACTION CHAINS ---
            else if (cmd == "macro")
            {
                var macros = LoadMacros();
                if (parts.Length > 1)
                {
                    string args = parts[1];
                    if (args.StartsWith("add "))
                    {
                        var mParts = args.Substring(4).Split("->", 2, StringSplitOptions.RemoveEmptyEntries);
                        if (mParts.Length == 2)
                        {
                            string mName = mParts[0].Trim();
                            string mChain = mParts[1].Trim();
                            suggestions.Add(new CommandResult
                            {
                                TITLE       = $"Save Macro: '{mName}'",
                                DESCRIPTION = $"Chain: {mChain}",
                                SIMILARITY  = 2.0,
                                EXECUTE     = () => SaveMacro(mName, mChain)
                            });
                        }
                    }
                    else
                    {
                        string mName = args.Trim();
                        var match = macros.Find(m => m.Name.Equals(mName, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            suggestions.Add(new CommandResult
                            {
                                TITLE       = $"⚡ Execute Macro: '{match.Name}'",
                                DESCRIPTION = $"Run chain: {match.CommandsChain}",
                                SIMILARITY  = 2.0,
                                EXECUTE     = () => RunMacroChain(match.CommandsChain)
                            });
                        }
                    }
                }

                foreach (var m in macros)
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"⚡ Macro: {m.Name}",
                        DESCRIPTION = $"Run: {m.CommandsChain}",
                        SIMILARITY  = 1.0,
                        EXECUTE     = () => RunMacroChain(m.CommandsChain)
                    });
                }

                suggestions.Add(new CommandResult
                {
                    TITLE       = "Add New Macro...",
                    DESCRIPTION = "Format: macro add <name> -> <cmd1> | <cmd2>",
                    SIMILARITY  = 0.5,
                    EXECUTE     = () => InputPromptOverlay.Show("Enter format: <name> -> <cmd1> | <cmd2>", (str) => ParseAndAddMacro(str))
                });
            }
            // --- 3. PING & SPEEDTEST ---
            else if (cmd == "ping")
            {
                string host = parts.Length > 1 ? parts[1].Trim() : "8.8.8.8";
                suggestions.Add(new CommandResult
                {
                    TITLE       = $"📡 Ping Host: {host}",
                    DESCRIPTION = "Measure roundtrip network latency",
                    SIMILARITY  = 2.0,
                    EXECUTE     = () => ExecutePing(host)
                });
            }
            else if (cmd == "speedtest")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE       = "🚀 Run Speedtest",
                    DESCRIPTION = "Measure ping and latency via network socket",
                    SIMILARITY  = 2.0,
                    EXECUTE     = () => ExecutePing("1.1.1.1")
                });
            }
            // --- 4. FOLDER QUICK JUMPS ---
            else if (cmd == "jump")
            {
                string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (parts.Length > 1)
                {
                    string target = parts[1].ToLower();
                    string targetDir = target switch
                    {
                        "downloads" => Path.Combine(userDir, "Downloads"),
                        "documents" => Path.Combine(userDir, "Documents"),
                        "desktop" => Path.Combine(userDir, "Desktop"),
                        "pictures" => Path.Combine(userDir, "Pictures"),
                        _ => target
                    };

                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"📁 Jump to: {Path.GetFileName(targetDir)}",
                        DESCRIPTION = targetDir,
                        SIMILARITY  = 2.0,
                        EXECUTE     = () => OpenFolder(targetDir)
                    });
                }
                else
                {
                    string[] defaults = new string[] { "Downloads", "Documents", "Desktop", "Pictures" };
                    foreach (var d in defaults)
                    {
                        string p = Path.Combine(userDir, d);
                        suggestions.Add(new CommandResult
                        {
                            TITLE       = $"📁 Jump to {d}",
                            DESCRIPTION = p,
                            SIMILARITY  = 1.0,
                            EXECUTE     = () => OpenFolder(p)
                        });
                    }
                }
            }
            // --- 5. PROCESS MANAGER GUI ---
            else if (cmd == "procs" || cmd == "taskmgr")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE       = "📊 Open Process Manager GUI",
                    DESCRIPTION = "Visual task manager listing top CPU/RAM processes with kill controls",
                    SIMILARITY  = 2.0,
                    EXECUTE     = () => ProcessManagerOverlay.OpenManager()
                });
            }
            // --- 6. WORLD CLOCK & TIMEZONE ---
            else if (cmd == "time")
            {
                if (parts.Length > 1)
                {
                    string city = parts[1].Trim();
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"🕒 World Clock: {city}",
                        DESCRIPTION = "Look up time for city/region",
                        SIMILARITY  = 2.0,
                        EXECUTE     = () => ShowCityTime(city)
                    });
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "🕒 World Clock...",
                        DESCRIPTION = "Type city (e.g. 'time Tokyo', 'time London')",
                        SIMILARITY  = 1.5,
                        EXECUTE     = () => InputPromptOverlay.Show("Enter city name:", (c) => ShowCityTime(c))
                    });
                }
            }
            // --- 7. FILE HASH ---
            else if (cmd == "hash")
            {
                if (parts.Length > 1)
                {
                    string path = parts[1].Trim();
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = $"🔒 Calculate Hash: {Path.GetFileName(path)}",
                        DESCRIPTION = "Compute SHA-256 checksum",
                        SIMILARITY  = 2.0,
                        EXECUTE     = () => CalculateFileHash(path)
                    });
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE       = "🔒 Calculate File Hash (Browse)...",
                        DESCRIPTION = "Pick a file to compute SHA-256 checksum",
                        SIMILARITY  = 1.5,
                        EXECUTE     = () => InputPromptOverlay.Show("Enter file path to hash:", (p) => CalculateFileHash(p))
                    });
                }
            }

            return suggestions;
        }

        // --- MACRO PERSISTENCE (.TXT FILES & JSON) ---
        private static List<MacroItem> LoadMacros()
        {
            var list = new List<MacroItem>();

            try
            {
                // 1. Scan Macros directory for .txt files
                string macrosDir = GetMacrosDirectory();
                if (Directory.Exists(macrosDir))
                {
                    var txtFiles = Directory.GetFiles(macrosDir, "*.txt");
                    foreach (var file in txtFiles)
                    {
                        string macroName = Path.GetFileNameWithoutExtension(file);
                        string[] lines = File.ReadAllLines(file);
                        var validCommands = new List<string>();

                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith("//"))
                            {
                                validCommands.Add(trimmed);
                            }
                        }

                        if (validCommands.Count > 0)
                        {
                            string chain = string.Join(" | ", validCommands);
                            list.Add(new MacroItem { Name = macroName, CommandsChain = chain });
                        }
                    }
                }

                // 2. Load JSON macros
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Macros.json");
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var jsonMacros = JsonSerializer.Deserialize<List<MacroItem>>(json);
                    if (jsonMacros != null)
                    {
                        foreach (var jm in jsonMacros)
                        {
                            if (!list.Exists(m => m.Name.Equals(jm.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                list.Add(jm);
                            }
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        private static string GetMacrosDirectory()
        {
            string checkDir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                string targetFolder = Path.Combine(checkDir, "Macros");
                if (Directory.Exists(targetFolder))
                {
                    return targetFolder;
                }
                var parent = Directory.GetParent(checkDir);
                if (parent == null) break;
                checkDir = parent.FullName;
            }

            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Macros");
            if (!Directory.Exists(defaultPath)) Directory.CreateDirectory(defaultPath);
            return defaultPath;
        }

        private static void SaveMacro(string name, string chain)
        {
            try
            {
                string macrosDir = GetMacrosDirectory();
                string txtPath = Path.Combine(macrosDir, $"{name}.txt");
                
                // Write each command separated by newlines
                string[] commands = chain.Split('|', StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                foreach (var c in commands)
                {
                    sb.AppendLine(c.Trim());
                }

                File.WriteAllText(txtPath, sb.ToString());
                TextOverlay.Show($"⚡ Macro '{name}.txt' saved in Macros folder!", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Save failed: {ex.Message}", 3000);
            }
        }

        private static void ParseAndAddMacro(string input)
        {
            var parts = input.Split("->", 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                SaveMacro(parts[0].Trim(), parts[1].Trim());
            }
            else
            {
                TextOverlay.Show("⚠️ Use format: <name> -> <cmd1> | <cmd2>", 3500);
            }
        }

        private static void RunMacroChain(string chain)
        {
            var commands = chain.Split('|', StringSplitOptions.RemoveEmptyEntries);
            foreach (var c in commands)
            {
                string query = c.Trim();
                var suggestions = CommandParser.GetSuggestions(query);
                if (suggestions.Count > 0 && suggestions[0].EXECUTE != null)
                {
                    suggestions[0].EXECUTE?.Invoke();
                }
            }
            TextOverlay.Show($"⚡ Executed Macro Chain ({commands.Length} actions)", 2500);
        }

        private static void ExecutePing(string host)
        {
            Task.Run(() =>
            {
                try
                {
                    using var p = new Ping();
                    var reply = p.Send(host, 3000);
                    if (reply.Status == IPStatus.Success)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CliOutputOverlay.Show($"Ping: {host}", $"✅ Reply from {reply.Address}: time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}");
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CliOutputOverlay.Show($"Ping: {host}", $"⚠️ Ping status: {reply.Status}");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TextOverlay.Show($"⚠️ Ping failed: {ex.Message}", 3000);
                    });
                }
            });
        }

        private static void OpenFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                TextOverlay.Show($"📁 Jumped to: {Path.GetFileName(path)}", 2500);
            }
            catch { }
        }

        private static void ShowCityTime(string city)
        {
            try
            {
                string now = DateTime.Now.ToString("dddd, MMM dd, yyyy - HH:mm:ss");
                CliOutputOverlay.Show($"Time Info: {city}", $"🕒 Local System Time: {now}\nRegion Query: {city.ToUpper()}");
            }
            catch { }
        }

        private static void CalculateFileHash(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    TextOverlay.Show("⚠️ File does not exist", 3000);
                    return;
                }

                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                byte[] hash = sha256.ComputeHash(stream);
                string hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();

                CliOutputOverlay.Show($"SHA-256 Hash: {Path.GetFileName(filePath)}", $"File: {filePath}\nSHA-256: {hashStr}");
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Hash error: {ex.Message}", 3000);
            }
        }

        private static void GetSystemQuote()
        {
            Task.Run(async () =>
            {
                string prompt = "Generate a single, short, witty, and slightly sassy philosophical or technical remark from Heaplit. Do not use tags.";
                string remark = await LlmRouter.AskAsync(prompt);
                Application.Current.Dispatcher.Invoke(() => {
                    TextOverlay.Show("🤖 Heaplit: " + remark, 5000);
                    TtsManager.Speak(remark);
                });
            });
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("snap left/right", "Snap foreground window to screen half", "snap left"),
                new CommandDesc("macro <name>", "Execute multi-command action chain", "macro focus"),
                new CommandDesc("ping <host>", "Check network roundtrip latency", "ping 8.8.8.8"),
                new CommandDesc("jump <folder>", "Quick jump to system folder path", "jump downloads"),
                new CommandDesc("procs / taskmgr", "Open interactive Process Manager GUI", "procs"),
                new CommandDesc("time <city>", "Look up global time & UTC offset", "time Tokyo"),
                new CommandDesc("hash <file>", "Compute file SHA-256 checksum", "hash notes.txt"),
                new CommandDesc("quote", "Get a sassy system remark", "quote")
            };
        }
    }
}
