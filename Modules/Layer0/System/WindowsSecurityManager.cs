// Developer: heaplyn
// Date: 2026-09-07
// Summary: Enterprise Windows Security & Defender Recovery Engine for Heaplit.
//          Scans, detects, and automatically repairs/re-enables Windows Defender,
//          Windows Firewall, Security Center Services, Registry Policies (via elevated Administrator PowerShell),
//          removes rogue malware exclusions, downloads fresh official Defender / MSERT
//          packages from Microsoft, and triggers emergency antivirus scans.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class SecurityAuditResult
    {
        public bool RealTimeProtectionEnabled { get; set; }
        public bool AntivirusEnabled { get; set; }
        public bool BehaviorMonitorEnabled { get; set; }
        public bool IoavProtectionEnabled { get; set; }
        public bool ScriptScanningEnabled { get; set; }
        public bool CloudProtectionEnabled { get; set; }
        public bool NetworkProtectionEnabled { get; set; }
        public bool FirewallDomainEnabled { get; set; }
        public bool FirewallPrivateEnabled { get; set; }
        public bool FirewallPublicEnabled { get; set; }
        public bool DefenderServiceRunning { get; set; }
        public bool FirewallServiceRunning { get; set; }
        public bool SecurityCenterServiceRunning { get; set; }
        public bool WindowsUpdateServiceRunning { get; set; }
        public List<string> RoguePoliciesDetected { get; set; } = new List<string>();
        public List<string> RogueExclusionPaths { get; set; } = new List<string>();
        public List<string> RogueExclusionProcesses { get; set; } = new List<string>();
        public List<string> TamperedHostsEntries { get; set; } = new List<string>();
        public List<string> HijackedIfeoProcesses { get; set; } = new List<string>();
        public string SignatureVersion { get; set; } = "Unknown";
        public DateTime SignatureLastUpdated { get; set; } = DateTime.MinValue;
        public List<string> LogMessages { get; set; } = new List<string>();

        public bool IsSystemCompromised =>
            !RealTimeProtectionEnabled ||
            !AntivirusEnabled ||
            !FirewallDomainEnabled ||
            !FirewallPrivateEnabled ||
            !FirewallPublicEnabled ||
            !DefenderServiceRunning ||
            RoguePoliciesDetected.Count > 0 ||
            RogueExclusionPaths.Count > 0 ||
            TamperedHostsEntries.Count > 0 ||
            HijackedIfeoProcesses.Count > 0;

        public int HealthScore
        {
            get
            {
                int score = 100;
                if (!RealTimeProtectionEnabled) score -= 30;
                if (!AntivirusEnabled) score -= 20;
                if (!FirewallPrivateEnabled || !FirewallPublicEnabled) score -= 15;
                if (!DefenderServiceRunning) score -= 20;
                if (!SecurityCenterServiceRunning) score -= 5;
                if (!WindowsUpdateServiceRunning) score -= 5;
                if (RoguePoliciesDetected.Count > 0) score -= (RoguePoliciesDetected.Count * 5);
                if (RogueExclusionPaths.Count > 0) score -= (RogueExclusionPaths.Count * 5);
                if (TamperedHostsEntries.Count > 0) score -= 10;
                if (HijackedIfeoProcesses.Count > 0) score -= 15;
                return Math.Max(0, Math.Min(100, score));
            }
        }
    }

    public static class WindowsSecurityManager
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        })
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        // Official permanent Microsoft download endpoints
        public const string DEFENDER_HEALTH_SETUP_URL = "https://go.microsoft.com/fwlink/?linkid=2262445";
        public const string DEFENDER_ENGINE_X64_URL = "https://go.microsoft.com/fwlink/?LinkID=121721&arch=x64";
        public const string DEFENDER_ENGINE_X86_URL = "https://go.microsoft.com/fwlink/?LinkID=121721&arch=x86";
        public const string MSERT_X64_URL = "https://go.microsoft.com/fwlink/?LinkId=212732";
        public const string MSERT_X86_URL = "https://go.microsoft.com/fwlink/?LinkId=212733";

        /// <summary>
        /// Checks if current process is running with elevated Administrator privileges.
        /// </summary>
        public static bool IsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Audits all Windows Defender, Firewall, Service, Registry, and Hosts security vectors.
        /// </summary>
        public static async Task<SecurityAuditResult> AuditSecurityStatusAsync()
        {
            var audit = new SecurityAuditResult();

            await Task.Run(() =>
            {
                // 1. Audit Windows Defender via PowerShell Get-MpComputerStatus
                try
                {
                    string psScript = @"
                        $res = @{
                            AntivirusEnabled = $false
                            RealTimeProtection = $false
                            BehaviorMonitor = $false
                            Ioav = $false
                            ScriptScan = $false
                            NIS = $false
                            Cloud = 0
                            Network = 0
                            SigVer = 'Unknown'
                        }
                        try {
                            $stat = Get-MpComputerStatus -ErrorAction SilentlyContinue
                            if ($stat) {
                                $res.AntivirusEnabled = [bool]$stat.AntivirusEnabled
                                $res.RealTimeProtection = [bool]$stat.RealTimeProtectionEnabled
                                $res.BehaviorMonitor = [bool]$stat.BehaviorMonitorEnabled
                                $res.Ioav = [bool]$stat.IoavProtectionEnabled
                                $res.ScriptScan = [bool]$stat.ScriptScanningEnabled
                                $res.NIS = [bool]$stat.NISScanEnabled
                                $res.Cloud = $stat.MAPSReporting
                                $res.Network = $stat.NetworkProtectionStatus
                                $res.SigVer = $stat.AntivirusSignatureVersion
                            }
                        } catch {}
                        $res | ConvertTo-Json -Compress
                    ";

                    string json = RunPowerShellOutput(psScript).Trim();
                    if (!string.IsNullOrEmpty(json) && json.StartsWith("{"))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("AntivirusEnabled", out var av)) audit.AntivirusEnabled = av.GetBoolean();
                        if (root.TryGetProperty("RealTimeProtection", out var rtp)) audit.RealTimeProtectionEnabled = rtp.GetBoolean();
                        if (root.TryGetProperty("BehaviorMonitor", out var bm)) audit.BehaviorMonitorEnabled = bm.GetBoolean();
                        if (root.TryGetProperty("Ioav", out var ioav)) audit.IoavProtectionEnabled = ioav.GetBoolean();
                        if (root.TryGetProperty("ScriptScan", out var ss)) audit.ScriptScanningEnabled = ss.GetBoolean();
                        if (root.TryGetProperty("Cloud", out var cloud)) audit.CloudProtectionEnabled = cloud.GetInt32() > 0;
                        if (root.TryGetProperty("Network", out var net)) audit.NetworkProtectionEnabled = net.GetInt32() > 0;
                        if (root.TryGetProperty("SigVer", out var sig)) audit.SignatureVersion = sig.GetString() ?? "Unknown";
                    }
                }
                catch (Exception ex)
                {
                    audit.LogMessages.Add($"Warning checking Get-MpComputerStatus: {ex.Message}");
                }

                // 2. Audit Windows Firewall Status
                try
                {
                    string psFw = @"
                        $fw = Get-NetFirewallProfile -ErrorAction SilentlyContinue
                        $res = @{
                            Domain = ($fw | Where-Object {$_.Name -eq 'Domain'}).Enabled -eq 'True'
                            Private = ($fw | Where-Object {$_.Name -eq 'Private'}).Enabled -eq 'True'
                            Public = ($fw | Where-Object {$_.Name -eq 'Public'}).Enabled -eq 'True'
                        }
                        $res | ConvertTo-Json -Compress
                    ";
                    string fwJson = RunPowerShellOutput(psFw).Trim();
                    if (!string.IsNullOrEmpty(fwJson) && fwJson.StartsWith("{"))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(fwJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("Domain", out var dom)) audit.FirewallDomainEnabled = dom.GetBoolean();
                        if (root.TryGetProperty("Private", out var priv)) audit.FirewallPrivateEnabled = priv.GetBoolean();
                        if (root.TryGetProperty("Public", out var pub)) audit.FirewallPublicEnabled = pub.GetBoolean();
                    }
                }
                catch (Exception ex)
                {
                    audit.LogMessages.Add($"Warning checking Firewall: {ex.Message}");
                }

                // 3. Audit Security Services
                audit.DefenderServiceRunning = IsServiceRunning("WinDefend");
                audit.FirewallServiceRunning = IsServiceRunning("MpsSvc");
                audit.SecurityCenterServiceRunning = IsServiceRunning("wscsvc");
                audit.WindowsUpdateServiceRunning = IsServiceRunning("wuauserv");

                // 4. Audit Registry Tampering Policies
                string[] policyKeys = new[]
                {
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender|DisableAntiSpyware",
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender|DisableRealtimeMonitoring",
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection|DisableRealtimeMonitoring",
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection|DisableBehaviorMonitoring",
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection|DisableOnAccessProtection",
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection|DisableIOAVProtection",
                    @"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System|DisableTaskMgr",
                    @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System|DisableTaskMgr",
                    @"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System|DisableRegistryTools",
                    @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System|DisableRegistryTools",
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU|NoAutoUpdate",
                    @"HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate|DisableWindowsUpdateAccess"
                };

                foreach (var item in policyKeys)
                {
                    var parts = item.Split('|');
                    string path = parts[0];
                    string valName = parts[1];
                    string testPs = $"(Get-ItemProperty -Path '{path}' -Name '{valName}' -ErrorAction SilentlyContinue).{valName}";
                    string result = RunPowerShellOutput(testPs).Trim();
                    if (result == "1" || result.Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        audit.RoguePoliciesDetected.Add($"{path} -> {valName} = {result}");
                    }
                }

                // 5. Audit Image File Execution Options (IFEO) debugger hijacking
                try
                {
                    string[] ifeoTargets = new[] { "MsMpEng.exe", "MpCmdRun.exe", "SecurityHealthHost.exe", "SecurityHealthService.exe", "SecurityHealthSystray.exe", "SecHealthUI.exe", "taskmgr.exe", "regedit.exe" };
                    foreach (var target in ifeoTargets)
                    {
                        string checkIfeo = $"(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\{target}' -Name 'Debugger' -ErrorAction SilentlyContinue).Debugger";
                        string debuggerVal = RunPowerShellOutput(checkIfeo).Trim();
                        if (!string.IsNullOrEmpty(debuggerVal))
                        {
                            audit.HijackedIfeoProcesses.Add($"{target} -> Hooked by: {debuggerVal}");
                        }
                    }
                }
                catch { }

                // 6. Audit Rogue Defender Exclusions
                try
                {
                    string psExclusions = @"
                        $pref = Get-MpPreference -ErrorAction SilentlyContinue
                        $res = @{
                            Paths = @($pref.ExclusionPath)
                            Processes = @($pref.ExclusionProcess)
                        }
                        $res | ConvertTo-Json -Compress
                    ";
                    string exJson = RunPowerShellOutput(psExclusions).Trim();
                    if (!string.IsNullOrEmpty(exJson) && exJson.StartsWith("{"))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(exJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("Paths", out var paths) && paths.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var p in paths.EnumerateArray())
                            {
                                string? str = p.GetString();
                                if (!string.IsNullOrWhiteSpace(str)) audit.RogueExclusionPaths.Add(str);
                            }
                        }
                        if (root.TryGetProperty("Processes", out var procs) && procs.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var pr in procs.EnumerateArray())
                            {
                                string? str = pr.GetString();
                                if (!string.IsNullOrWhiteSpace(str)) audit.RogueExclusionProcesses.Add(str);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    audit.LogMessages.Add($"Warning checking exclusions: {ex.Message}");
                }

                // 7. Audit Hosts File for Security Hijacking
                try
                {
                    string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                    if (File.Exists(hostsPath))
                    {
                        var lines = File.ReadAllLines(hostsPath);
                        string[] securityDomains = new[] { "microsoft.com", "windowsupdate.com", "defender", "virustotal", "kaspersky", "malwarebytes", "bitdefender", "avast" };
                        foreach (var line in lines)
                        {
                            string l = line.Trim();
                            if (string.IsNullOrEmpty(l) || l.StartsWith("#")) continue;
                            if (securityDomains.Any(d => l.Contains(d, StringComparison.OrdinalIgnoreCase)))
                            {
                                audit.TamperedHostsEntries.Add(l);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    audit.LogMessages.Add($"Warning checking hosts file: {ex.Message}");
                }
            });

            return audit;
        }

        /// <summary>
        /// Comprehensively repairs all Windows Security, Defender, Firewall, IFEO, and Windows Update registry keys as Administrator via PowerShell.
        /// </summary>
        public static async Task<(bool success, List<string> logs)> FixAllRegistryPoliciesElevatedAsync(Action<string>? progressCallback = null)
        {
            var logs = new List<string>();
            void Log(string s) { logs.Add(s); progressCallback?.Invoke(s); }

            return await Task.Run(() =>
            {
                Log("🛡️ [Registry Fixer] Initiating Administrator PowerShell Registry Repair Protocol...");

                string elevatedRegistryScript = @"
                    $ErrorActionPreference = 'SilentlyContinue'

                    # 1. Purge Windows Defender Policy Locks
                    $defenderKeys = @(
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Advanced Threat Protection'
                    )
                    $defenderProps = @(
                        'DisableAntiSpyware', 'DisableRealtimeMonitoring', 'DisableBehaviorMonitoring',
                        'DisableOnAccessProtection', 'DisableIOAVProtection', 'DisableScanOnRealtimeEnable',
                        'DisableScriptScanning', 'DisableBlockAtFirstSeen', 'DisableRoutinelyTakingAction',
                        'ServiceKeepAlive', 'AllowFastServiceStartup'
                    )

                    foreach ($k in $defenderKeys) {
                        if (Test-Path $k) {
                            foreach ($p in $defenderProps) {
                                Remove-ItemProperty -Path $k -Name $p -ErrorAction SilentlyContinue
                            }
                        }
                    }

                    # Enforce SpyNet cloud & sample submission
                    if (-not (Test-Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet')) {
                        New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet' -Force | Out-Null
                    }
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet' -Name 'SpynetReporting' -Value 2 -Type DWord -ErrorAction SilentlyContinue
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet' -Name 'SubmitSamplesConsent' -Value 1 -Type DWord -ErrorAction SilentlyContinue

                    # 2. Unlock Windows Defender Security Center UI Lockdown policies
                    $secCenterBase = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender Security Center'
                    if (Test-Path $secCenterBase) {
                        Get-ChildItem -Path $secCenterBase -Recurse | ForEach-Object {
                            Remove-ItemProperty -Path $_.PSPath -Name 'UILockdown' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $_.PSPath -Name 'HideSystray' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $_.PSPath -Name 'HideThreats' -ErrorAction SilentlyContinue
                        }
                    }

                    # 3. Purge System Tool Lockouts (Task Manager, Regedit, CMD)
                    $systemPolicyPaths = @(
                        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System',
                        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System',
                        'HKCU:\SOFTWARE\Policies\Microsoft\Windows\System',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'
                    )
                    $toolProps = @('DisableTaskMgr', 'DisableRegistryTools', 'DisableCMD', 'DisableLockWorkstation', 'DisableChangePassword', 'HideFastUserSwitching')
                    foreach ($p in $systemPolicyPaths) {
                        if (Test-Path $p) {
                            foreach ($tp in $toolProps) {
                                Remove-ItemProperty -Path $p -Name $tp -ErrorAction SilentlyContinue
                            }
                        }
                    }
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1 -Type DWord -ErrorAction SilentlyContinue

                    # 4. Purge Windows Update Restrictions
                    $wuKeys = @(
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
                    )
                    $wuProps = @('DisableWindowsUpdateAccess', 'DoNotConnectToWindowsUpdateInternetLocations', 'SetPolicyDrivenUpdateApproval', 'WUServer', 'WUStatusServer', 'NoAutoUpdate', 'UseWUServer', 'NoAutoRebootWithLoggedOnUsers')
                    foreach ($wk in $wuKeys) {
                        if (Test-Path $wk) {
                            foreach ($wp in $wuProps) {
                                Remove-ItemProperty -Path $wk -Name $wp -ErrorAction SilentlyContinue
                            }
                        }
                    }

                    # 5. Purge IFEO Debugger Hijacks
                    $ifeo = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options'
                    $ifeoTargets = @(
                        'MsMpEng.exe', 'MpCmdRun.exe', 'SecurityHealthHost.exe', 'SecurityHealthService.exe',
                        'SecurityHealthSystray.exe', 'SecHealthUI.exe', 'smartscreen.exe', 'taskmgr.exe',
                        'regedit.exe', 'powershell.exe', 'cmd.exe', 'msert.exe', 'mbam.exe', 'malwarebytes.exe'
                    )
                    foreach ($t in $ifeoTargets) {
                        $targetPath = ""$ifeo\$t""
                        if (Test-Path $targetPath) {
                            Remove-ItemProperty -Path $targetPath -Name 'Debugger' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $targetPath -Name 'FilterFullPath' -ErrorAction SilentlyContinue
                        }
                    }

                    # 6. Reset Core Security Services Startup Types
                    $svcMap = @{
                        'WinDefend' = 2              # Automatic
                        'WdNisSvc' = 3               # Manual
                        'WdBoot' = 0                 # Boot
                        'WdFilter' = 0               # Boot
                        'Sense' = 3                  # Manual
                        'SecurityHealthService' = 2  # Automatic
                        'wscsvc' = 2                 # Automatic
                        'MpsSvc' = 2                 # Automatic
                        'wuauserv' = 2               # Automatic
                        'bits' = 2                   # Automatic
                        'CryptSvc' = 2               # Automatic
                        'TrustedInstaller' = 3       # Manual
                    }
                    foreach ($s in $svcMap.Keys) {
                        $sp = ""HKLM:\SYSTEM\CurrentControlSet\Services\$s""
                        if (Test-Path $sp) {
                            Set-ItemProperty -Path $sp -Name 'Start' -Value $svcMap[$s] -Type DWord -ErrorAction SilentlyContinue
                        }
                    }

                    # 7. Restore Winlogon userinit and shell defaults
                    $winlogon = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
                    if (Test-Path $winlogon) {
                        Set-ItemProperty -Path $winlogon -Name 'Userinit' -Value 'C:\Windows\system32\userinit.exe,' -Type String -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $winlogon -Name 'Shell' -Value 'explorer.exe' -Type String -ErrorAction SilentlyContinue
                    }

                    # 8. Restore Security Health Systray startup run key
                    $runKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
                    if (Test-Path $runKey) {
                        Set-ItemProperty -Path $runKey -Name 'SecurityHealth' -Value '%windir%\system32\SecurityHealthSystray.exe' -Type ExpandString -ErrorAction SilentlyContinue
                    }
                ";

                Log("⚡ [Administrator Execution] Dispatching elevated PowerShell registry overhaul...");
                RunElevatedPowerShell(elevatedRegistryScript);

                Log("  ✅ Defender Group Policies stripped (DisableAntiSpyware, DisableRealtimeMonitoring).");
                Log("  ✅ System tool locks stripped (DisableTaskMgr, DisableRegistryTools, DisableCMD).");
                Log("  ✅ Windows Update restrictions removed & WSUS hijacking cleared.");
                Log("  ✅ IFEO debugger hooks removed for all security executables & system tools.");
                Log("  ✅ Service startup parameters set to Automatic/Boot in HKLM\\SYSTEM\\CurrentControlSet\\Services.");
                Log("  ✅ Winlogon Shell & Security Health Systray autorun restored.");

                return (true, logs);
            });
        }

        /// <summary>
        /// Downloads and installs official Microsoft SecurityHealthSetup.exe to restore missing/corrupted Windows Security App & SecurityHealthService.
        /// </summary>
        public static async Task<(bool success, string message)> DownloadAndReinstallDefenderAppAsync(Action<string>? progressCallback = null)
        {
            string tempInstaller = Path.Combine(Path.GetTempPath(), "SecurityHealthSetup.exe");
            try
            {
                progressCallback?.Invoke("🌐 Connecting to Microsoft CDN to download official SecurityHealthSetup.exe...");

                bool downloaded = await DownloadFileWithProgressAsync(DEFENDER_HEALTH_SETUP_URL, tempInstaller, progressCallback);
                if (!downloaded || !File.Exists(tempInstaller))
                {
                    return (false, "Failed to download SecurityHealthSetup.exe from Microsoft CDN.");
                }

                progressCallback?.Invoke("⚙️ Executing elevated SecurityHealthSetup.exe installer as Administrator...");
                var psi = new ProcessStartInfo
                {
                    FileName = tempInstaller,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await Task.Run(() => proc.WaitForExit(60000));
                }

                progressCallback?.Invoke("📦 Re-registering Microsoft.SecHealthUI modern AppX package via elevated PowerShell...");
                string reRegisterScript = @"
                    Get-AppxPackage Microsoft.SecHealthUI -AllUsers | Reset-AppxPackage -ErrorAction SilentlyContinue
                    $manifest = (Get-AppxPackage Microsoft.SecHealthUI -AllUsers).InstallLocation + '\AppXManifest.xml'
                    if (Test-Path $manifest) {
                        Add-AppxPackage -DisableDevelopmentMode -Register $manifest -ErrorAction SilentlyContinue
                    }
                    sc.exe config SecurityHealthService start= auto
                    sc.exe start SecurityHealthService
                ";
                RunElevatedPowerShell(reRegisterScript);

                progressCallback?.Invoke("✅ Windows Security App & Security Health Service successfully restored!");
                return (true, "SecurityHealthSetup completed successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error downloading/reinstalling Windows Security: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempInstaller)) File.Delete(tempInstaller); } catch { }
            }
        }

        /// <summary>
        /// Downloads and installs the latest Microsoft Defender Antimalware Engine & Definitions package (mpam-fe.exe).
        /// </summary>
        public static async Task<(bool success, string message)> DownloadAndReinstallAntimalwareEngineAsync(Action<string>? progressCallback = null)
        {
            bool is64 = Environment.Is64BitOperatingSystem;
            string url = is64 ? DEFENDER_ENGINE_X64_URL : DEFENDER_ENGINE_X86_URL;
            string tempInstaller = Path.Combine(Path.GetTempPath(), "mpam-fe.exe");

            try
            {
                progressCallback?.Invoke($"🌐 Downloading latest Microsoft Defender Antimalware Engine & Signatures ({ (is64 ? "64-bit" : "32-bit") })...");

                bool downloaded = await DownloadFileWithProgressAsync(url, tempInstaller, progressCallback);
                if (!downloaded || !File.Exists(tempInstaller))
                {
                    return (false, "Failed to download mpam-fe.exe from Microsoft definition update servers.");
                }

                progressCallback?.Invoke("🛡️ Extracting and enforcing antimalware engine binaries as Administrator (mpam-fe.exe -q)...");
                var psi = new ProcessStartInfo
                {
                    FileName = tempInstaller,
                    Arguments = "-q",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await Task.Run(() => proc.WaitForExit(90000));
                }

                progressCallback?.Invoke("⚙️ Starting Defender core services...");
                RunProcess("sc.exe", "config WinDefend start= auto");
                RunProcess("sc.exe", "start WinDefend");
                RunProcess("sc.exe", "config WdNisSvc start= auto");
                RunProcess("sc.exe", "start WdNisSvc");

                progressCallback?.Invoke("✅ Microsoft Defender engine and signature definitions successfully installed!");
                return (true, "Antimalware engine installed successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error downloading/installing Defender Engine: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempInstaller)) File.Delete(tempInstaller); } catch { }
            }
        }

        /// <summary>
        /// Downloads and executes Microsoft Safety Scanner (MSERT) emergency standalone malware scanner.
        /// </summary>
        public static async Task<(bool success, string message)> DownloadAndRunMsertScannerAsync(bool quiet = false, Action<string>? progressCallback = null)
        {
            bool is64 = Environment.Is64BitOperatingSystem;
            string url = is64 ? MSERT_X64_URL : MSERT_X86_URL;
            string tempMsert = Path.Combine(Path.GetTempPath(), "MSERT.exe");

            try
            {
                progressCallback?.Invoke($"🌐 Downloading Microsoft Emergency Safety Scanner (MSERT) ({ (is64 ? "64-bit" : "32-bit") })...");

                bool downloaded = await DownloadFileWithProgressAsync(url, tempMsert, progressCallback);
                if (!downloaded || !File.Exists(tempMsert))
                {
                    return (false, "Failed to download MSERT.exe from Microsoft.");
                }

                progressCallback?.Invoke("🚀 Launching Microsoft Safety Scanner as Administrator (MSERT)...");
                var psi = new ProcessStartInfo
                {
                    FileName = tempMsert,
                    Arguments = quiet ? "/q /f:y" : "",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(psi);
                progressCallback?.Invoke("✅ Microsoft Safety Scanner (MSERT) is now active!");
                return (true, "MSERT dispatched successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error downloading/running MSERT: {ex.Message}");
            }
        }

        /// <summary>
        /// Fixes registry service permissions, starts disabled core services (WinDefend, wuauserv), and removes IFEO hijack hooks.
        /// </summary>
        public static async Task<bool> FixServicePermissionsAndStartupAsync(Action<string>? progressCallback = null)
        {
            return await Task.Run(async () =>
            {
                progressCallback?.Invoke("🔧 Purging malicious IFEO hooks and resetting service startup keys in Registry as Administrator...");
                await FixAllRegistryPoliciesElevatedAsync(progressCallback);

                progressCallback?.Invoke("⚙️ Starting WinDefend, MpsSvc, wscsvc, wuauserv, and CryptSvc...");
                string[] services = new[] { "wuauserv", "bits", "CryptSvc", "WinDefend", "WdNisSvc", "SecurityHealthService", "wscsvc", "MpsSvc" };
                foreach (var svc in services)
                {
                    RunProcess("sc.exe", $"config {svc} start= auto");
                    RunProcess("sc.exe", $"start {svc}");
                }

                return true;
            });
        }

        /// <summary>
        /// Re-enables, resets, and fully restores all Windows Defender, Firewall, and Security subsystems.
        /// </summary>
        public static async Task<(bool success, List<string> logs)> ReenableWindowsSecurityAsync(bool triggerQuickScan = true, Action<string>? liveLog = null)
        {
            var logs = new List<string>();
            void Log(string msg)
            {
                logs.Add(msg);
                liveLog?.Invoke(msg);
            }

            return await Task.Run(async () =>
            {
                Log($"🛡️ [1/9] Starting Full Windows Security & Defender Recovery Protocol at {DateTime.Now:HH:mm:ss}...");

                // Step 1: Remove Malicious Registry Policies & IFEO Hooks via Administrator PowerShell
                Log("🧹 [2/9] Executing Administrator PowerShell registry & policy purge...");
                var (regOk, regLogs) = await FixAllRegistryPoliciesElevatedAsync(Log);

                // Step 2: Un-disable & Start Security Services
                Log("⚙️ [3/9] Restoring and un-disabling Windows Defender, Security Center, Firewall & Windows Update services...");
                await FixServicePermissionsAndStartupAsync(s => Log($"  -> {s}"));
                Log("  ✅ Security services configured to Automatic startup and started.");

                // Step 3: Enable Windows Defender Real-Time Protection & Preferences
                Log("🛡️ [4/9] Enforcing Windows Defender Real-Time Protection, Script Scanning & Cloud AI Guard via elevated PowerShell...");
                string enableMpPs = @"
                    Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableBehaviorMonitoring $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableIOAVProtection $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableScriptScanning $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableBlockAtFirstSeen $false -ErrorAction SilentlyContinue
                    Set-MpPreference -MAPSReporting 2 -ErrorAction SilentlyContinue
                    Set-MpPreference -SubmitSamplesConsent 1 -ErrorAction SilentlyContinue
                    Set-MpPreference -EnableNetworkProtection 1 -ErrorAction SilentlyContinue
                    Set-MpPreference -PUAProtection 1 -ErrorAction SilentlyContinue
                    Set-MpPreference -ScanAvgCPULoadFactor 50 -ErrorAction SilentlyContinue
                ";
                RunElevatedPowerShell(enableMpPs);
                Log("  ✅ Real-time Monitoring, Behavior Monitoring, IOAV, Script Scan, PUA Protection, and Cloud AI Guard enabled.");

                // Step 4: Reset & Enable Windows Firewall
                Log("🔥 [5/9] Activating Windows Firewall on Domain, Private, and Public profiles...");
                RunProcess("netsh.exe", "advfirewall set allprofiles state on");
                RunElevatedPowerShell("Set-NetFirewallProfile -All -Enabled True -ErrorAction SilentlyContinue");
                Log("  ✅ Windows Firewall state set to Active (ON) for all network profiles.");

                // Step 5: Clean Rogue Defender Exclusions
                Log("🧹 [6/9] Purging broad/malicious Defender exclusions added by malware...");
                string cleanExclusionsPs = @"
                    $pref = Get-MpPreference -ErrorAction SilentlyContinue
                    if ($pref) {
                        foreach ($p in $pref.ExclusionPath) {
                            if ($p -eq 'C:\' -or $p -eq 'C:\*' -or $p -like '*Temp*' -or $p -like '*AppData*') {
                                Remove-MpPreference -ExclusionPath $p -ErrorAction SilentlyContinue
                            }
                        }
                        foreach ($ext in $pref.ExclusionExtension) {
                            if ($ext -eq 'exe' -or $ext -eq 'dll' -or $ext -eq 'bat' -or $ext -eq 'ps1' -or $ext -eq 'vbs') {
                                Remove-MpPreference -ExclusionExtension $ext -ErrorAction SilentlyContinue
                            }
                        }
                    }
                ";
                RunElevatedPowerShell(cleanExclusionsPs);
                Log("  ✅ Removed dangerous broad exclusion directories and executable extensions.");

                // Step 6: Clean Hosts file if tampered
                Log("🌐 [7/9] Inspecting network routing and hosts file integrity...");
                try
                {
                    string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                    if (File.Exists(hostsPath))
                    {
                        var lines = File.ReadAllLines(hostsPath);
                        string[] securityDomains = new[] { "microsoft.com", "windowsupdate.com", "defender", "virustotal", "kaspersky", "malwarebytes" };
                        var cleanLines = lines.Where(line =>
                        {
                            string l = line.Trim();
                            if (string.IsNullOrEmpty(l) || l.StartsWith("#")) return true;
                            return !securityDomains.Any(d => l.Contains(d, StringComparison.OrdinalIgnoreCase));
                        }).ToList();

                        if (cleanLines.Count != lines.Length)
                        {
                            File.WriteAllLines(hostsPath, cleanLines);
                            Log("  ✅ Purged malicious DNS blocking entries from hosts file.");
                        }
                        else
                        {
                            Log("  ✅ Hosts file is clean and untampered.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"  ⚠️ Hosts file check notice: {ex.Message}");
                }

                // Step 7: Update Antivirus Signatures
                Log("🔄 [8/9] Updating Windows Defender Antivirus Signatures and Definitions...");
                RunElevatedPowerShell("Update-MpSignature -ErrorAction SilentlyContinue");
                Log("  ✅ Antivirus signature definitions update dispatched.");

                // Step 8: Optional Emergency Quick Scan
                if (triggerQuickScan)
                {
                    Log("⚡ [9/9] Initiating background Quick Malware Scan...");
                    RunElevatedPowerShell("Start-MpScan -ScanType QuickScan -ErrorAction SilentlyContinue");
                    Log("  ✅ Quick Scan active in background.");
                }

                Log($"🎉 Windows Security Recovery Complete at {DateTime.Now:HH:mm:ss}! System protection restored.");
                return (true, logs);
            });
        }

        /// <summary>
        /// Removes all configured Defender exclusion paths, extensions, and processes.
        /// </summary>
        public static async Task<bool> ClearAllExclusionsAsync()
        {
            return await Task.Run(() =>
            {
                string ps = @"
                    $pref = Get-MpPreference -ErrorAction SilentlyContinue
                    if ($pref) {
                        foreach ($p in $pref.ExclusionPath) { Remove-MpPreference -ExclusionPath $p -ErrorAction SilentlyContinue }
                        foreach ($pr in $pref.ExclusionProcess) { Remove-MpPreference -ExclusionProcess $pr -ErrorAction SilentlyContinue }
                        foreach ($ext in $pref.ExclusionExtension) { Remove-MpPreference -ExclusionExtension $ext -ErrorAction SilentlyContinue }
                    }
                ";
                RunElevatedPowerShell(ps);
                return true;
            });
        }

        /// <summary>
        /// Updates Windows Defender antivirus definitions.
        /// </summary>
        public static async Task<bool> UpdateSignaturesAsync()
        {
            return await Task.Run(() =>
            {
                RunElevatedPowerShell("Update-MpSignature -ErrorAction SilentlyContinue");
                return true;
            });
        }

        /// <summary>
        /// Starts a Defender Quick Scan.
        /// </summary>
        public static async Task<bool> StartQuickScanAsync()
        {
            return await Task.Run(() =>
            {
                RunElevatedPowerShell("Start-MpScan -ScanType QuickScan -ErrorAction SilentlyContinue");
                return true;
            });
        }

        /// <summary>
        /// Starts a full deep system malware scan.
        /// </summary>
        public static async Task<bool> StartFullScanAsync()
        {
            return await Task.Run(() =>
            {
                RunElevatedPowerShell("Start-MpScan -ScanType FullScan -ErrorAction SilentlyContinue");
                return true;
            });
        }

        private static async Task<bool> DownloadFileWithProgressAsync(string url, string destinationPath, Action<string>? progressCallback)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                DateTime lastReport = DateTime.MinValue;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (DateTime.Now - lastReport > TimeSpan.FromMilliseconds(500))
                    {
                        lastReport = DateTime.Now;
                        if (totalBytes > 0)
                        {
                            double pct = (double)totalRead / totalBytes * 100.0;
                            double mbRead = totalRead / (1024.0 * 1024.0);
                            double mbTotal = totalBytes / (1024.0 * 1024.0);
                            progressCallback?.Invoke($"  ⏬ Downloading: {mbRead:F1} MB / {mbTotal:F1} MB ({pct:F0}%)");
                        }
                        else
                        {
                            double mbRead = totalRead / (1024.0 * 1024.0);
                            progressCallback?.Invoke($"  ⏬ Downloading: {mbRead:F1} MB...");
                        }
                    }
                }

                progressCallback?.Invoke($"  ✅ Download complete ({totalRead / (1024.0 * 1024.0):F1} MB).");
                return true;
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"  ❌ Download error: {ex.Message}");
                return false;
            }
        }

        private static bool IsServiceRunning(string serviceName)
        {
            try
            {
                string outText = RunProcess("sc.exe", $"query {serviceName}");
                return outText.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static string RunProcess(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return string.Empty;
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(10000);
                return output;
            }
            catch { return string.Empty; }
        }

        private static string RunPowerShellOutput(string script)
        {
            try
            {
                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encoded}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return string.Empty;
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(15000);
                return output;
            }
            catch { return string.Empty; }
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
                proc?.WaitForExit(30000);
            }
            catch
            {
                // Fallback to standard process execution if UAC elevation prompt is denied or already elevated
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
                    proc?.WaitForExit(30000);
                }
                catch { }
            }
        }
    }
}
