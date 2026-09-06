// Developer: heaplyn
// Date: 2026-08-19
// Summary: Universal Developer & Offline Suite Manager.
//          Orchestrates installation and verification of languages, package managers, and game engines.
//          Primary engine: Windows Package Manager (winget).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class DevToolInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string WingetId { get; set; } = "";
        public bool IsInstalled { get; set; } = false;
        public string Version { get; set; } = "Unknown";
    }

    public static class DevSuiteManager
    {
        private static readonly List<DevToolInfo> _tools = new();

        static DevSuiteManager()
        {
            InitializeToolList();
        }

        private static void InitializeToolList()
        {
            // --- LANGUAGES ---
            AddTool("Python", "Python 3.12", "Language", "Python Programming Language", "Python.Python.3.12");
            AddTool("NodeJS", "Node.js (LTS)", "Language", "JavaScript Runtime", "OpenJS.NodeJS.LTS");
            AddTool("Rust", "Rust (rustup)", "Language", "Rust Programming Language", "Rustlang.Rustup");
            AddTool("Go", "Go Programming Language", "Language", "Go Programming Language", "Google.Go");
            AddTool("OpenJDK", "OpenJDK 21", "Language", "Java Development Kit", "RedHat.OpenJDK.21");
            AddTool("DotnetSDK", ".NET 8 SDK", "Language", "C#, F#, and VB.NET development", "Microsoft.DotNet.SDK.8");
            AddTool("CppBuildTools", "C++ Build Tools", "Language", "MSVC, CMake, and Windows SDK", "Microsoft.VisualStudio.2022.BuildTools");
            AddTool("LLVM", "LLVM / Clang", "Language", "C/C++ compiler and toolchain", "LLVM.LLVM");
            AddTool("Mingw", "MinGW-w64", "Language", "GCC for Windows", "msys2.mingw.w64");
            AddTool("NASM", "NASM Assembly", "Language", "Netwide Assembler", "NASM.NASM");

            // --- PACKAGE MANAGERS ---
            AddTool("Choco", "Chocolatey", "Package Manager", "The Windows Package Manager", "Chocolatey.Chocolatey");
            AddTool("Scoop", "Scoop", "Package Manager", "A command-line installer for Windows", "Scoop.Scoop");

            // --- GAME ENGINES ---
            AddTool("Unity", "Unity Hub", "Game Engine", "Unity Game Engine Management", "Unity.UnityHub");
            AddTool("Epic", "Epic Games Launcher", "Game Engine", "Epic Games & Unreal Engine", "EpicGames.EpicGamesLauncher");
            AddTool("Godot", "Godot Engine", "Game Engine", "Free, open-source 2D/3D engine", "GodotEngine.GodotEngine");
            AddTool("Roblox", "Roblox Studio", "Game Engine", "Creation tool for Roblox", "Roblox.RobloxStudio");
            AddTool("Lumberyard", "Open 3D Engine", "Game Engine", "Successor to Amazon Lumberyard", "Open3DEngine.O3DE");

            // --- IDEs & EDITORS ---
            AddTool("VSCode", "VS Code", "IDE", "Visual Studio Code", "Microsoft.VisualStudioCode");
            AddTool("VisualStudio", "Visual Studio Community", "IDE", "Full C# / C++ IDE", "Microsoft.VisualStudio.2022.Community");
            AddTool("SublimeText", "Sublime Text", "IDE", "Sophisticated text editor", "SublimeHQ.SublimeText.4");
            AddTool("JetBrainsToolbox", "JetBrains Toolbox", "IDE", "Manage IntelliJ, PyCharm, ReSharper", "JetBrains.Toolbox");
            AddTool("PyCharm", "PyCharm Community", "IDE", "Python IDE", "JetBrains.PyCharm.Community");
            AddTool("IntelliJ", "IntelliJ IDEA Community", "IDE", "Java/Kotlin IDE", "JetBrains.IntelliJIDEA.Community");
            AddTool("Vim", "Vim", "IDE", "The ubiquitous text editor", "vim.vim");

            // --- DATABASE & SQL ---
            AddTool("PostgreSQL", "PostgreSQL 16", "Database", "Relational database system", "PostgreSQL.PostgreSQL.16");
            AddTool("MySQL", "MySQL Community Server", "Database", "The world's most popular open source database", "Oracle.MySQL");
            AddTool("SQLite", "SQLite Tools", "Database", "Command-line shell for SQLite", "SQLite.SQLite");
            AddTool("MongoDB", "MongoDB Community", "Database", "NoSQL document database", "MongoDB.Server");
            AddTool("AzureDataStudio", "Azure Data Studio", "Database", "Data management tool for SQL Server", "Microsoft.AzureDataStudio");
            AddTool("HeidiSQL", "MySQL / MariaDB / SQL Server", "Database", "Lightweight SQL editor", "AnsgarBecker.HeidiSQL");
            AddTool("DBeaver", "DBeaver Community", "Database", "Universal database tool", "dbeaver.dbeaver");
            AddTool("Redis", "Redis for Windows", "Database", "In-memory data structure store", "Microsoft.OpenTech.Redis");

            // --- BROWSERS ---
            AddTool("Chrome", "Google Chrome", "Browser", "Fast, secure, and free browser", "Google.Chrome");
            AddTool("Firefox", "Mozilla Firefox", "Browser", "Privacy-focused browser", "Mozilla.Firefox");
            AddTool("Brave", "Brave Browser", "Browser", "Privacy-focused ad-blocking browser", "Brave.Brave");

            // --- SYSTEM & VIRTUALIZATION ---
            AddTool("VirtualBox", "Oracle VirtualBox", "Virtualization", "X86 and AMD64/Intel64 virtualization", "Oracle.VirtualBox");
            AddTool("VMwarePlayer", "VMware Workstation Player", "Virtualization", "Local desktop virtualization", "VMware.WorkstationPlayer");
            AddTool("Putty", "PuTTY", "Utility", "SSH and telnet client", "PuTTY.PuTTY");
            AddTool("WinSCP", "WinSCP", "Utility", "SFTP and FTP client", "WinSCP.WinSCP");
            AddTool("Steam", "Steam", "Gaming", "Digital distribution platform by Valve", "Valve.Steam");
            AddTool("EpicGames", "Epic Games Launcher", "Gaming", "Epic Games store and Unreal Engine", "EpicGames.EpicGamesLauncher");
            AddTool("Zoom", "Zoom", "Chat", "Video conferencing and meetings", "Zoom.Zoom");
            AddTool("Teams", "Microsoft Teams", "Chat", "Collaboration and communication", "Microsoft.Teams");
            AddTool("PowerShell7", "PowerShell 7", "Tool", "Cross-platform shell and scripting", "Microsoft.PowerShell");
            AddTool("Python3", "Python 3.12", "Language", "Latest Python 3 environment", "Python.Python.3.12");
        }

        private static void AddTool(string id, string name, string cat, string desc, string winget)
        {
            _tools.Add(new DevToolInfo { Id = id, Name = name, Category = cat, Description = desc, WingetId = winget });
        }

        public static List<DevToolInfo> GetAllTools() => _tools;

        public static async Task RefreshInstallationStatusAsync()
        {
            try
            {
                // Bulk check via one winget command to avoid massive lag
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return;
                string output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                foreach (var tool in _tools)
                {
                    tool.IsInstalled = output.Contains(tool.WingetId, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                // Fallback to slower individual checks if bulk fails
                foreach (var tool in _tools)
                {
                    tool.IsInstalled = await CheckIfInstalledAsync(tool.WingetId);
                }
            }
        }

        public static void InstallAllMissing()
        {
            var missing = _tools.Where(t => !t.IsInstalled).ToList();
            if (!missing.Any()) { TextOverlay.Show("All tools in suite are already installed!", 3000); return; }

            TextOverlay.Show($"📥 Batch-installing {missing.Count} tools in background...", 5000);

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("echo [SYSTEM] Starting massive batch installation of developer tools...");
            foreach (var tool in missing)
            {
                sb.AppendLine($"echo [INSTALL] {tool.Name} ({tool.WingetId})...");
                sb.AppendLine($"winget install --id {tool.WingetId} --silent --accept-source-agreements --accept-package-agreements");
            }
            sb.AppendLine("echo [COMPLETE] All requested tools have been queued for installation.");
            sb.AppendLine("pause");

            string tempBat = Path.Combine(Path.GetTempPath(), "heaplit_batch_install.bat");
            File.WriteAllText(tempBat, sb.ToString());

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{tempBat}\"",
                CreateNoWindow = false,
                UseShellExecute = true
            });
        }

        public static async Task<bool> CheckIfInstalledAsync(string wingetId)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"list --id {wingetId}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                string output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();
                return output.Contains(wingetId);
            }
            catch { return false; }
        }

        public static void InstallTool(string wingetId)
        {
            TextOverlay.Show($"📥 Initiating installation of {wingetId}...", 4000);
            Process.Start("cmd.exe", $"/c start cmd /k \"echo Installing {wingetId} via Winget... & winget install --id {wingetId} --silent --accept-source-agreements --accept-package-agreements & echo Installation triggered! & pause\"");
        }

        public static void UninstallTool(string wingetId)
        {
            TextOverlay.Show($"🗑️ Initiating uninstallation of {wingetId}...", 4000);
            Process.Start("cmd.exe", $"/c start cmd /k \"echo Uninstalling {wingetId}... & winget uninstall --id {wingetId} & echo Uninstall triggered! & pause\"");
        }

        public static async Task<string> RunGenericCommandAsync(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{cmd}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return "Failed to start process.";
                string outStr = await proc.StandardOutput.ReadToEndAsync();
                string errStr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();
                return string.IsNullOrWhiteSpace(outStr) ? errStr : outStr;
            }
            catch (Exception ex) { return $"Error: {ex.Message}"; }
        }
    }
}
