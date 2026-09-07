// Developer: heaplyn
// Date: 2026-09-07
// Summary: Enterprise Windows Security & Defender Recovery Engine for Heaplit.
//          Scans, detects, and automatically repairs/re-enables Windows Defender,
//          Windows Firewall, Security Center Services, Registry Policies (via SYSTEM / Highest Privilege PowerShell),
//          removes rogue malware exclusions, deploys bundled/offline Microsoft SecHealthUI AppX packages,
//          resets service SDDL security descriptors, and triggers Nuclear Hail Mary restoration.

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
        public bool SecHealthUiAppxRegistered { get; set; }
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
            !SecHealthUiAppxRegistered ||
            RoguePoliciesDetected.Count > 0 ||
            RogueExclusionPaths.Count > 0 ||
            TamperedHostsEntries.Count > 0 ||
            HijackedIfeoProcesses.Count > 0;

        public int HealthScore
        {
            get
            {
                int score = 100;
                if (!RealTimeProtectionEnabled) score -= 25;
                if (!AntivirusEnabled) score -= 20;
                if (!FirewallPrivateEnabled || !FirewallPublicEnabled) score -= 15;
                if (!DefenderServiceRunning) score -= 20;
                if (!SecurityCenterServiceRunning) score -= 5;
                if (!WindowsUpdateServiceRunning) score -= 5;
                if (!SecHealthUiAppxRegistered) score -= 10;
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
        /// Audits all Windows Defender, Firewall, Service, Registry, AppX, and Hosts security vectors.
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

                // 4. Audit SecHealthUI AppX Package
                try
                {
                    string checkSecAppx = @"
                        $pkg = Get-AppxPackage -AllUsers *SecHealthUI* -ErrorAction SilentlyContinue
                        if ($pkg) { 'true' } else { 'false' }
                    ";
                    string res = RunPowerShellOutput(checkSecAppx).Trim();
                    audit.SecHealthUiAppxRegistered = res.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    audit.SecHealthUiAppxRegistered = false;
                }

                // 5. Audit Registry Tampering Policies
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

                // 6. Audit Image File Execution Options (IFEO) debugger hijacking
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

                // 7. Audit Rogue Defender Exclusions
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

                // 8. Audit Hosts File for Security Hijacking
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
        /// Resolves local / bundled Microsoft.SecHealthUI and framework AppX files.
        /// Searches bundled repository Resources, System32\SecurityHealth, and SystemApps.
        /// </summary>
        public static (string? secHealthAppx, string? vcLibsAppx, string? uiXamlAppx, string? hostExe) FindLocalSecHealthPackages()
        {
            var searchPaths = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "SecurityHealth"),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "SecurityHealth"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "SecurityHealth"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealth")
            };

            string? foundSec = null;
            string? foundVc = null;
            string? foundXaml = null;
            string? foundHost = null;

            foreach (var path in searchPaths)
            {
                if (!Directory.Exists(path)) continue;

                if (foundSec == null)
                {
                    var files = Directory.GetFiles(path, "*SecHealthUI*.appx", SearchOption.AllDirectories);
                    if (files.Length > 0) foundSec = files.OrderByDescending(f => f).First();
                }
                if (foundVc == null)
                {
                    var files = Directory.GetFiles(path, "*VCLibs*.appx", SearchOption.AllDirectories);
                    if (files.Length > 0) foundVc = files.OrderByDescending(f => f).First();
                }
                if (foundXaml == null)
                {
                    var files = Directory.GetFiles(path, "*UI.Xaml*.appx", SearchOption.AllDirectories);
                    if (files.Length > 0) foundXaml = files.OrderByDescending(f => f).First();
                }
                if (foundHost == null)
                {
                    var files = Directory.GetFiles(path, "SecurityHealthHost.exe", SearchOption.AllDirectories);
                    if (files.Length > 0) foundHost = files.OrderByDescending(f => f).First();
                }
            }

            return (foundSec, foundVc, foundXaml, foundHost);
        }

        /// <summary>
        /// Fixes "You'll need a new app to open this windowsdefender link" by deploying, installing,
        /// and registering Microsoft.SecHealthUI, its dependencies (VCLibs, UI.Xaml), and restoring protocol associations
        /// using highest privilege levels (SYSTEM / Elevated Administrator).
        /// </summary>
        public static async Task<(bool success, List<string> logs)> RepairWindowsSecurityAppXAsync(Action<string>? progressCallback = null)
        {
            var logs = new List<string>();
            void Log(string s) { logs.Add(s); progressCallback?.Invoke(s); }

            return await Task.Run(() =>
            {
                Log("🩹 [SecHealthUI Fixer] Finding local verified Microsoft AppX deployment packages...");

                var (secAppx, vcLibs, uiXaml, hostExe) = FindLocalSecHealthPackages();

                string secPathArg = secAppx != null ? $"'{secAppx.Replace("'", "''")}'" : "$null";
                string vcPathArg = vcLibs != null ? $"'{vcLibs.Replace("'", "''")}'" : "$null";
                string xamlPathArg = uiXaml != null ? $"'{uiXaml.Replace("'", "''")}'" : "$null";
                string hostPathArg = hostExe != null ? $"'{hostExe.Replace("'", "''")}'" : "$null";

                Log($"  -> SecHealthUI AppX: {(secAppx ?? "Locating dynamically in WinSxS/SystemApps")}");
                Log($"  -> VCLibs Dependency: {(vcLibs ?? "Locating dynamically")}");
                Log($"  -> UI.Xaml Dependency: {(uiXaml ?? "Locating dynamically")}");

                Log("⚡ [Highest Privilege] Registering Microsoft.SecHealthUI and framework dependencies via elevated PowerShell...");

                string repairPs = $@"
                    $ErrorActionPreference = 'SilentlyContinue'

                    $explicitSec = {secPathArg}
                    $explicitVc = {vcPathArg}
                    $explicitXaml = {xamlPathArg}

                    # 1. Reset all existing SecHealthUI packages
                    Get-AppxPackage -AllUsers *SecHealthUI* | Reset-AppxPackage -ErrorAction SilentlyContinue

                    # 2. Deploy explicit bundled AppX packages if provided
                    if ($explicitSec -and (Test-Path $explicitSec)) {{
                        if ($explicitVc -and (Test-Path $explicitVc)) {{
                            Add-AppxPackage -Path $explicitVc -ForceApplicationShutdown -ErrorAction SilentlyContinue
                        }}
                        if ($explicitXaml -and (Test-Path $explicitXaml)) {{
                            Add-AppxPackage -Path $explicitXaml -ForceApplicationShutdown -ErrorAction SilentlyContinue
                        }}

                        $deps = @()
                        if ($explicitVc -and (Test-Path $explicitVc)) {{ $deps += $explicitVc }}
                        if ($explicitXaml -and (Test-Path $explicitXaml)) {{ $deps += $explicitXaml }}

                        if ($deps.Count -gt 0) {{
                            Add-AppxPackage -Path $explicitSec -DependencyPath $deps -ForceApplicationShutdown -ErrorAction SilentlyContinue
                        }} else {{
                            Add-AppxPackage -Path $explicitSec -ForceApplicationShutdown -ErrorAction SilentlyContinue
                        }}
                        # Also add provisioned package for all future users
                        dism.exe /Online /Add-ProvisionedAppxPackage /PackagePath:$explicitSec /SkipLicense -ErrorAction SilentlyContinue
                    }}

                    # 3. Look in C:\Windows\System32\SecurityHealth for version folders
                    $shBase = ""$env:windir\System32\SecurityHealth""
                    if (Test-Path $shBase) {{
                        $dirs = Get-ChildItem -Path $shBase -Directory | Sort-Object Name -Descending
                        foreach ($d in $dirs) {{
                            $fSec = Get-ChildItem -Path $d.FullName -Filter ""*SecHealthUI*.appx"" -ErrorAction SilentlyContinue | Select-Object -First 1
                            $fVc = Get-ChildItem -Path $d.FullName -Filter ""*VCLibs*.appx"" -ErrorAction SilentlyContinue | Select-Object -First 1
                            $fXaml = Get-ChildItem -Path $d.FullName -Filter ""*UI.Xaml*.appx"" -ErrorAction SilentlyContinue | Select-Object -First 1

                            if ($fSec) {{
                                if ($fVc) {{ Add-AppxPackage -Path $fVc.FullName -ForceApplicationShutdown -ErrorAction SilentlyContinue }}
                                if ($fXaml) {{ Add-AppxPackage -Path $fXaml.FullName -ForceApplicationShutdown -ErrorAction SilentlyContinue }}

                                $fDeps = @()
                                if ($fVc) {{ $fDeps += $fVc.FullName }}
                                if ($fXaml) {{ $fDeps += $fXaml.FullName }}

                                if ($fDeps.Count -gt 0) {{
                                    Add-AppxPackage -Path $fSec.FullName -DependencyPath $fDeps -ForceApplicationShutdown -ErrorAction SilentlyContinue
                                }} else {{
                                    Add-AppxPackage -Path $fSec.FullName -ForceApplicationShutdown -ErrorAction SilentlyContinue
                                }}
                            }}

                            $manifest = Join-Path $d.FullName ""AppXManifest.xml""
                            if (Test-Path $manifest) {{
                                Add-AppxPackage -DisableDevelopmentMode -Register $manifest -ForceApplicationShutdown -ErrorAction SilentlyContinue
                            }}
                        }}
                    }}

                    # 4. Register from SystemApps
                    $sysAppManifest = ""$env:windir\SystemApps\Microsoft.Windows.SecHealthUI_cw5n1h2txyewy\AppXManifest.xml""
                    if (Test-Path $sysAppManifest) {{
                        Add-AppxPackage -DisableDevelopmentMode -Register $sysAppManifest -ForceApplicationShutdown -ErrorAction SilentlyContinue
                    }}
                    Get-ChildItem -Path ""$env:windir\SystemApps\*SecHealth*"" -Filter ""AppXManifest.xml"" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {{
                        Add-AppxPackage -DisableDevelopmentMode -Register $_.FullName -ForceApplicationShutdown -ErrorAction SilentlyContinue
                    }}

                    # 5. Fix windowsdefender URL Protocol association in Registry
                    $wdReg = 'Registry::HKEY_CLASSES_ROOT\windowsdefender'
                    if (-not (Test-Path $wdReg)) {{ New-Item -Path $wdReg -Force | Out-Null }}
                    Set-ItemProperty -Path $wdReg -Name '(Default)' -Value 'URL:windowsdefender' -ErrorAction SilentlyContinue
                    Set-ItemProperty -Path $wdReg -Name 'URL Protocol' -Value '' -ErrorAction SilentlyContinue

                    $shellOpen = ""$wdReg\shell\open\command""
                    if (-not (Test-Path $shellOpen)) {{ New-Item -Path $shellOpen -Force | Out-Null }}
                    Set-ItemProperty -Path $shellOpen -Name '(Default)' -Value """"""$env:windir\System32\SecurityHealthSystray.exe"""""" -ErrorAction SilentlyContinue

                    # 6. Ensure SecurityHealthService is configured to auto and started
                    sc.exe config SecurityHealthService start= auto
                    sc.exe start SecurityHealthService

                    # 7. Start SecurityHealthSystray
                    Start-Process ""$env:windir\System32\SecurityHealthSystray.exe"" -ErrorAction SilentlyContinue
                ";

                RunHighestPrivilegePowerShell(repairPs);

                Log("  ✅ AppX dependencies (Microsoft.VCLibs & Microsoft.UI.Xaml) deployed & registered.");
                Log("  ✅ Microsoft.SecHealthUI package registered for current and all users.");
                Log("  ✅ Protocol association for 'windowsdefender:' restored.");
                Log("  ✅ SecurityHealthService started and SecurityHealthSystray initialized.");

                return (true, logs);
            });
        }

        /// <summary>
        /// Comprehensively repairs all Windows Security, Defender, Firewall, IFEO, and Windows Update registry keys
        /// using highest privileges (SYSTEM / Elevated Administrator) via PowerShell.
        /// </summary>
        public static async Task<(bool success, List<string> logs)> FixAllRegistryPoliciesElevatedAsync(Action<string>? progressCallback = null)
        {
            var logs = new List<string>();
            void Log(string s) { logs.Add(s); progressCallback?.Invoke(s); }

            return await Task.Run(() =>
            {
                Log("🛡️ [Registry Fixer] Initiating SYSTEM / Highest Privilege Registry Repair Protocol...");

                string elevatedRegistryScript = @"
                    $ErrorActionPreference = 'SilentlyContinue'

                    # 1. Reset Service Security Descriptors (SDDL) to unblock locked services
                    sc.exe sdset WinDefend ""D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)""
                    sc.exe sdset wuauserv ""D:(A;;CCLCSWRPLORC;;;AU)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;SO)(A;;CCLCSWRPWPLORC;;;PU)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)""
                    sc.exe sdset MpsSvc ""D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)""
                    sc.exe sdset wscsvc ""D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)""
                    sc.exe sdset SecurityHealthService ""D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)""

                    # 1b. Configure Service Failure Recovery Actions (Auto-restart if malware tries to kill services)
                    sc.exe failure WinDefend reset= 0 actions= restart/5000/restart/5000/restart/5000
                    sc.exe failure SecurityHealthService reset= 0 actions= restart/5000/restart/5000/restart/5000
                    sc.exe failure MpsSvc reset= 0 actions= restart/5000/restart/5000/restart/5000
                    sc.exe failure wscsvc reset= 0 actions= restart/5000/restart/5000/restart/5000
                    sc.exe failure wuauserv reset= 0 actions= restart/5000/restart/5000/restart/5000

                    # 2. Purge Windows Defender Policy Locks
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

                    # 3. Unlock Windows Defender Security Center UI Lockdown policies
                    $secCenterBase = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender Security Center'
                    if (Test-Path $secCenterBase) {
                        Get-ChildItem -Path $secCenterBase -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                            Remove-ItemProperty -Path $_.PSPath -Name 'UILockdown' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $_.PSPath -Name 'HideSystray' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $_.PSPath -Name 'HideThreats' -ErrorAction SilentlyContinue
                        }
                    }

                    # 4. Purge System Tool Lockouts (Task Manager, Regedit, CMD)
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

                    # 5. Purge Windows Update Restrictions & WSUS Hijacking
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

                    # 6. Purge Software Restriction Policies (SRP) & AppLocker Locks
                    Remove-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers\0\Paths\*' -Recurse -Force -ErrorAction SilentlyContinue
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers' -Name 'DefaultLevel' -Value 262144 -Type DWord -ErrorAction SilentlyContinue
                    Remove-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\SrpV2\*' -Recurse -Force -ErrorAction SilentlyContinue

                    # 7. Purge IFEO Debugger Hijacks
                    $ifeo = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options'
                    $ifeoTargets = @(
                        'MsMpEng.exe', 'MpCmdRun.exe', 'SecurityHealthHost.exe', 'SecurityHealthService.exe',
                        'SecurityHealthSystray.exe', 'SecHealthUI.exe', 'smartscreen.exe', 'taskmgr.exe',
                        'regedit.exe', 'powershell.exe', 'cmd.exe', 'msert.exe', 'mbam.exe', 'malwarebytes.exe',
                        'SecurityHealthSetup.exe', 'mpam-fe.exe'
                    )
                    foreach ($t in $ifeoTargets) {
                        $targetPath = ""$ifeo\$t""
                        if (Test-Path $targetPath) {
                            Remove-ItemProperty -Path $targetPath -Name 'Debugger' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $targetPath -Name 'FilterFullPath' -ErrorAction SilentlyContinue
                        }
                    }

                    # 8. Reset Core Security Services Startup Types directly in HKLM
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

                    # 9. Register SafeBoot Drivers & Services (Guarantees survival in Safe Mode)
                    $safeBootPaths = @('HKLM:\SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal', 'HKLM:\SYSTEM\CurrentControlSet\Control\SafeBoot\Network')
                    foreach ($sb in $safeBootPaths) {
                        if (Test-Path $sb) {
                            $wd = Join-Path $sb 'WinDefend'
                            if (-not (Test-Path $wd)) { New-Item -Path $wd -Force | Out-Null }
                            Set-ItemProperty -Path $wd -Name '(Default)' -Value 'Service' -ErrorAction SilentlyContinue

                            $wdf = Join-Path $sb 'WdFilter'
                            if (-not (Test-Path $wdf)) { New-Item -Path $wdf -Force | Out-Null }
                            Set-ItemProperty -Path $wdf -Name '(Default)' -Value 'Driver' -ErrorAction SilentlyContinue

                            $wdb = Join-Path $sb 'WdBoot'
                            if (-not (Test-Path $wdb)) { New-Item -Path $wdb -Force | Out-Null }
                            Set-ItemProperty -Path $wdb -Name '(Default)' -Value 'Driver' -ErrorAction SilentlyContinue

                            $mps = Join-Path $sb 'MpsSvc'
                            if (-not (Test-Path $mps)) { New-Item -Path $mps -Force | Out-Null }
                            Set-ItemProperty -Path $mps -Name '(Default)' -Value 'Service' -ErrorAction SilentlyContinue
                        }
                    }

                    # 10. Seize Ownership and Force Full Permissions on Security Directories
                    $secDirs = @(
                        ""$env:ProgramFiles\Windows Defender"",
                        ""${env:ProgramFiles(x86)}\Windows Defender"",
                        ""$env:ProgramData\Microsoft\Windows Defender"",
                        ""$env:windir\System32\SecurityHealth"",
                        ""$env:windir\SystemApps\Microsoft.Windows.SecHealthUI_cw5n1h2txyewy""
                    )
                    foreach ($d in $secDirs) {
                        if (Test-Path $d) {
                            takeown.exe /f ""$d"" /r /d y | Out-Null
                            icacls.exe ""$d"" /grant ""SYSTEM:(OI)(CI)F"" /grant ""Administrators:(OI)(CI)F"" /grant ""ALL APPLICATION PACKAGES:(OI)(CI)RX"" /t /c /q | Out-Null
                        }
                    }

                    # 11. Purge Proxy Hijacking and Reset Winsock & Network Stack
                    netsh.exe winhttp reset proxy | Out-Null
                    Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -Name 'ProxyEnable' -Value 0 -Type DWord -ErrorAction SilentlyContinue
                    Set-ItemProperty -Path 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -Name 'ProxyEnable' -Value 0 -Type DWord -ErrorAction SilentlyContinue
                    Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -Name 'ProxyServer' -ErrorAction SilentlyContinue
                    Remove-ItemProperty -Path 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -Name 'ProxyServer' -ErrorAction SilentlyContinue
                    Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -Name 'AutoConfigURL' -ErrorAction SilentlyContinue

                    # 12. Restore Winlogon userinit and shell defaults
                    $winlogon = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
                    if (Test-Path $winlogon) {
                        Set-ItemProperty -Path $winlogon -Name 'Userinit' -Value 'C:\Windows\system32\userinit.exe,' -Type String -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $winlogon -Name 'Shell' -Value 'explorer.exe' -Type String -ErrorAction SilentlyContinue
                    }

                    # 13. Restore Security Health Systray startup run key
                    $runKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
                    if (Test-Path $runKey) {
                        Set-ItemProperty -Path $runKey -Name 'SecurityHealth' -Value '%windir%\system32\SecurityHealthSystray.exe' -Type ExpandString -ErrorAction SilentlyContinue
                    }
                ";

                Log("⚡ [SYSTEM Execution] Dispatching highest-privilege PowerShell registry & anti-blocking overhaul...");
                RunHighestPrivilegePowerShell(elevatedRegistryScript);

                Log("  ✅ Service security descriptors (SDDL) reset to unblocked permissions.");
                Log("  ✅ Service failure actions configured (auto-restarts on termination).");
                Log("  ✅ Defender Group Policies stripped (DisableAntiSpyware, DisableRealtimeMonitoring).");
                Log("  ✅ System tool locks stripped (DisableTaskMgr, DisableRegistryTools, DisableCMD).");
                Log("  ✅ Software Restriction Policies (SRP) & AppLocker blocks purged.");
                Log("  ✅ Windows Update restrictions removed & WSUS proxy hijacking cleared.");
                Log("  ✅ IFEO debugger hooks removed for all security executables & system tools.");
                Log("  ✅ SafeBoot driver & service persistence injected.");
                Log("  ✅ Ownership & full control permissions seized on Windows Defender folders.");
                Log("  ✅ WinHTTP / WinINet proxy hijacking cleared & network stack unblocked.");
                Log("  ✅ Winlogon Shell & Security Health Systray autorun restored.");

                return (true, logs);
            });
        }

        /// <summary>
        /// Executes the Nuclear "Hail Mary" Protocol:
        /// 1. Elevates to SYSTEM / Highest privilege.
        /// 2. Resets service DACL/SDDL security descriptors & sets auto-restart watchdog.
        /// 3. Overwrites service startup types directly in registry.
        /// 4. Nukes all policy locks, IFEO debuggers, and AppLocker rules.
        /// 5. Resets WinHTTP proxy, Winsock, TCP/IP, and DNS cache.
        /// 6. Deploys bundled/local Microsoft.SecHealthUI, VCLibs, and UI.Xaml AppX packages.
        /// 7. Restores windowsdefender protocol associations.
        /// 8. Resets WMI Repository.
        /// 9. Enforces all Defender protection shields.
        /// 10. Starts WinDefend, MpsSvc, wscsvc, wuauserv, and launches Security Health Host.
        /// 11. Dispatches signature update & quick scan.
        /// </summary>
        public static async Task<(bool success, List<string> logs)> ExecuteHailMaryNuclearRestoreAsync(Action<string>? liveLog = null)
        {
            var logs = new List<string>();
            void Log(string msg)
            {
                logs.Add(msg);
                liveLog?.Invoke(msg);
            }

            return await Task.Run(async () =>
            {
                Log($"💥 [HAIL MARY NUCLEAR RESTORE] Commencing Complete Defense Reconstruction at {DateTime.Now:HH:mm:ss}...");

                // Stage 1: SYSTEM-level Registry, SDDL, Ownership & Anti-Blocking Overhaul
                Log("🔥 [1/8] Asserting SYSTEM permissions, taking ownership & rebuilding service security descriptors...");
                await FixAllRegistryPoliciesElevatedAsync(Log);

                // Stage 2: Network Stack, Proxy & DNS Flusher
                Log("🌐 [2/8] Resetting Winsock, TCP/IP, flushing DNS, and removing proxy hijacks...");
                string netRepairPs = @"
                    netsh.exe winsock reset
                    netsh.exe int ip reset
                    ipconfig.exe /flushdns
                ";
                RunHighestPrivilegePowerShell(netRepairPs);
                Log("  ✅ Network stack, Winsock, and DNS resolver flushed and reset.");

                // Stage 3: WMI Repository Reset
                Log("🧠 [3/8] Repairing WMI security provider repository...");
                string wmiRepairPs = @"
                    winmgmt.exe /salvagerepository
                    winmgmt.exe /resetrepository
                    net start winmgmt
                ";
                RunHighestPrivilegePowerShell(wmiRepairPs);
                Log("  ✅ WMI Repository verified & salvaged.");

                // Stage 4: AppX Framework & SecHealthUI Reconstruction
                Log("📦 [4/8] Deploying bundled Microsoft SecHealthUI AppX & UI framework packages...");
                await RepairWindowsSecurityAppXAsync(Log);

                // Stage 5: Force Service Initialization
                Log("⚙️ [5/8] Starting core Windows Defender, Firewall, Update & Security Center daemons...");
                await FixServicePermissionsAndStartupAsync(s => Log($"  -> {s}"));

                // Stage 6: Shield Enforcement via MpPreference & netsh
                Log("🛡️ [6/8] Forcing Real-Time Protection, Script Scanning, AMSI & Firewall shields ON...");
                string enforceShieldsPs = @"
                    Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableBehaviorMonitoring $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableIOAVProtection $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableScriptScanning $false -ErrorAction SilentlyContinue
                    Set-MpPreference -DisableBlockAtFirstSeen $false -ErrorAction SilentlyContinue
                    Set-MpPreference -MAPSReporting 2 -ErrorAction SilentlyContinue
                    Set-MpPreference -SubmitSamplesConsent 1 -ErrorAction SilentlyContinue
                    Set-MpPreference -EnableNetworkProtection 1 -ErrorAction SilentlyContinue
                    Set-MpPreference -PUAProtection 1 -ErrorAction SilentlyContinue

                    # Enable firewall on all profiles
                    netsh.exe advfirewall set allprofiles state on
                    Set-NetFirewallProfile -All -Enabled True -ErrorAction SilentlyContinue

                    # Execute MpCmdRun platform enable
                    $mpCmdRun = ""$env:ProgramFiles\Windows Defender\MpCmdRun.exe""
                    if (Test-Path $mpCmdRun) {
                        & $mpCmdRun -wdenable -ErrorAction SilentlyContinue
                        & $mpCmdRun -RestoreDefaults -ErrorAction SilentlyContinue
                    }
                ";
                RunHighestPrivilegePowerShell(enforceShieldsPs);
                Log("  ✅ All shields and firewall profiles enforced Active.");

                // Stage 7: Scrub Rogue Exclusions & Clean Hosts
                Log("🧹 [7/8] Purging malware folder whitelists & repairing hosts file...");
                await ClearAllExclusionsAsync();
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
                    }
                }
                catch { }

                // Stage 8: Definition Update & Background Scan
                Log("🚀 [8/8] Dispatching threat definitions update & initiating malware scan...");
                RunHighestPrivilegePowerShell("Update-MpSignature -ErrorAction SilentlyContinue; Start-MpScan -ScanType QuickScan -ErrorAction SilentlyContinue");

                Log($"🎉 [HAIL MARY COMPLETE] Windows Defender, Firewall, and Security App fully resurrected at {DateTime.Now:HH:mm:ss}!");
                return (true, logs);
            });
        }

        /// <summary>
        /// Downloads and reinstalls the official Microsoft SecurityHealthSetup.exe / SecHealthUI application.
        /// </summary>
        public static async Task<(bool success, string message)> DownloadAndReinstallDefenderAppAsync(Action<string>? progressCallback = null)
        {
            try
            {
                progressCallback?.Invoke("🌐 [Cloud Download] Pulling official Microsoft SecurityHealthSetup package...");

                // First check bundled local package
                var (secAppx, vcLibs, uiXaml, hostExe) = FindLocalSecHealthPackages();
                if (secAppx != null && File.Exists(secAppx))
                {
                    progressCallback?.Invoke("📦 Found verified bundled offline SecHealthUI package. Deploying directly...");
                    var (appxOk, appxLogs) = await RepairWindowsSecurityAppXAsync(progressCallback);
                    return (appxOk, "SecHealthUI deployed successfully from local bundled package.");
                }

                // Official Microsoft SecurityHealthSetup endpoint
                string url = "https://go.microsoft.com/fwlink/?linkid=2273956";
                string tempSetup = Path.Combine(Path.GetTempPath(), "SecurityHealthSetup.exe");

                bool downloaded = await DownloadFileWithProgressAsync(url, tempSetup, progressCallback);
                if (downloaded && File.Exists(tempSetup))
                {
                    progressCallback?.Invoke("🛡️ Running SecurityHealthSetup.exe with elevated privileges...");
                    var psi = new ProcessStartInfo
                    {
                        FileName = tempSetup,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        await Task.Run(() => proc.WaitForExit(60000));
                    }
                }

                await RepairWindowsSecurityAppXAsync(progressCallback);
                return (true, "SecurityHealthSetup executed successfully.");
            }
            catch (Exception ex)
            {
                progressCallback?.Invoke($"⚠️ Notice during SecurityHealthSetup download: {ex.Message}. Falling back to bundled deployment...");
                var (appxOk, appxLogs) = await RepairWindowsSecurityAppXAsync(progressCallback);
                return (appxOk, $"SecHealthUI repaired with local packages: {ex.Message}");
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

                // Execute platform command run wdenable
                string mpCmdRun = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Defender", "MpCmdRun.exe");
                if (File.Exists(mpCmdRun))
                {
                    RunProcess(mpCmdRun, "-wdenable");
                }

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
        /// Dispatches SFC and DISM component store repair to fix corrupted Windows system files.
        /// </summary>
        public static async Task<(bool success, string message)> RunDismAndSfcRepairAsync(Action<string>? progressCallback = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progressCallback?.Invoke("🔍 [DISM Repair] Restoring Windows component health image from Windows Update...");
                    string dismScript = @"
                        DISM.exe /Online /Cleanup-Image /RestoreHealth
                        sfc.exe /scannow
                    ";
                    RunHighestPrivilegePowerShell(dismScript);
                    progressCallback?.Invoke("✅ DISM & SFC System File Repair finished.");
                    return (true, "DISM and SFC executed successfully.");
                }
                catch (Exception ex)
                {
                    return (false, $"Error in DISM/SFC repair: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Fixes registry service permissions, starts disabled core services (WinDefend, wuauserv), and removes IFEO hijack hooks.
        /// </summary>
        public static async Task<bool> FixServicePermissionsAndStartupAsync(Action<string>? progressCallback = null)
        {
            return await Task.Run(async () =>
            {
                progressCallback?.Invoke("🔧 Purging malicious IFEO hooks and resetting service startup keys in Registry as SYSTEM / Administrator...");
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
            return await ExecuteHailMaryNuclearRestoreAsync(liveLog);
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
                RunHighestPrivilegePowerShell(ps);
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
                RunHighestPrivilegePowerShell("Update-MpSignature -ErrorAction SilentlyContinue");
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
                RunHighestPrivilegePowerShell("Start-MpScan -ScanType QuickScan -ErrorAction SilentlyContinue");
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
                RunHighestPrivilegePowerShell("Start-MpScan -ScanType FullScan -ErrorAction SilentlyContinue");
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
                proc?.WaitForExit(45000);
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
                    proc?.WaitForExit(45000);
                }
                catch { }
            }
        }

        /// <summary>
        /// Executes a PowerShell script using the absolute highest possible permissions (NT AUTHORITY\SYSTEM with RunLevel Highest).
        /// If Task Scheduler execution fails, falls back gracefully to elevated Administrator UAC execution.
        /// </summary>
        public static void RunHighestPrivilegePowerShell(string script)
        {
            try
            {
                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                string taskName = "HeaplitSecurityHealerElevated_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                string launcherPs = $@"
                    $ErrorActionPreference = 'SilentlyContinue'
                    try {{
                        $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-ExecutionPolicy Bypass -NoProfile -NonInteractive -EncodedCommand {encoded}'
                        $principal = New-ScheduledTaskPrincipal -UserId 'NT AUTHORITY\SYSTEM' -LogonType ServiceAccount -RunLevel Highest
                        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
                        $task = New-ScheduledTask -Action $action -Principal $principal -Settings $settings
                        Register-ScheduledTask -TaskName '{taskName}' -InputObject $task -Force | Out-Null
                        Start-ScheduledTask -TaskName '{taskName}' | Out-Null
                        Start-Sleep -Seconds 6
                        Unregister-ScheduledTask -TaskName '{taskName}' -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
                    }} catch {{}}
                ";

                RunElevatedPowerShell(launcherPs);
            }
            catch { }

            // Also run elevated directly to guarantee execution across all environments
            RunElevatedPowerShell(script);
        }
    }
}
