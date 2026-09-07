// Developer: heaplyn
// Date: 2026-09-07
// Summary: Command Handler for Windows Defender & Security Remediation.
//          Enables quick search triggers for security audit, malware recovery,
//          fixing SecHealthUI / "You'll need a new app" error, administrator PowerShell registry fixing,
//          downloading official Defender from Microsoft, running MSERT, DISM/SFC repair, and purging rogue exclusions.

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
                   query.StartsWith("fixappx") ||
                   query.StartsWith("fixreg") ||
                   query.StartsWith("fixregistry") ||
                   query.StartsWith("restoresec") ||
                   query.StartsWith("reenablesec") ||
                   query.StartsWith("msert") ||
                   query.StartsWith("safetyscan") ||
                   query.StartsWith("dism") ||
                   query.StartsWith("sfc") ||
                   query.StartsWith("downloaddef") ||
                   query.StartsWith("reinstalldef") ||
                   query.Contains("stealth") ||
                   query.Contains("cloak") ||
                   query.Contains("disguise") ||
                   query.Contains("covert") ||
                   query.Contains("bios") ||
                   query.Contains("uefi") ||
                   query.Contains("flash") ||
                   query.Contains("usb") ||
                   query.Contains("restore") ||
                   query.Contains("recovery") ||
                   SearchUtil.IsClose(first, "security") ||
                   SearchUtil.IsClose(first, "defender") ||
                   SearchUtil.IsClose(first, "antivirus") ||
                   SearchUtil.IsClose(first, "firewall") ||
                   SearchUtil.IsClose(first, "malware") ||
                   SearchUtil.IsClose(first, "registry") ||
                   SearchUtil.IsClose(first, "stealth") ||
                   SearchUtil.IsClose(first, "bios") ||
                   SearchUtil.IsClose(first, "msert");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var results = new List<CommandResult>();
            query = query.Trim().ToLowerInvariant();

            double baseSim = 4.5;
            if (query.Contains("fix") || query.Contains("restore") || query.Contains("reenable") || query.Contains("malware") || query.Contains("download") || query.Contains("reinstall") || query.Contains("reg") || query.Contains("appx") || query.Contains("bios") || query.Contains("flash") || query.Contains("usb") || query.Contains("stealth"))
            {
                baseSim = 5.5;
            }

            // 0a. Stealth Reinstall Defender (Disguised Package & Binary Name)
            if (query.Contains("stealth") || query.Contains("cloak") || query.Contains("disguise") || query.Contains("covert") || query.Contains("name") || query.Contains("package") || query.Contains("reinstall") || SearchUtil.IsClose(query, "stealth"))
            {
                results.Add(new CommandResult
                {
                    TITLE = "🥷 Stealth Reinstall Defender (Disguised Package & Name)",
                    DESCRIPTION = "Reinstalls Defender under a decoy package name (AppHealthBroker) and cloaked binaries (WinSysBrokerHost.exe) so malware cannot find or kill it",
                    SIMILARITY = baseSim + 1.25,
                    EXECUTE = () =>
                    {
                        WindowsSecurityHealerOverlay.ShowOverlay();
                        _ = WindowsSecurityManager.DeployStealthDefenderCloakAsync();
                    }
                });
            }

            // 0b. Restart to BIOS (Restore with Flash Drive / USB Boot)
            if (query.Contains("bios") || query.Contains("flash") || query.Contains("usb") || query.Contains("restore") || query.Contains("recover") || query.Contains("boot") || SearchUtil.IsClose(query, "bios"))
            {
                results.Add(new CommandResult
                {
                    TITLE = "🔌 Restart to BIOS (Restore with Flash Drive / USB Boot)",
                    DESCRIPTION = "Reboot computer directly into motherboard BIOS / UEFI setup to select a USB flash drive or restore media",
                    SIMILARITY = baseSim + 1.2,
                    EXECUTE = () => PowerCommandHandler.TriggerBootToBios()
                });

                results.Add(new CommandResult
                {
                    TITLE = "🚀 Reboot to Advanced Startup (USB / Launch Options)",
                    DESCRIPTION = "Restart into Windows Recovery Environment (WinRE) to select 'Use a device' (USB Boot) or Startup Settings",
                    SIMILARITY = baseSim + 1.15,
                    EXECUTE = () => PowerCommandHandler.TriggerBootToLaunchOptions()
                });
            }

            // 1. Primary Healer Hub
            results.Add(new CommandResult
            {
                TITLE = "🛡️ Open Windows Security Healer Hub",
                DESCRIPTION = "Interactive live audit & remediation console for Windows Defender, Firewall, and rogue policies",
                SIMILARITY = baseSim + 1.0,
                EXECUTE = () => WindowsSecurityHealerOverlay.ShowOverlay()
            });

            // 2. Fix "You'll need a new app" (SecHealthUI AppX & Dependencies)
            results.Add(new CommandResult
            {
                TITLE = "🩹 Fix \"You'll need a new app to open windowsdefender\"",
                DESCRIPTION = "Re-register Microsoft.SecHealthUI, VCLibs, and UI.Xaml AppX packages and fix protocol handler",
                SIMILARITY = baseSim + 0.98,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.RepairWindowsSecurityAppXAsync();
                }
            });

            // 3. Fix Registry as Administrator via PowerShell
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

            // 4. Download & Reinstall Defender from Microsoft Cloud
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
                        await WindowsSecurityManager.RepairWindowsSecurityAppXAsync();
                    });
                }
            });

            // 5. Microsoft Emergency Safety Scanner (MSERT)
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

            // 6. DISM & SFC System File Repair
            results.Add(new CommandResult
            {
                TITLE = "🔍 Run DISM & SFC System Image Repair",
                DESCRIPTION = "Repair corrupted Windows system files and component store via DISM /Online /Cleanup-Image /RestoreHealth",
                SIMILARITY = baseSim + 0.82,
                EXECUTE = () =>
                {
                    WindowsSecurityHealerOverlay.ShowOverlay();
                    _ = WindowsSecurityManager.RunDismAndSfcRepairAsync();
                }
            });

            // 7. Immediate 1-Click Restore
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

            // 8. Purge Rogue Defender Exclusions
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

            // 9. Update Defender Antivirus Definitions
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

            // 10. Run Antivirus Quick Scan
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
                    COMMAND_NAME = "security / defender / fixsecurity / fixappx / fixregistry / downloaddefender",
                    COMMAND_DESCRIPTION = "Audits, downloads, and re-enables Windows Defender, repairs SecHealthUI AppX & protocol, fixes registry as Admin",
                    COMMAND_EXAMPLE = "fixappx"
                }
            };
        }
    }
}
