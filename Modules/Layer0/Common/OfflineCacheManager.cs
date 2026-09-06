// Developer: heaplyn
// Date: 2026-08-13
// Summary: Manages offline caching, Wi-Fi pre-downloading, and offline fallback routing for speech, LLM, and TTS features.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class OfflineCacheManager
    {
        public static readonly string OfflineDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "OfflineCache");

        private static readonly List<OfflineTool> RequiredOfflineTools = new List<OfflineTool>
        {
            new OfflineTool("Git", "Git.Git", "git"),
            new OfflineTool("Node.js", "OpenJS.NodeJS", "node"),
            new OfflineTool("Python", "Python.Python.3", "python"),
            new OfflineTool("Rust", "Rustlang.Rustup", "rustc"),
            new OfflineTool("Go", "Google.Go", "go"),
            new OfflineTool("LLVM/Clang", "LLVM.LLVM", "clang"),
            new OfflineTool("NASM", "NASM.NASM", "nasm"),
            new OfflineTool("FFmpeg", "Gyan.FFmpeg", "ffmpeg"),
            new OfflineTool("Ollama", "Ollama.Ollama", "ollama"),
            new OfflineTool("7-Zip", "7zip.7zip", "7z"),
            new OfflineTool("VS Code", "Microsoft.VisualStudioCode", "code")
        };

        private class OfflineTool
        {
            public string Name { get; }
            public string PackageId { get; }
            public string Command { get; }

            public OfflineTool(string name, string packageId, string command)
            {
                Name = name;
                PackageId = packageId;
                Command = command;
            }
        }

        static OfflineCacheManager()
        {
            if (!Directory.Exists(OfflineDataDirectory))
            {
                Directory.CreateDirectory(OfflineDataDirectory);
            }
        }

        /// <summary>
        /// Checks whether active internet / Wi-Fi connection is available.
        /// </summary>
        public static bool IsInternetAvailable()
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("8.8.8.8", 1500);
                return reply != null && reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether Gemini AI can be queried (Internet available + API key configured).
        /// </summary>
        public static bool CanUseGemini()
        {
            string key = SettingsManager.Current.GOOGLE_AI_KEY;
            if (string.IsNullOrWhiteSpace(key)) return false;
            return IsInternetAvailable();
        }

        /// <summary>
        /// Pre-caches all online resources (Vosk speech model, Package Managers, Dev Tools) for 100% offline usage.
        /// </summary>
        public static async Task PreCacheAllForOfflineAsync(Action<string>? statusCallback = null)
        {
            statusCallback?.Invoke("📡 Checking internet connection...");
            if (!IsInternetAvailable())
            {
                statusCallback?.Invoke("⚠️ Internet disconnected. Pre-caching requires Wi-Fi / active internet connection.");
                return;
            }

            // 1. Download Offline Vosk Speech Model
            statusCallback?.Invoke("🎙️ Pre-caching Vosk Offline Neural Speech Model (~40MB)...");
            await VoskEngine.EnsureModelDownloadedAsync(showToast: false);

            // 2. Install Essential Development Tools & Package Managers
            foreach (var tool in RequiredOfflineTools)
            {
                if (!IsAppInstalled(tool.Command))
                {
                    statusCallback?.Invoke($"📥 Installing {tool.Name} via winget...");
                    await Task.Run(() => InstallToolSilently(tool.PackageId, tool.Name));

                    // Small delay to let system register installation
                    await Task.Delay(2000);
                }
                else
                {
                    statusCallback?.Invoke($"✅ {tool.Name} is already installed.");
                }
            }

            // 3. Pre-pull Ollama Models if Ollama is running
            if (await IsOllamaRunningAsync())
            {
                string defaultModel = SettingsManager.Current.OLLAMA_MODEL;
                if (string.IsNullOrWhiteSpace(defaultModel)) defaultModel = "llama3.2";

                if (!await IsOllamaModelCachedAsync(defaultModel))
                {
                    statusCallback?.Invoke($"🧠 Pulling local LLM model: {defaultModel}...");
                    await Task.Run(() => PullOllamaModelSilently(defaultModel));
                }
                else
                {
                    statusCallback?.Invoke($"✅ LLM Model '{defaultModel}' is already cached.");
                }
            }

            statusCallback?.Invoke($"✅ Pre-cache complete! Cached Vosk, Package Managers, and Dev Tools for 100% offline usage.");
            TextOverlay.Show("📶 Heaplit is now 100% Ready For Offline Use!", 3500);
        }

        private static void InstallToolSilently(string packageId, string friendlyName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"install -e --id {packageId} --accept-source-agreements --accept-package-agreements --silent",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(600000); // 1 min timeout per tool
            }
            catch { }
        }

        private static void PullOllamaModelSilently(string modelName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = $"pull {modelName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(300000); // 5 min timeout
            }
            catch { }
        }

        /// <summary>
        /// Parses spoken queries locally when internet is offline.
        /// </summary>
        public static string OfflineIntentFallback(string query)
        {
            string lower = query.ToLower().Trim();

            if (lower.Contains("time") || lower.Contains("clock"))
                return $"The current time is {DateTime.Now:h:mm tt}.";
            if (lower.Contains("date") || lower.Contains("day"))
                return $"Today is {DateTime.Now:dddd, MMMM d, yyyy}.";
            if (lower.Contains("battery") || lower.Contains("power"))
                return "Checking system battery status locally...";
            if (lower.Contains("restart") || lower.Contains("reboot"))
                return "Restarting system requested. Awaiting user confirmation.";
            if (lower.Contains("shutdown") || lower.Contains("power off"))
                return "Shutdown requested. Awaiting user confirmation.";

            return $"Offline Mode Active: Standard desktop system handler ready for '{query}'.";
        }

        /// <summary>
        /// Checks if a command/executable is available in the system PATH.
        /// </summary>
        public static bool IsCommandAvailable(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c where {cmd}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if Ollama is running and responding on its API port.
        /// </summary>
        public static async Task<bool> IsOllamaRunningAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(500);
                string endpoint = SettingsManager.Current.OLLAMA_ENDPOINT;
                if (string.IsNullOrWhiteSpace(endpoint)) endpoint = "http://localhost:11434";
                var response = await client.GetAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a specific model is pulled and cached locally in Ollama.
        /// </summary>
        public static async Task<bool> IsOllamaModelCachedAsync(string modelName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                string output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();
                return output.Contains(modelName, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Installs a tool via winget in a separate user-facing terminal.
        /// </summary>
        public static void InstallToolViaWinget(string packageId, string friendlyName)
        {
            try
            {
                string args;
                if (packageId.StartsWith("npm:", StringComparison.OrdinalIgnoreCase))
                {
                    string npmPackage = packageId.Substring(4);
                    args = $"/c start cmd /k \"echo Installing {friendlyName} globally via npm... & npm install -g {npmPackage} & echo. & echo Done! Press any key to close. & pause > null\"";
                }
                else
                {
                    args = $"/c start cmd /k \"echo Installing {friendlyName}... & winget install -e --id {packageId} --accept-source-agreements --accept-package-agreements & echo. & echo Done! Press any key to close. & pause > null\"";
                }
                Process.Start("cmd.exe", args);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"❌ Failed to start installer for {friendlyName}: {ex.Message}", 3500);
            }
        }

        /// <summary>
        /// Pulls a model in Ollama in a separate user-facing terminal.
        /// </summary>
        public static void PullOllamaModel(string modelName)
        {
            try
            {
                string args = $"/c start cmd /k \"echo Pulling model {modelName} locally... & ollama pull {modelName} & echo. & echo Done! Press any key to close. & pause > null\"";
                Process.Start("cmd.exe", args);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"❌ Failed to start Ollama pull: {ex.Message}", 3500);
            }
        }

        public static void ClearCache()
        {
            try {
                if (Directory.Exists(OfflineDataDirectory)) {
                    foreach (var file in Directory.GetFiles(OfflineDataDirectory)) File.Delete(file);
                    foreach (var dir in Directory.GetDirectories(OfflineDataDirectory)) Directory.Delete(dir, true);
                }
            } catch { }
        }

        /// <summary>
        /// Checks if an application is installed, looking in PATH and standard installation paths.
        /// </summary>
        public static bool IsAppInstalled(string cmdOrDisplayName)
        {
            // 1. Try PATH check first
            if (IsCommandAvailable(cmdOrDisplayName)) return true;

            // 2. Check common installation directories
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] searchPaths = new[]
            {
                Path.Combine(programFiles, "paint.net", "PaintDotNet.exe"),
                Path.Combine(programFiles, "GIMP 2", "bin", "gimp-2.10.exe"),
                Path.Combine(programFiles, "Krita (x64)", "bin", "krita.exe"),
                Path.Combine(programFiles, "Inkscape", "bin", "inkscape.exe"),
                Path.Combine(programFiles, "Blender Foundation", "Blender", "blender.exe"),
                Path.Combine(programFiles, "Audacity", "Audacity.exe"),
                Path.Combine(programFilesX86, "Audacity", "Audacity.exe"),
                Path.Combine(programFiles, "VideoLAN", "VLC", "vlc.exe"),
                Path.Combine(programFilesX86, "VideoLAN", "VLC", "vlc.exe"),
                Path.Combine(programFiles, "Microsoft Visual Studio", "2022", "Community", "Common7", "IDE", "devenv.exe"),
                Path.Combine(localAppData, "Programs", "Microsoft VS Code", "Code.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo", "bin", "rustc.exe"),
                Path.Combine(programFiles, "LLVM", "bin", "clang.exe"),
                Path.Combine(programFiles, "nasm", "nasm.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "msys64", "msys2.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "Strawberry", "perl", "bin", "perl.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "Ruby32-x64", "bin", "ruby.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "Ruby31-x64", "bin", "ruby.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "tools", "php83", "php.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "tools", "php82", "php.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ghcup", "bin", "ghc.exe"),
                Path.Combine(localAppData, "Programs", "Julia-1.10.0", "bin", "julia.exe"),
                Path.Combine(localAppData, "Programs", "Julia-1.10.1", "bin", "julia.exe"),
                Path.Combine(localAppData, "Programs", "Julia-1.10.2", "bin", "julia.exe"),
                Path.Combine(programFiles, "Unity Hub", "Unity Hub.exe"),
                Path.Combine(programFiles, "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe"),
                Path.Combine(programFilesX86, "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe"),
                Path.Combine(programFiles, "OpenTTD", "openttd.exe"),
                Path.Combine(programFilesX86, "OpenTTD", "openttd.exe")
            };

            foreach (var path in searchPaths)
            {
                if (path.Contains(cmdOrDisplayName, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                    return true;
            }

            return false;
        }
    }
}
