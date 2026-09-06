using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class SideloadlyIntegrator
    {
        private static readonly string[] SearchPaths = new[]
        {
            @"C:\Program Files\Sideloadly\Sideloadly.exe",
            @"C:\Program Files (x86)\Sideloadly\Sideloadly.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Sideloadly\Sideloadly.exe")
        };

        public static string GetIpaBundlerPath()
        {
            string toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Tools");
            return Path.Combine(toolsDir, "apptoipa.exe");
        }

        public static void EnsureIpaBundlerDownloaded()
        {
            string exePath = GetIpaBundlerPath();
            if (File.Exists(exePath)) return;

            try
            {
                string dir = Path.GetDirectoryName(exePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                DebugConsoleOverlay.Log("IPABundler", "Downloading IPABundler (apptoipa.exe) from GitHub...");
                using (var client = new HttpClient())
                {
                    var data = client.GetByteArrayAsync("https://github.com/deqline/IPABundler/releases/download/3.0/apptoipa.exe").GetAwaiter().GetResult();
                    File.WriteAllBytes(exePath, data);
                }
                DebugConsoleOverlay.Log("IPABundler", "IPABundler downloaded successfully.");
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("IPABundler", $"Failed to download IPABundler: {ex.Message}");
            }
        }

        public static string? GetSideloadlyPath()
        {
            foreach (var path in SearchPaths)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }

        public static bool IsInstalled => GetSideloadlyPath() != null;

        public static void RunSideload(string ipaPath)
        {
            // Download IPABundler in background when sideloading is requested
            Task.Run(() => EnsureIpaBundlerDownloaded());

            string? execPath = GetSideloadlyPath();
            if (string.IsNullOrEmpty(execPath) || !File.Exists(execPath))
            {
                TriggerDownload();
                return;
            }

            string finalIpa = ipaPath;
            if (Directory.Exists(ipaPath) || (!string.IsNullOrEmpty(ipaPath) && ipaPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase)))
            {
                EnsureIpaBundlerDownloaded();
                string bundlerPath = GetIpaBundlerPath();
                if (File.Exists(bundlerPath))
                {
                    string parentDir = Path.GetDirectoryName(ipaPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                    string appName = Path.GetFileNameWithoutExtension(ipaPath);
                    string targetIpa = Path.Combine(parentDir, $"{appName}.ipa");
                    
                    try
                    {
                        TextOverlay.Show("📦 Converting .app to .ipa using IPABundler...", 3000);
                        var psiBundler = new ProcessStartInfo
                        {
                            FileName = bundlerPath,
                            Arguments = $"\"{ipaPath}\"",
                            WorkingDirectory = parentDir,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var procBundler = Process.Start(psiBundler);
                        procBundler?.WaitForExit();
                        if (File.Exists(targetIpa))
                        {
                            finalIpa = targetIpa;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsoleOverlay.Log("IPABundler", $"Failed to bundle .app using IPABundler: {ex.Message}");
                    }
                }
            }

            if (!File.Exists(finalIpa))
            {
                TextOverlay.Show("⚠️ No compiled IPA found to sideload.", 3000);
                return;
            }

            try
            {
                // Run Sideloadly directly passing the IPA as argument
                var psi = new ProcessStartInfo
                {
                    FileName = execPath,
                    Arguments = $"--ipa=\"{finalIpa}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
                TextOverlay.Show("📲 Launching Sideloadly...", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to start Sideloadly: {ex.Message}", 4000);
            }
        }

        public static void TriggerDownload()
        {
            try
            {
                string args = "/c start cmd /k \"echo Installing Sideloadly via Winget... & winget install -e --id iOSGods.Sideloadly --accept-source-agreements --accept-package-agreements & echo. & echo Done! Press any key to close. & pause > null\"";
                Process.Start("cmd.exe", args);
                TextOverlay.Show("📲 Initializing Sideloadly installation via Winget...", 3500);
            }
            catch
            {
                try
                {
                    // Open default browser to Sideloadly download page
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://sideloadly.io/index.html",
                        UseShellExecute = true
                    });
                    TextOverlay.Show("🌐 Opening Sideloadly Download Page...", 3000);
                }
                catch { }
            }
        }
    }
}
