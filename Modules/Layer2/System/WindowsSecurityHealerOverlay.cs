// Developer: heaplyn
// Date: 2026-09-07
// Summary: Interactive Glassmorphic Overlay for Windows Defender & Security Healing.
//          Audits real-time security state, repairs registry locks via elevated PowerShell as Administrator,
//          repairs broken SecHealthUI AppX & "You'll need a new app to open this windowsdefender link" errors,
//          cleans malware exclusions, restarts security services, downloads fresh official Defender packages & MSERT from Microsoft,
//          re-enables Firewall, and triggers emergency antivirus scans.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class WindowsSecurityHealerOverlay : BaseOverlay
    {
        private static WindowsSecurityHealerOverlay? _instance;

        private ProgressBar _healthScoreBar = null!;
        private OutlinedText _healthScoreText = null!;
        private OutlinedText _healthStatusSummary = null!;

        private StackPanel _statusCardsPanel = null!;
        private TextBox _logConsoleBox = null!;
        private Button _btnRestoreAll = null!;
        private Button _btnFixSecHealthUi = null!;
        private Button _btnReinstallOnline = null!;
        private Button _btnMsertScanner = null!;
        private Button _btnFixRegistry = null!;
        private Button _btnFixServices = null!;
        private Button _btnDismSfc = null!;
        private Button _btnAudit = null!;
        private Button _btnClearExclusions = null!;
        private Button _btnUpdateSigs = null!;
        private Button _btnQuickScan = null!;
        private Button _btnOpenDefender = null!;

        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded)
                {
                    _instance = new WindowsSecurityHealerOverlay();
                    _instance.Show();
                }
                else
                {
                    _instance.Activate();
                    _instance.BringToFront();
                }
            });
        }

        private WindowsSecurityHealerOverlay()
            : base("🛡️ HEAPLIT WINDOWS SECURITY HEALER & MALWARE RECOVERY", width: 980, height: 740)
        {
            this.Closed += (s, e) => _instance = null;

            var rootGrid = new Grid { Margin = new Thickness(8) };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Health Banner
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Actions
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content (Status cards & Logs)

            // 1. Health Banner
            rootGrid.Children.Add(BuildHealthBanner());

            // 2. Action Toolbar
            var actionToolbar = BuildActionToolbar();
            Grid.SetRow(actionToolbar, 1);
            rootGrid.Children.Add(actionToolbar);

            // 3. Tab Control for Cards & Logs
            var tabControl = new TabControl { Margin = new Thickness(0, 10, 0, 0) };
            StyleTabControl(tabControl);

            tabControl.Items.Add(new TabItem
            {
                Header = "🔍 Live Security Vector Cards",
                Content = BuildCardsTab()
            });

            tabControl.Items.Add(new TabItem
            {
                Header = "📜 Operation & Remediation Logs",
                Content = BuildLogsTab()
            });

            Grid.SetRow(tabControl, 2);
            rootGrid.Children.Add(tabControl);

            this.UserContent = rootGrid;

            // Trigger initial audit on open
            _ = RunLiveAuditAsync();
        }

        private UIElement BuildHealthBanner()
        {
            var bannerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) });

            var infoStack = new StackPanel();
            infoStack.Children.Add(CreateHeader("🛡️ Windows Security Posture & Healing Hub", category: "Headers"));

            _healthStatusSummary = new OutlinedText
            {
                Text = "Analyzing real-time Windows Defender, Firewall, and system policies...",
                Category = "Subtext",
                FontSize = 11,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 4, 0, 0)
            };
            infoStack.Children.Add(_healthStatusSummary);
            grid.Children.Add(infoStack);

            var scoreStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _healthScoreText = new OutlinedText
            {
                Text = "Score: -- / 100",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = Brushes.Cyan,
                Margin = new Thickness(0, 0, 0, 4)
            };
            scoreStack.Children.Add(_healthScoreText);

            _healthScoreBar = new ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Foreground = Brushes.Cyan,
                BorderThickness = new Thickness(0)
            };
            scoreStack.Children.Add(_healthScoreBar);

            Grid.SetColumn(scoreStack, 1);
            grid.Children.Add(scoreStack);

            bannerBorder.Child = grid;
            return bannerBorder;
        }

        private UIElement BuildActionToolbar()
        {
            var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };

            _btnRestoreAll = CreateStyledButton("⚡ 1-Click Restore All Security", async (s, e) => await ExecuteFullRestoreAsync(), isPrimary: true);
            _btnRestoreAll.ToolTip = "Purges rogue malware registry policies & IFEO hooks, fixes service start types, resets Defender & Firewall, and triggers scan.";

            _btnFixSecHealthUi = CreateStyledButton("🩹 Fix \"You'll need a new app\" (SecHealthUI)", async (s, e) => await FixSecHealthUiAppxAsync(), isPrimary: true);
            _btnFixSecHealthUi.ToolTip = "Re-registers SecHealthUI, VCLibs, and UI.Xaml AppX dependencies and fixes the windowsdefender: protocol association.";

            _btnReinstallOnline = CreateStyledButton("🌐 Reinstall Defender (Online Download)", async (s, e) => await DownloadAndReinstallDefenderOnlineAsync(), isPrimary: true);
            _btnReinstallOnline.ToolTip = "Downloads official Microsoft SecurityHealthSetup.exe and mpam-fe.exe antimalware engine directly from Microsoft CDN.";

            _btnMsertScanner = CreateStyledButton("🛡️ Microsoft Safety Scanner (MSERT)", async (s, e) => await DownloadAndRunMsertAsync());
            _btnMsertScanner.ToolTip = "Downloads and runs Microsoft Emergency Safety Scanner standalone tool directly from Microsoft.";

            _btnFixRegistry = CreateStyledButton("🔑 Fix Registry (Admin PowerShell)", async (s, e) => await FixRegistryPoliciesAsync());
            _btnFixRegistry.ToolTip = "Executes elevated PowerShell as Administrator to purge DisableAntiSpyware, TaskMgr lockouts, IFEO hooks, and WSUS hijacking.";

            _btnFixServices = CreateStyledButton("🔧 Fix Disabled Services", async (s, e) => await FixServicesAndIfeoAsync());
            _btnFixServices.ToolTip = "Un-disables WinDefend and wuauserv services in Registry and starts them.";

            _btnDismSfc = CreateStyledButton("🔍 DISM / SFC Repair", async (s, e) => await RunDismSfcRepairAsync());
            _btnDismSfc.ToolTip = "Runs DISM /Online /Cleanup-Image /RestoreHealth and sfc /scannow to fix corrupted Windows system files.";

            _btnAudit = CreateStyledButton("🔍 Re-Audit Security", async (s, e) => await RunLiveAuditAsync());
            _btnAudit.ToolTip = "Scans all security subsystems and updates the status cards.";

            _btnClearExclusions = CreateStyledButton("🧹 Clean Rogue Exclusions", async (s, e) => await ClearExclusionsAsync());
            _btnClearExclusions.ToolTip = "Clears all Defender exclusion paths and processes created by malware.";

            _btnUpdateSigs = CreateStyledButton("🔄 Update Signatures", async (s, e) => await UpdateSignaturesAsync());
            _btnUpdateSigs.ToolTip = "Forces Microsoft Defender to fetch the latest virus & malware definitions.";

            _btnQuickScan = CreateStyledButton("🚀 Run Quick Scan", async (s, e) => await TriggerQuickScanAsync());
            _btnQuickScan.ToolTip = "Starts a background Windows Defender Quick Malware Scan.";

            _btnOpenDefender = CreateStyledButton("🛡️ Open Defender App", async (s, e) => await OpenWindowsDefenderAppAsync());
            _btnOpenDefender.ToolTip = "Launches the official Windows Security Control Center with intelligent multi-tiered fallback.";

            wrap.Children.Add(_btnRestoreAll);
            wrap.Children.Add(_btnFixSecHealthUi);
            wrap.Children.Add(_btnReinstallOnline);
            wrap.Children.Add(_btnMsertScanner);
            wrap.Children.Add(_btnFixRegistry);
            wrap.Children.Add(_btnFixServices);
            wrap.Children.Add(_btnDismSfc);
            wrap.Children.Add(_btnAudit);
            wrap.Children.Add(_btnClearExclusions);
            wrap.Children.Add(_btnUpdateSigs);
            wrap.Children.Add(_btnQuickScan);
            wrap.Children.Add(_btnOpenDefender);

            return wrap;
        }

        private UIElement BuildCardsTab()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _statusCardsPanel = new StackPanel { Margin = new Thickness(4) };

            _statusCardsPanel.Children.Add(new OutlinedText
            {
                Text = "Loading security components...",
                Category = "Subtext",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(4)
            });

            scroll.Content = _statusCardsPanel;
            return scroll;
        }

        private UIElement BuildLogsTab()
        {
            var grid = new Grid { Margin = new Thickness(4) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var logHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var clearLogBtn = CreateStyledButton("Clear Output", (s, e) => _logConsoleBox.Text = string.Empty, isPrimary: false, fontSize: 10);
            clearLogBtn.HorizontalAlignment = HorizontalAlignment.Right;
            logHeader.Children.Add(clearLogBtn);
            logHeader.Children.Add(CreateLabel("Real-Time Security Activity & Remediation Stream:", 11, true));
            grid.Children.Add(logHeader);

            _logConsoleBox = new TextBox
            {
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromArgb(60, 10, 10, 20)),
                Foreground = Brushes.LightGreen,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Padding = new Thickness(8),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_logConsoleBox, 1);
            grid.Children.Add(_logConsoleBox);

            return grid;
        }

        private void AppendLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_logConsoleBox != null)
                {
                    _logConsoleBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                    _logConsoleBox.ScrollToEnd();
                }
            });
        }

        private async Task RunLiveAuditAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("Starting comprehensive Windows Security audit...");

            try
            {
                var audit = await WindowsSecurityManager.AuditSecurityStatusAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateHealthDisplay(audit);
                    RenderCards(audit);
                });

                AppendLog($"Audit completed. System Health Score: {audit.HealthScore}/100. Compromised: {audit.IsSystemCompromised}");
                foreach (var msg in audit.LogMessages)
                {
                    AppendLog(msg);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error running audit: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void UpdateHealthDisplay(SecurityAuditResult audit)
        {
            int score = audit.HealthScore;
            _healthScoreBar.Value = score;
            _healthScoreText.Text = $"Score: {score} / 100";

            if (score >= 85)
            {
                _healthScoreBar.Foreground = Brushes.LimeGreen;
                _healthScoreText.Foreground = Brushes.LimeGreen;
                _healthStatusSummary.Text = "✅ Windows Security is fully armed, real-time protection is active, and no tampering detected.";
                _healthStatusSummary.Foreground = Brushes.LightGreen;
            }
            else if (score >= 50)
            {
                _healthScoreBar.Foreground = Brushes.Gold;
                _healthScoreText.Foreground = Brushes.Gold;
                _healthStatusSummary.Text = "⚠️ Warning: Some security services, AppX packages, or firewall profiles are degraded or disabled.";
                _healthStatusSummary.Foreground = Brushes.Yellow;
            }
            else
            {
                _healthScoreBar.Foreground = Brushes.Tomato;
                _healthScoreText.Foreground = Brushes.Tomato;
                _healthStatusSummary.Text = "🚨 CRITICAL: Windows Security features disabled or malware rogue policies detected!";
                _healthStatusSummary.Foreground = Brushes.Tomato;
            }
        }

        private void RenderCards(SecurityAuditResult audit)
        {
            _statusCardsPanel.Children.Clear();

            // 1. Antivirus & Realtime Protection Card
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "🛡️ Windows Defender Antivirus & Real-Time Monitoring",
                audit.RealTimeProtectionEnabled && audit.AntivirusEnabled,
                new List<(string, bool, string)>
                {
                    ("Real-Time Protection", audit.RealTimeProtectionEnabled, "Live continuous filesystem & process monitoring"),
                    ("Antivirus Core Engine", audit.AntivirusEnabled, "Microsoft Defender core AV service status"),
                    ("Behavior Monitoring", audit.BehaviorMonitorEnabled, "Active heuristics & malicious behavior tracking"),
                    ("IOAV Download Scan", audit.IoavProtectionEnabled, "Scans downloaded attachments & files"),
                    ("Script Scanning", audit.ScriptScanningEnabled, "AMSI script & PowerShell inspection"),
                    ("Cloud-Delivered AI Protection", audit.CloudProtectionEnabled, "Next-gen Microsoft cloud telemetry & instant blocking")
                },
                extraInfo: $"Signature Version: {audit.SignatureVersion}"
            ));

            // 2. Windows Security App & AppX Package Card
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "📦 Windows Security App (SecHealthUI) & Protocol",
                audit.SecHealthUiAppxRegistered,
                new List<(string, bool, string)>
                {
                    ("Microsoft.SecHealthUI AppX", audit.SecHealthUiAppxRegistered, "Handles 'windowsdefender:' protocol and Security Center GUI"),
                    ("AppX Dependencies", audit.SecHealthUiAppxRegistered, "Microsoft.VCLibs and Microsoft.UI.Xaml runtime bridges")
                },
                extraInfo: audit.SecHealthUiAppxRegistered ? "AppX package registered" : "Missing / Unregistered -> Causes 'You'll need a new app' popup"
            ));

            // 3. Windows Firewall Card
            bool firewallAll = audit.FirewallDomainEnabled && audit.FirewallPrivateEnabled && audit.FirewallPublicEnabled;
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "🔥 Windows Defender Firewall Profiles",
                firewallAll,
                new List<(string, bool, string)>
                {
                    ("Domain Network Firewall", audit.FirewallDomainEnabled, "Protects active domain connections"),
                    ("Private Network Firewall", audit.FirewallPrivateEnabled, "Protects trusted home/work networks"),
                    ("Public Network Firewall", audit.FirewallPublicEnabled, "Protects public Wi-Fi & untrusted endpoints")
                }
            ));

            // 4. Security Services Card
            bool servicesAll = audit.DefenderServiceRunning && audit.FirewallServiceRunning && audit.SecurityCenterServiceRunning && audit.WindowsUpdateServiceRunning;
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "⚙️ Security & System Services",
                servicesAll,
                new List<(string, bool, string)>
                {
                    ("WinDefend (Defender Service)", audit.DefenderServiceRunning, "Core Windows Defender process"),
                    ("MpsSvc (Firewall Service)", audit.FirewallServiceRunning, "Windows Defender Firewall daemon"),
                    ("wscsvc (Security Center)", audit.SecurityCenterServiceRunning, "Windows Security Center health telemetry"),
                    ("wuauserv (Windows Update)", audit.WindowsUpdateServiceRunning, "Required for definition updates")
                }
            ));

            // 5. Registry Policy Tampering Card
            bool policiesClean = audit.RoguePoliciesDetected.Count == 0;
            var policyDetails = new List<(string, bool, string)>();
            if (policiesClean)
            {
                policyDetails.Add(("Group Policy Locks", true, "No malicious DisableAntiSpyware or TaskMgr lockout policies found."));
            }
            else
            {
                foreach (var p in audit.RoguePoliciesDetected)
                {
                    policyDetails.Add(($"Lock: {p}", false, "Malware-enforced restriction disabling security tools"));
                }
            }
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "🧹 Malicious Group Policy & Registry Locks",
                policiesClean,
                policyDetails
            ));

            // 6. IFEO Debugger Hijacking Card
            bool ifeoClean = audit.HijackedIfeoProcesses.Count == 0;
            var ifeoDetails = new List<(string, bool, string)>();
            if (ifeoClean)
            {
                ifeoDetails.Add(("IFEO Process Hooks", true, "No Image File Execution Options debugger hooks blocking Defender / Tools."));
            }
            else
            {
                foreach (var hook in audit.HijackedIfeoProcesses)
                {
                    ifeoDetails.Add(($"Hijack: {hook}", false, "Malware IFEO hook preventing process launch"));
                }
            }
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "🪝 Process Execution & IFEO Integrity",
                ifeoClean,
                ifeoDetails
            ));

            // 7. Rogue Exclusions Card
            bool exclusionsClean = audit.RogueExclusionPaths.Count == 0 && audit.RogueExclusionProcesses.Count == 0;
            var exclusionDetails = new List<(string, bool, string)>();
            if (exclusionsClean)
            {
                exclusionDetails.Add(("Defender Exclusions", true, "No broad malware whitelist paths or rogue processes detected."));
            }
            else
            {
                foreach (var path in audit.RogueExclusionPaths)
                {
                    exclusionDetails.Add(($"Path: {path}", false, "Whitelisted directory bypassing antivirus"));
                }
                foreach (var proc in audit.RogueExclusionProcesses)
                {
                    exclusionDetails.Add(($"Process: {proc}", false, "Whitelisted executable bypassing antivirus"));
                }
            }
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "🚨 Defender Whitelist & Exclusions",
                exclusionsClean,
                exclusionDetails
            ));

            // 8. Network & Hosts File Card
            bool hostsClean = audit.TamperedHostsEntries.Count == 0;
            var hostsDetails = new List<(string, bool, string)>();
            if (hostsClean)
            {
                hostsDetails.Add(("Hosts File Integrity", true, "No antivirus/update domain redirection or blocking detected."));
            }
            else
            {
                foreach (var entry in audit.TamperedHostsEntries)
                {
                    hostsDetails.Add(($"Blocked: {entry}", false, "Redirecting security updates or telemetry"));
                }
            }
            _statusCardsPanel.Children.Add(CreateVectorCard(
                "🌐 Hosts File & DNS Integrity",
                hostsClean,
                hostsDetails
            ));
        }

        private UIElement CreateVectorCard(string title, bool isHealthy, List<(string name, bool ok, string desc)> items, string? extraInfo = null)
        {
            var cardBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 20, 24, 36)),
                BorderBrush = isHealthy
                    ? new SolidColorBrush(Color.FromArgb(70, 34, 197, 94))
                    : new SolidColorBrush(Color.FromArgb(120, 239, 68, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stack = new StackPanel();

            // Header line
            var headerDock = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var badge = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Background = isHealthy ? new SolidColorBrush(Color.FromArgb(60, 34, 197, 94)) : new SolidColorBrush(Color.FromArgb(80, 239, 68, 68)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            badge.Child = new TextBlock
            {
                Text = isHealthy ? "SECURE" : "ATTENTION",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = isHealthy ? Brushes.LightGreen : Brushes.OrangeRed
            };
            DockPanel.SetDock(badge, Dock.Right);
            headerDock.Children.Add(badge);

            var titleText = new OutlinedText
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = isHealthy ? Brushes.LightGreen : Brushes.Tomato
            };
            headerDock.Children.Add(titleText);
            stack.Children.Add(headerDock);

            // Extra info if any
            if (!string.IsNullOrEmpty(extraInfo))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = extraInfo,
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 6)
                });
            }

            // Items grid/stack
            foreach (var (name, ok, desc) in items)
            {
                var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };

                var statusIcon = new TextBlock
                {
                    Text = ok ? "✅" : "❌",
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                row.Children.Add(statusIcon);

                var itemText = new TextBlock
                {
                    Text = $"{name} — {desc}",
                    FontSize = 11,
                    Foreground = ok ? Brushes.Gainsboro : Brushes.OrangeRed,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                row.Children.Add(itemText);

                stack.Children.Add(row);
            }

            cardBorder.Child = stack;
            return cardBorder;
        }

        private async Task ExecuteFullRestoreAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("⚡ STARTING FULL 1-CLICK WINDOWS SECURITY REMEDIATION...");

            try
            {
                var (success, logs) = await WindowsSecurityManager.ReenableWindowsSecurityAsync(triggerQuickScan: true, liveLog: AppendLog);
                if (success)
                {
                    AppendLog("🎉 Windows Security Restoration applied successfully! Re-auditing in 2 seconds...");
                    await Task.Delay(2000);
                    await RunLiveAuditAsync();
                }
                else
                {
                    AppendLog("⚠️ Remediation encountered issues. Check logs above.");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error executing restoration: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task FixSecHealthUiAppxAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🩹 REPAIRING SECHEALTHUI & 'YOU'LL NEED A NEW APP' ERROR...");

            try
            {
                var (ok, logs) = await WindowsSecurityManager.RepairWindowsSecurityAppXAsync(AppendLog);
                AppendLog(ok ? "🎉 SecHealthUI and protocol registration repaired successfully!" : "⚠️ Repair completed with notices.");
                await Task.Delay(2000);
                await RunLiveAuditAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error repairing SecHealthUI: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task FixRegistryPoliciesAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🔑 EXECUTING ADMINISTRATOR POWERSHELL REGISTRY POLICY OVERHAUL...");

            try
            {
                var (ok, logs) = await WindowsSecurityManager.FixAllRegistryPoliciesElevatedAsync(AppendLog);
                AppendLog(ok ? "✅ Registry policies fixed successfully as Administrator!" : "⚠️ Registry repair completed with notices.");
                await Task.Delay(2000);
                await RunLiveAuditAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error repairing registry: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task DownloadAndReinstallDefenderOnlineAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🌐 STARTING ONLINE MICROSOFT DEFENDER REINSTALLATION PROTOCOL...");

            try
            {
                // 1. Download and run SecurityHealthSetup
                AppendLog("Phase 1/3: Downloading official Microsoft SecurityHealthSetup.exe...");
                var (appOk, appMsg) = await WindowsSecurityManager.DownloadAndReinstallDefenderAppAsync(AppendLog);
                AppendLog($"SecurityHealthSetup result: {appMsg}");

                // 2. Download and install mpam-fe antimalware engine
                AppendLog("Phase 2/3: Downloading official Microsoft Antimalware Engine & Definitions...");
                var (engOk, engMsg) = await WindowsSecurityManager.DownloadAndReinstallAntimalwareEngineAsync(AppendLog);
                AppendLog($"Antimalware engine result: {engMsg}");

                // 3. Run full remediation
                AppendLog("Phase 3/3: Restoring services, AppX packages and policies...");
                await WindowsSecurityManager.FixServicePermissionsAndStartupAsync(AppendLog);
                await WindowsSecurityManager.RepairWindowsSecurityAppXAsync(AppendLog);

                AppendLog("🎉 Online Microsoft Defender Reinstallation finished! Re-auditing in 2 seconds...");
                await Task.Delay(2000);
                await RunLiveAuditAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error in online reinstallation: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task DownloadAndRunMsertAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🛡️ DOWNLOADING & LAUNCHING MICROSOFT SAFETY SCANNER (MSERT)...");

            try
            {
                var (ok, msg) = await WindowsSecurityManager.DownloadAndRunMsertScannerAsync(quiet: false, AppendLog);
                AppendLog(ok ? "✅ Microsoft Safety Scanner launched successfully!" : $"⚠️ Failed to launch MSERT: {msg}");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error launching MSERT: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task FixServicesAndIfeoAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🔧 Un-disabling services and stripping IFEO hooks via Administrator PowerShell...");

            try
            {
                bool ok = await WindowsSecurityManager.FixServicePermissionsAndStartupAsync(AppendLog);
                AppendLog(ok ? "✅ Service configurations updated and services started." : "⚠️ Failed to configure services.");
                await Task.Delay(2000);
                await RunLiveAuditAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error fixing services: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task RunDismSfcRepairAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🔍 STARTING DISM & SFC SYSTEM IMAGE REPAIR...");

            try
            {
                var (ok, msg) = await WindowsSecurityManager.RunDismAndSfcRepairAsync(AppendLog);
                AppendLog(ok ? "✅ System component store repair dispatched." : $"⚠️ DISM/SFC notice: {msg}");
                await RunLiveAuditAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error running DISM/SFC: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task ClearExclusionsAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🧹 Purging all Windows Defender exclusions...");

            try
            {
                bool ok = await WindowsSecurityManager.ClearAllExclusionsAsync();
                AppendLog(ok ? "✅ All exclusions cleared successfully." : "⚠️ Failed to clear exclusions.");
                await Task.Delay(1000);
                await RunLiveAuditAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error clearing exclusions: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task UpdateSignaturesAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🔄 Dispatched Antivirus Signatures update request...");

            try
            {
                bool ok = await WindowsSecurityManager.UpdateSignaturesAsync();
                AppendLog(ok ? "✅ Signature update initiated." : "⚠️ Failed to update signatures.");
                await Task.Delay(2000);
                await RunLiveAuditAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error updating signatures: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task TriggerQuickScanAsync()
        {
            SetButtonsEnabled(false);
            AppendLog("🚀 Dispatching Windows Defender Quick Scan...");

            try
            {
                bool ok = await WindowsSecurityManager.StartQuickScanAsync();
                AppendLog(ok ? "✅ Quick Scan active in background." : "⚠️ Failed to start scan.");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error starting scan: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task OpenWindowsDefenderAppAsync()
        {
            AppendLog("Attempting multi-tiered launch of Windows Defender Security Center...");

            // Method 1: Try Direct SecurityHealthHost in System32\SecurityHealth
            try
            {
                string shBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealth");
                if (Directory.Exists(shBase))
                {
                    var hostExes = Directory.GetFiles(shBase, "SecurityHealthHost.exe", SearchOption.AllDirectories);
                    if (hostExes.Length > 0)
                    {
                        string latestHost = hostExes.OrderByDescending(f => f).First();
                        AppendLog($"Launching via SecurityHealthHost: {latestHost}...");
                        Process.Start(new ProcessStartInfo { FileName = latestHost, UseShellExecute = true });
                        return;
                    }
                }
            }
            catch { }

            // Method 2: Try windowsdefender: URL protocol
            try
            {
                AppendLog("Launching via windowsdefender: URL protocol...");
                Process.Start(new ProcessStartInfo { FileName = "windowsdefender:", UseShellExecute = true });
                return;
            }
            catch { }

            // Method 3: Try ms-settings:windowsdefender
            try
            {
                AppendLog("Launching via ms-settings:windowsdefender...");
                Process.Start(new ProcessStartInfo { FileName = "ms-settings:windowsdefender", UseShellExecute = true });
                return;
            }
            catch { }

            // Method 4: If all fail, auto-trigger SecHealthUI repair
            AppendLog("⚠️ All direct launch mechanisms failed. Auto-triggering SecHealthUI AppX repair...");
            await FixSecHealthUiAppxAsync();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_btnRestoreAll != null) _btnRestoreAll.IsEnabled = enabled;
                if (_btnFixSecHealthUi != null) _btnFixSecHealthUi.IsEnabled = enabled;
                if (_btnReinstallOnline != null) _btnReinstallOnline.IsEnabled = enabled;
                if (_btnMsertScanner != null) _btnMsertScanner.IsEnabled = enabled;
                if (_btnFixRegistry != null) _btnFixRegistry.IsEnabled = enabled;
                if (_btnFixServices != null) _btnFixServices.IsEnabled = enabled;
                if (_btnDismSfc != null) _btnDismSfc.IsEnabled = enabled;
                if (_btnAudit != null) _btnAudit.IsEnabled = enabled;
                if (_btnClearExclusions != null) _btnClearExclusions.IsEnabled = enabled;
                if (_btnUpdateSigs != null) _btnUpdateSigs.IsEnabled = enabled;
                if (_btnQuickScan != null) _btnQuickScan.IsEnabled = enabled;
            });
        }
    }
}
