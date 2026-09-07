// Developer: heaplyn
// Date: 2026-09-07
// Summary: Command Handler for Windows Defender & Security Remediation.
//          Enables quick search triggers for security audit, malware recovery,
//          administrator PowerShell registry fixing, downloading official Defender from Microsoft,
//          running Microsoft Safety Scanner (MSERT), and purging rogue exclusions.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class WindowsSecurityCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLowerInvariant();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string first = parts[0];

            return query.StartsWith("sec") ||
                   query.StartsWith("defend") ||
                   query.StartsWith("firewall") ||
                   query.StartsWith("antivirus") ||
                   query.StartsWith("malware") ||
                   query.StartsWith("fixsec") ||
                   query.StartsWith("fixreg") ||
                   query.StartsWith("fixregistry") ||
                   query.StartsWith("restoresec") ||
                   query.StartsWith("reenablesec") ||
                   query.StartsWith("msert") ||
                   query.StartsWith("safetyscan") ||
                   query.StartsWith("downloaddef") ||
                   query.StartsWith("reinstalldef") ||
                   SearchUtil.IsClose(first, "security") ||
                   SearchUtil.IsClose(first, "defender") ||
                   SearchUtil.IsClose(first, "antivirus") ||
                   SearchUtil.IsClose(first, "firewall") ||
                   SearchUtil.IsClose(first, "malware") ||
                   SearchUtil.IsClose(first, "registry") ||
                   SearchUtil.IsClose(first, "msert");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var results = new List<CommandResult>();
            query = query.Trim().ToLowerInvariant();

            double baseSim = 4.5;
            if (query.Contains("fix") || query.Contains("restore") || query.Contains("reenable") || query.Contains("malware") || query.Contains("download") || query.Contains("reinstall") || query.Contains("reg"))
            {
                baseSim = 5.5;
            }

            // 1. Primary Healer Hub
            results.Add(new CommandResult
            {
                TITLE = "🛡️ Open Windows Security Healer Hub",
                DESCRIPTION = "Interactive live audit & remediation console for Windows Defender, Firewall, and rogue policies",
                SIMILARITY = baseSim + 1.0,
                EXECUTE = () => WindowsSecurityHealerOverlay.ShowOverlay()
            });

            // 2. Fix Registry as Administrator via PowerShell
            results.Add(new CommandResult
            {
                TITLE = "🔑 Fix Registry Policies (Admin PowerShell)",
                DESCRIPTION = "Purge DisableAntiSpyware, TaskMgr lockouts, IFEO debugger hooks, and WSUS hijacking via elevated PowerShell",
                SIMILARITY = baseSim + 0.95,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.FixAllRegistryPoliciesElevatedAsync();
                }
            });

            // 3. Download & Reinstall Defender from Microsoft Cloud
            results.Add(new CommandResult
            {
                TITLE = "🌐 Download & Reinstall Microsoft Defender (Cloud)",
                DESCRIPTION = "Pull official SecurityHealthSetup.exe & antimalware engine installer directly from Microsoft CDN",
                SIMILARITY = baseSim + 0.9,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = Task.Run(async () =>
                    {
                        await WindowsSecurityManager.DownloadAndReinstallDefenderAppAsync();
                        await WindowsSecurityManager.DownloadAndReinstallAntimalwareEngineAsync();
                        await WindowsSecurityManager.FixServicePermissionsAndStartupAsync();
                    });
                }
            });

            // 4. Microsoft Emergency Safety Scanner (MSERT)
            results.Add(new CommandResult
            {
                TITLE = "🛡️ Run Microsoft Safety Scanner (MSERT)",
                DESCRIPTION = "Download and run standalone Microsoft emergency malware scanner (bypasses broken Defender services)",
                SIMILARITY = baseSim + 0.85,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.DownloadAndRunMsertScannerAsync(quiet: false);
                }
            });

            // 5. Immediate 1-Click Restore
            results.Add(new CommandResult
            {
                TITLE = "⚡ 1-Click Restore & Re-Enable Windows Security",
                DESCRIPTION = "Instantly purge malware registry locks, start WinDefend services, enable Firewall, and trigger quick scan",
                SIMILARITY = baseSim + 0.8,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.ReenableWindowsSecurityAsync(triggerQuickScan: true);
                }
            });

            // 6. Purge Rogue Defender Exclusions
            results.Add(new CommandResult
            {
                TITLE = "🧹 Purge Rogue Defender Exclusions",
                DESCRIPTION = "Remove folder & process whitelists injected by malware to hide from antivirus",
                SIMILARITY = baseSim + 0.4,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.ClearAllExclusionsAsync();
                }
            });

            // 7. Update Defender Antivirus Definitions
            results.Add(new CommandResult
            {
                TITLE = "🔄 Update Windows Defender Signatures",
                DESCRIPTION = "Force download latest Microsoft Defender virus & threat intelligence definitions",
                SIMILARITY = baseSim + 0.2,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.UpdateSignaturesAsync();
                }
            });

            // 8. Run Antivirus Quick Scan
            results.Add(new CommandResult
            {
                TITLE = "🚀 Run Defender Antivirus Quick Scan",
                DESCRIPTION = "Dispatch background heuristic malware and memory scan via Microsoft Defender",
                SIMILARITY = baseSim,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.StartQuickScanAsync();
                }
            });

            return results;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc
                {
                    COMMAND_NAME = "security / defender / fixsecurity / fixregistry / downloaddefender",
                    COMMAND_DESCRIPTION = "Audits, downloads, and re-enables Windows Defender, Firewall, and cleans malware registry tampering via Administrator PowerShell",
                    COMMAND_EXAMPLE = "fixregistry"
                }
            };
        }
    }
}
