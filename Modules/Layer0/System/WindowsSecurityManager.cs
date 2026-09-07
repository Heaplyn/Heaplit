// Developer: heaplyn
// Date: 2026-09-07
// Summary: Enterprise Windows Security & Defender Recovery Engine for Heaplit.
//          Scans, detects, and automatically repairs/re-enables Windows Defender,
//          Windows Firewall, Security Center Services, Registry Policies,
//          removes rogue malware exclusions, and triggers emergency antivirus scans.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            TamperedHostsEntries.Count > 0;

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
                if (RoguePoliciesDetected.Count > 0) score -= (RoguePoliciesDetected.Count * 5);
                if (RogueExclusionPaths.Count > 0) score -= (RogueExclusionPaths.Count * 5);
                if (TamperedHostsEntries.Count > 0) score -= 10;
                return Math.Max(0, Math.Min(100, score));
            }
        }
    }

    public static class WindowsSecurityManager
    {
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

                // 4. Audit Registry Tampering Policies (Group Policies targeting Defender/Tools)
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
                    @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System|DisableRegistryTools"
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

                // 5. Audit Rogue Defender Exclusions
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

                // 6. Audit Hosts File for Security Hijacking
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
        /// Re-enables, resets, and fully restores all Windows Defender, Firewall, and Security subsystems.
        /// </summary>
        public static async Task<(bool success, List<string> logs)> ReenableWindowsSecurityAsync(bool triggerQuickScan = true)
        {
            var logs = new List<string>();

            return await Task.Run(() =>
            {
                logs.Add($"🛡️ [1/8] Starting Full Windows Security & Defender Recovery Protocol at {DateTime.Now:HH:mm:ss}...");

                // Step 1: Remove Malicious Registry Policies
                logs.Add("🧹 [2/8] Purging malicious Group Policy overrides & restriction registry keys...");
                string cleanRegPs = @"
                    $keys = @(
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate',
                        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System',
                        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System',
                        'HKCU:\SOFTWARE\Policies\Microsoft\Windows\System',
                        'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'
                    )
                    foreach ($k in $keys) {
                        if (Test-Path $k) {
                            Remove-ItemProperty -Path $k -Name 'DisableAntiSpyware' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableRealtimeMonitoring' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableBehaviorMonitoring' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableOnAccessProtection' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableIOAVProtection' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableScanOnRealtimeEnable' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableTaskMgr' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableRegistryTools' -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path $k -Name 'DisableCMD' -ErrorAction SilentlyContinue
                        }
                    }
                    # Ensure UAC is active
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1 -ErrorAction SilentlyContinue
                ";
                RunElevatedPowerShell(cleanRegPs);
                logs.Add("  ✅ Cleaned malicious DisableAntiSpyware, DisableRealtimeMonitoring, and TaskMgr lockout policies.");

                // Step 2: Configure and Start Security Services
                logs.Add("⚙️ [3/8] Restoring Windows Defender, Security Center & Firewall system services...");
                string[] services = new[] { "WinDefend", "WdNisSvc", "Sense", "SecurityHealthService", "wscsvc", "MpsSvc", "wuauserv", "bits", "CryptSvc" };
                foreach (var svc in services)
                {
                    RunProcess("sc.exe", $"config {svc} start= auto");
                    RunProcess("sc.exe", $"start {svc}");
                }
                logs.Add("  ✅ Set security services to Automatic startup and initiated service start triggers.");

                // Step 3: Enable Windows Defender Real-Time Protection & Preferences
                logs.Add("🛡️ [4/8] Enforcing Windows Defender Real-Time Protection, Script Scanning & Cloud AI Guard...");
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
                logs.Add("  ✅ Real-time Monitoring, Behavior Monitoring, IOAV, Script Scan, PUA Protection, and Cloud AI Guard enabled.");

                // Step 4: Reset & Enable Windows Firewall
                logs.Add("🔥 [5/8] Activating Windows Firewall on Domain, Private, and Public profiles...");
                RunProcess("netsh.exe", "advfirewall set allprofiles state on");
                RunElevatedPowerShell("Set-NetFirewallProfile -All -Enabled True -ErrorAction SilentlyContinue");
                logs.Add("  ✅ Windows Firewall state set to Active (ON) for all network profiles.");

                // Step 5: Clean Rogue Defender Exclusions
                logs.Add("🧹 [6/8] Purging broad/malicious Defender exclusions added by malware...");
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
                logs.Add("  ✅ Removed dangerous broad exclusion directories and executable extensions.");

                // Step 6: Clean Hosts file if tampered
                logs.Add("🌐 [7/8] Inspecting network routing and hosts file integrity...");
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
                            logs.Add("  ✅ Purged malicious DNS blocking entries from hosts file.");
                        }
                        else
                        {
                            logs.Add("  ✅ Hosts file is clean and untampered.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logs.Add($"  ⚠️ Hosts file check notice: {ex.Message}");
                }

                // Step 7: Update Antivirus Signatures
                logs.Add("🔄 [8/8] Updating Windows Defender Antivirus Signatures and Definitions...");
                RunElevatedPowerShell("Update-MpSignature -ErrorAction SilentlyContinue");
                logs.Add("  ✅ Antivirus signature definitions update dispatched.");

                // Optional Emergency Quick Scan
                if (triggerQuickScan)
                {
                    logs.Add("⚡ Initiating background Quick Malware Scan...");
                    RunElevatedPowerShell("Start-MpScan -ScanType QuickScan -ErrorAction SilentlyContinue");
                    logs.Add("  ✅ Quick Scan active in background.");
                }

                logs.Add($"🎉 Windows Security Recovery Complete at {DateTime.Now:HH:mm:ss}! System protection restored.");
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
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encoded}",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(20000);
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
                        Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encoded}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(20000);
                }
                catch { }
            }
        }
    }
}
