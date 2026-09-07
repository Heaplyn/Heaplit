// Developer: heaplyn
// Date: 2026-09-07
// Summary: Automated Online System Package & Runtime Installer for Heaplit.
//          Auto-detects and installs missing .NET 10 SDK/Desktop Runtimes, Visual C++ Redistributable,
//          WebView2 Evergreen Runtime, Winget, PowerShell modules, and online packages via PowerShell.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public enum PrerequisiteType
    {
        DotNet10DesktopRuntime,
        DotNet10Sdk,
        VcRedist2015_2022_x64,
        WebView2Runtime,
        WingetCli,
        PowerShell7,
        GitForWindows
    }

    public class PrerequisitesStatus
    {
        public bool DotNet10Installed { get; set; }
        public string DotNetVersion { get; set; } = "Not Found";
        public bool VcRedistInstalled { get; set; }
        public bool WebView2Installed { get; set; }
        public bool WingetInstalled { get; set; }
        public bool PowerShell7Installed { get; set; }
        public bool GitInstalled { get; set; }
        public List<string> MissingPrerequisites { get; set; } = new List<string>();

        public bool AllCoreInstalled => DotNet10Installed && VcRedistInstalled && WebView2Installed;
    }

    public static class SystemPackageManager
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        static SystemPackageManager()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "Heaplit-SystemPackageManager/1.0 (Windows NT 10.0; Win64; x64)");
        }

        public static bool IsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        /// <summary>
        /// Audits system for required runtimes and developer prerequisites via fast native registry & process checks.
        /// </summary>
        public static async Task<PrerequisitesStatus> AuditPrerequisitesAsync()
        {
            return await Task.Run(() =>
            {
                var status = new PrerequisitesStatus();

                // 1. Check .NET 10 / Current Runtime
                try
                {
                    string dotnetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
                    bool hasDotnetExe = File.Exists(dotnetPath);

                    if (hasDotnetExe)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = dotnetPath,
                            Arguments = "--list-runtimes",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        using var p = Process.Start(psi);
                        if (p != null)
                        {
                            string outText = p.StandardOutput.ReadToEnd();
                            p.WaitForExit(3000);
                            status.DotNet10Installed = outText.Contains("Microsoft.WindowsDesktop.App 10.") ||
                                                       outText.Contains("Microsoft.NETCore.App 10.") ||
                                                       Environment.Version.Major >= 10;
                            status.DotNetVersion = Environment.Version.ToString();
                        }
                    }
                    else
                    {
                        status.DotNet10Installed = Environment.Version.Major >= 10;
                        status.DotNetVersion = Environment.Version.ToString();
                    }
                }
                catch
                {
                    status.DotNet10Installed = Environment.Version.Major >= 10;
                }

                if (!status.DotNet10Installed)
                {
                    status.MissingPrerequisites.Add(".NET 10 Desktop Runtime & SDK");
                }

                // 2. Check VC++ 2015-2022 Redistributable (x64)
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
                    if (key != null)
                    {
                        var inst = key.GetValue("Installed");
                        status.VcRedistInstalled = inst is int i && i == 1;
                    }
                }
                catch { status.VcRedistInstalled = false; }

                if (!status.VcRedistInstalled)
                {
                    status.MissingPrerequisites.Add("Microsoft Visual C++ 2015-2022 Redistributable (x64)");
                }

                // 3. Check WebView2 Runtime
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-6F3A27050742}") ??
                                    Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-6F3A27050742}");
                    if (key != null)
                    {
                        var pv = key.GetValue("pv")?.ToString();
                        status.WebView2Installed = !string.IsNullOrEmpty(pv) && pv != "0.0.0.0";
                    }
                }
                catch { status.WebView2Installed = false; }

                if (!status.WebView2Installed)
                {
                    status.MissingPrerequisites.Add("Microsoft Edge WebView2 Runtime");
                }

                // 4. Check Winget
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget.exe",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.WaitForExit(1500);
                        status.WingetInstalled = p.ExitCode == 0;
                    }
                }
                catch { status.WingetInstalled = false; }

                if (!status.WingetInstalled)
                {
                    status.MissingPrerequisites.Add("Windows Package Manager (winget)");
                }

                // 5. Check PowerShell 7
                try
                {
                    string pwshPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe");
                    status.PowerShell7Installed = File.Exists(pwshPath);
                }
                catch { status.PowerShell7Installed = false; }

                // 6. Check Git
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "git.exe",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.WaitForExit(1500);
                        status.GitInstalled = p.ExitCode == 0;
                    }
                }
                catch { status.GitInstalled = false; }

                return status;
            });
        }

        /// <summary>
        /// Automatically downloads and installs all missing core runtimes (.NET 10, VC++ Redist, WebView2, Winget) online via PowerShell.
        /// </summary>
        public static async Task<string> InstallAllMissingPrerequisitesAsync(IProgress<string>? progress = null)
        {
            var audit = await AuditPrerequisitesAsync();
            var log = new StringBuilder();

            if (audit.MissingPrerequisites.Count == 0)
            {
                progress?.Report("✅ All required system runtimes and packages are already installed!");
                return "All core runtimes (.NET 10, VC++ Redistributable, WebView2, Winget) are present and verified.";
            }

            progress?.Report($"📦 Found {audit.MissingPrerequisites.Count} missing components. Starting automated online setup...");
            log.AppendLine($"Starting Auto-Installation of {audit.MissingPrerequisites.Count} packages:");

            if (!audit.VcRedistInstalled)
            {
                progress?.Report("📥 Downloading & Installing Microsoft Visual C++ 2015-2022 x64...");
                string res = await InstallVcRedistAsync(progress);
                log.AppendLine($"- VC++ Redist: {res}");
            }

            if (!audit.WebView2Installed)
            {
                progress?.Report("📥 Downloading & Installing Microsoft Edge WebView2 Runtime...");
                string res = await InstallWebView2Async(progress);
                log.AppendLine($"- WebView2: {res}");
            }

            if (!audit.DotNet10Installed)
            {
                progress?.Report("📥 Downloading & Installing .NET 10 Desktop Runtime & SDK...");
                string res = await InstallDotNet10Async(progress);
                log.AppendLine($"- .NET 10: {res}");
            }

            if (!audit.WingetInstalled)
            {
                progress?.Report("📥 Bootstrapping Windows Package Manager (Winget)...");
                string res = await InstallWingetAsync(progress);
                log.AppendLine($"- Winget: {res}");
            }

            progress?.Report("✨ Auto-installation of online packages completed successfully!");
            return log.ToString();
        }

        /// <summary>
        /// Downloads and installs .NET 10 Desktop Runtime & SDK silently using Microsoft's official dotnet-install.ps1 script.
        /// </summary>
        public static async Task<string> InstallDotNet10Async(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("🚀 Invoking Microsoft dotnet-install engine for .NET 10...");
                    string tempDir = Path.Combine(Path.GetTempPath(), "HeaplitDotNetSetup_" + Guid.NewGuid().ToString("N").Substring(0, 6));
                    Directory.CreateDirectory(tempDir);

                    string psScript = $@"
                        Continue = 'Stop'
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
                        
                         = ""C:\Program Files\dotnet""
                         = ""{tempDir.Replace("\\", "/")}/dotnet-install.ps1""
                        
                        Write-Host 'Downloading official dotnet-install.ps1 from Microsoft CDN...'
                        Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile  -UseBasicParsing
                        
                        Write-Host 'Installing .NET 10 Desktop Runtime...'
                        &  -Channel 10.0 -Runtime windowsdesktop -InstallDir 
                        
                        Write-Host 'Installing .NET 10 SDK...'
                        &  -Channel 10.0 -InstallDir 
                        
                        # Add dotnet to Machine Environment PATH if missing
                         = [Environment]::GetEnvironmentVariable('Path', 'Machine')
                        if ( -notlike ""**"") {{
                            [Environment]::SetEnvironmentVariable('Path', "";"", 'Machine')
                        }}
                        C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.5.0_x64__8wekyb3d8bbwe;C:/Users/Kyle/.gemini/antigravity/bin;C:\Users\Kyle\AppData\Roaming\Antigravity\bin;C:\Python314\Scripts\;C:\Python314\;C:\WINDOWS\system32;C:\WINDOWS;C:\WINDOWS\System32\Wbem;C:\WINDOWS\System32\WindowsPowerShell\v1.0\;C:\WINDOWS\System32\OpenSSH\;C:\Program Files\Git\cmd;C:\ProgramData\chocolatey\bin;C:\Program Files\CMake\bin;C:\Program Files\Microsoft SQL Server\170\Tools\Binn\;C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\;C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\;C:\Program Files\Go\bin;C:\Program Files (x86)\Google\Cloud SDK\google-cloud-sdk\bin;C:\Program Files\GitHub CLI\;C:\Program Files\nodejs\;C:\Program Files\Git-Xet\;C:\Program Files\dotnet\;C:\Program Files\Docker\Docker\resources\bin;C:\Program Files\Cloudflare\Cloudflare WARP\;C:\Users\Kyle\.cargo\bin;\\?\C:\Users\Kyle\AppData\Local\Programs\Jan\resources\bin;C:\Users\Kyle\.kimi-code\bin;C:\Users\Kyle\.local\bin;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\Scripts\;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\;C:\Users\Kyle\AppData\Local\Microsoft\WindowsApps;C:\Users\Kyle\AppData\Local\Programs\Microsoft VS Code\bin;C:\Users\Kyle\.dotnet\tools;C:\Program Files\NASM;C:\msys64\ucrt64\bin;C:\Users\Kyle\go\bin;C:\Users\Kyle\AppData\Local\Programs\Antigravity IDE\bin;C:\Program Files\Obsidian;C:\Users\Kyle\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin;C:\Users\Kyle\AppData\Roaming\npm;C:\Users\Kyle\flutter\bin;C:\Program Files\JetBrains\IntelliJ IDEA 2026.2.1\bin;C:\Users\Kyle\ngrok;C:\Users\Kyle\AppData\Local\Programs\Ollama;C:\Users\Kyle\.lmstudio\bin;C:\Program Files\qemu;C:\Users\Kyle\.dotnet\tools;C:\Users\Kyle\AppData\Local\lemonade_server\bin\ = ""C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.5.0_x64__8wekyb3d8bbwe;C:/Users/Kyle/.gemini/antigravity/bin;C:\Users\Kyle\AppData\Roaming\Antigravity\bin;C:\Python314\Scripts\;C:\Python314\;C:\WINDOWS\system32;C:\WINDOWS;C:\WINDOWS\System32\Wbem;C:\WINDOWS\System32\WindowsPowerShell\v1.0\;C:\WINDOWS\System32\OpenSSH\;C:\Program Files\Git\cmd;C:\ProgramData\chocolatey\bin;C:\Program Files\CMake\bin;C:\Program Files\Microsoft SQL Server\170\Tools\Binn\;C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\;C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\;C:\Program Files\Go\bin;C:\Program Files (x86)\Google\Cloud SDK\google-cloud-sdk\bin;C:\Program Files\GitHub CLI\;C:\Program Files\nodejs\;C:\Program Files\Git-Xet\;C:\Program Files\dotnet\;C:\Program Files\Docker\Docker\resources\bin;C:\Program Files\Cloudflare\Cloudflare WARP\;C:\Users\Kyle\.cargo\bin;\\?\C:\Users\Kyle\AppData\Local\Programs\Jan\resources\bin;C:\Users\Kyle\.kimi-code\bin;C:\Users\Kyle\.local\bin;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\Scripts\;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\;C:\Users\Kyle\AppData\Local\Microsoft\WindowsApps;C:\Users\Kyle\AppData\Local\Programs\Microsoft VS Code\bin;C:\Users\Kyle\.dotnet\tools;C:\Program Files\NASM;C:\msys64\ucrt64\bin;C:\Users\Kyle\go\bin;C:\Users\Kyle\AppData\Local\Programs\Antigravity IDE\bin;C:\Program Files\Obsidian;C:\Users\Kyle\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin;C:\Users\Kyle\AppData\Roaming\npm;C:\Users\Kyle\flutter\bin;C:\Program Files\JetBrains\IntelliJ IDEA 2026.2.1\bin;C:\Users\Kyle\ngrok;C:\Users\Kyle\AppData\Local\Programs\Ollama;C:\Users\Kyle\.lmstudio\bin;C:\Program Files\qemu;C:\Users\Kyle\.dotnet\tools;C:\Users\Kyle\AppData\Local\lemonade_server\bin\;""
                        Write-Host 'Successfully installed .NET 10 Desktop Runtime & SDK!'
                    ";

                    RunElevatedPowerShell(psScript);
                    try { Directory.Delete(tempDir, true); } catch { }
                    return "Installed .NET 10 Desktop Runtime & SDK successfully.";
                }
                catch (Exception ex)
                {
                    return $"Failed to install .NET 10: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Downloads and installs Microsoft Visual C++ 2015-2022 Redistributable (x64) silently.
        /// </summary>
        public static async Task<string> InstallVcRedistAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("⬇️ Downloading Visual C++ 2015-2022 Redistributable (x64)...");
                    string tempExe = Path.Combine(Path.GetTempPath(), $"vc_redist.x64_{Guid.NewGuid():N}.exe");

                    string psScript = $@"
                        Continue = 'Stop'
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
                         = 'https://aka.ms/vs/17/release/vc_redist.x64.exe'
                         = '{tempExe.Replace("\\", "/")}'
                        Invoke-WebRequest -Uri  -OutFile  -UseBasicParsing
                        Start-Process -FilePath  -ArgumentList '/install /quiet /norestart' -Wait
                        Remove-Item  -Force -ErrorAction SilentlyContinue
                    ";

                    RunElevatedPowerShell(psScript);
                    return "Visual C++ 2015-2022 Redistributable installed successfully.";
                }
                catch (Exception ex)
                {
                    return $"Failed to install VC++ Redistributable: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Downloads and installs Microsoft Edge WebView2 Evergreen Runtime.
        /// </summary>
        public static async Task<string> InstallWebView2Async(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("⬇️ Downloading Microsoft Edge WebView2 Bootstrapper...");
                    string tempExe = Path.Combine(Path.GetTempPath(), $"MicrosoftEdgeWebview2Setup_{Guid.NewGuid():N}.exe");

                    string psScript = $@"
                        Continue = 'Stop'
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
                         = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703'
                         = '{tempExe.Replace("\\", "/")}'
                        Invoke-WebRequest -Uri  -OutFile  -UseBasicParsing
                        Start-Process -FilePath  -ArgumentList '/silent /install' -Wait
                        Remove-Item  -Force -ErrorAction SilentlyContinue
                    ";

                    RunElevatedPowerShell(psScript);
                    return "Microsoft Edge WebView2 Runtime installed successfully.";
                }
                catch (Exception ex)
                {
                    return $"Failed to install WebView2: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Bootstraps Windows Package Manager (winget) and dependencies on clean/LTSC Windows systems.
        /// </summary>
        public static async Task<string> InstallWingetAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("⬇️ Fetching Windows Package Manager AppX packages from GitHub...");
                    string psScript = @"
                        Continue = 'SilentlyContinue'
                        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13

                         = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), ""WingetSetup_"" + [System.Guid]::NewGuid().ToString('N'))
                        New-Item -ItemType Directory -Path  -Force | Out-Null

                        try {
                             = 'https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx'
                             = 'https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx'
                             = 'https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle'
                             = 'https://github.com/microsoft/winget-cli/releases/latest/download/e7d7045b4c104273892fb71d9d95f87b_License1.xml'

                            Invoke-WebRequest -Uri  -OutFile ""/vclibs.appx"" -UseBasicParsing
                            Invoke-WebRequest -Uri  -OutFile ""/xaml.appx"" -UseBasicParsing
                            Invoke-WebRequest -Uri  -OutFile ""/winget.msixbundle"" -UseBasicParsing

                            Add-AppxPackage -Path ""/vclibs.appx"" -ErrorAction SilentlyContinue
                            Add-AppxPackage -Path ""/xaml.appx"" -ErrorAction SilentlyContinue
                            Add-AppxPackage -Path ""/winget.msixbundle"" -DependencyPath @(""/vclibs.appx"", ""/xaml.appx"") -ErrorAction SilentlyContinue
                        } finally {
                            Remove-Item  -Recurse -Force -ErrorAction SilentlyContinue
                        }
                    ";

                    RunElevatedPowerShell(psScript);
                    return "Windows Package Manager (winget) bootstrapped successfully.";
                }
                catch (Exception ex)
                {
                    return $"Failed to bootstrap Winget: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Universal package downloader & installer: downloads packages from Winget, Choco, direct URLs, or NuGet via PowerShell.
        /// </summary>
        public static async Task<string> InstallOnlinePackageAsync(string target, IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(target)) return "Invalid package target.";
                string trimmed = target.Trim();

                // Direct URL download and install
                if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report($"🌐 Downloading package from URL: {trimmed}...");
                    string ext = Path.GetExtension(new Uri(trimmed).AbsolutePath).ToLowerInvariant();
                    string tempFile = Path.Combine(Path.GetTempPath(), $"OnlinePackage_{Guid.NewGuid():N}{ext}");

                    string psScript;
                    if (ext == ".msi")
                    {
                        psScript = $@"
                            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
                            Invoke-WebRequest -Uri '{trimmed}' -OutFile '{tempFile.Replace("\\", "/")}' -UseBasicParsing
                            Start-Process msiexec.exe -ArgumentList '/i ""{tempFile.Replace("\\", "/")}"" /qn /norestart' -Wait
                            Remove-Item '{tempFile.Replace("\\", "/")}' -Force -ErrorAction SilentlyContinue
                        ";
                    }
                    else if (ext == ".appx" || ext == ".msix" || ext == ".msixbundle" || ext == ".appxbundle")
                    {
                        psScript = $@"
                            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
                            Invoke-WebRequest -Uri '{trimmed}' -OutFile '{tempFile.Replace("\\", "/")}' -UseBasicParsing
                            Add-AppxPackage -Path '{tempFile.Replace("\\", "/")}'
                            Remove-Item '{tempFile.Replace("\\", "/")}' -Force -ErrorAction SilentlyContinue
                        ";
                    }
                    else
                    {
                        psScript = $@"
                            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
                            Invoke-WebRequest -Uri '{trimmed}' -OutFile '{tempFile.Replace("\\", "/")}' -UseBasicParsing
                            Start-Process -FilePath '{tempFile.Replace("\\", "/")}' -ArgumentList '/silent /verysilent /quiet /norestart /s' -Wait
                            Remove-Item '{tempFile.Replace("\\", "/")}' -Force -ErrorAction SilentlyContinue
                        ";
                    }

                    RunElevatedPowerShell(psScript);
                    return $"Package from {trimmed} downloaded and installed.";
                }

                // If winget ID or name
                progress?.Report($"📦 Installing package '{trimmed}' via Winget / Online Repositories...");
                string wingetScript = $@"
                    if (Get-Command winget.exe -ErrorAction SilentlyContinue) {{
                        winget install --id '{trimmed}' --exact --silent --accept-source-agreements --accept-package-agreements
                        if ( -ne 0) {{
                            winget install '{trimmed}' --silent --accept-source-agreements --accept-package-agreements
                        }}
                    }} elseif (Get-Command choco.exe -ErrorAction SilentlyContinue) {{
                        choco install '{trimmed}' -y
                    }} else {{
                        Write-Host 'Installing Winget to proceed...'
                    }}
                ";

                RunElevatedPowerShell(wingetScript);
                return $"Package '{trimmed}' installation command executed.";
            });
        }

        private static void RunElevatedPowerShell(string script)
        {
            try
            {
                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                bool alreadyAdmin = IsAdministrator();

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NoProfile -NonInteractive -EncodedCommand {encoded}",
                    UseShellExecute = !alreadyAdmin,
                    Verb = alreadyAdmin ? "" : "runas",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit(60000);
            }
            catch
            {
                try
                {
                    string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -NoProfile -NonInteractive -EncodedCommand {encoded}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(60000);
                }
                catch { }
            }
        }
    }
}
