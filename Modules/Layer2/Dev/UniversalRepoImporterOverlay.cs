// Developer: heaplyn
// Date: 2026-09-07
// Summary: Universal Repository Importer & Smart Setup UI Overlay for Heaplit.
//          Provides full graphical interface for importing, analyzing, synthesizing configs,
//          installing dependencies, and launching projects from any Git host or local directory.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class UniversalRepoImporterOverlay : BaseOverlay
    {
        private static UniversalRepoImporterOverlay? _instance;

        private TextBox _repoTargetBox = null!;
        private TextBox _destPathBox = null!;
        private CheckBox _autoInstallDepsCheck = null!;
        private CheckBox _autoSynthesizeConfigsCheck = null!;
        private CheckBox _autoInstallSdksCheck = null!;
        private CheckBox _autoLaunchCheck = null!;

        private StackPanel _stackInfoPanel = null!;
        private WrapPanel _frameworksBadgePanel = null!;
        private StackPanel _synthesizedConfigsPanel = null!;
        private OutlinedText _primaryLangText = null!;
        private OutlinedText _runCommandText = null!;
        private TextBox _logConsoleBox = null!;

        private Button _btnImportSetup = null!;
        private Button _btnRunDev = null!;
        private Button _btnOpenVsCode = null!;
        private Button _btnOpenFolder = null!;
        private Button _btnOpenTerminal = null!;

        private RepoImportResult? _lastResult;

        public static void ShowOverlay(string? initialTarget = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded)
                {
                    _instance = new UniversalRepoImporterOverlay(initialTarget);
                    _instance.Show();
                }
                else
                {
                    _instance.Activate();
                    _instance.BringToFront();
                    if (!string.IsNullOrEmpty(initialTarget))
                    {
                        _instance._repoTargetBox.Text = initialTarget;
                    }
                }
            });
        }

        private UniversalRepoImporterOverlay(string? initialTarget = null)
            : base("🚀 UNIVERSAL REPO IMPORTER & SMART SETUP", width: 1000, height: 740)
        {
            this.Closed += (s, e) => _instance = null;

            var rootGrid = new Grid { Margin = new Thickness(10) };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input deck
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Options deck
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main content (Stack HUD & Logs)
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Action toolbar

            // 1. Input Deck
            rootGrid.Children.Add(BuildInputDeck(initialTarget));

            // 2. Options Deck
            var optionsDeck = BuildOptionsDeck();
            Grid.SetRow(optionsDeck, 1);
            rootGrid.Children.Add(optionsDeck);

            // 3. Main Content (Split Stack HUD & Logs)
            var contentSplit = BuildContentSplit();
            Grid.SetRow(contentSplit, 2);
            rootGrid.Children.Add(contentSplit);

            // 4. Action Toolbar
            var actionToolbar = BuildActionToolbar();
            Grid.SetRow(actionToolbar, 3);
            rootGrid.Children.Add(actionToolbar);

            this.UserContent = rootGrid;

            if (!string.IsNullOrEmpty(initialTarget))
            {
                _ = ExecuteImportAsync();
            }
        }

        private UIElement BuildInputDeck(string? initialTarget)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Target Repository / URL
            var targetGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160, GridUnitType.Pixel) });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var targetLabel = new OutlinedText
            {
                Text = "Repository Target:",
                Category = "Headers",
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            targetGrid.Children.Add(targetLabel);

            _repoTargetBox = new TextBox
            {
                Text = initialTarget ?? "",
                Background = new SolidColorBrush(Color.FromArgb(50, 10, 10, 20)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 56, 189, 248)),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 12,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            _repoTargetBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) _ = ExecuteImportAsync();
            };
            Grid.SetColumn(_repoTargetBox, 1);
            targetGrid.Children.Add(_repoTargetBox);

            _btnImportSetup = CreateStyledButton("⚡ Smart Import & Setup", async (s, e) => await ExecuteImportAsync(), isPrimary: true, fontSize: 11);
            Grid.SetColumn(_btnImportSetup, 2);
            targetGrid.Children.Add(_btnImportSetup);
            grid.Children.Add(targetGrid);

            // Row 1: Destination Path
            var destGrid = new Grid();
            destGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160, GridUnitType.Pixel) });
            destGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            destGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var destLabel = new OutlinedText
            {
                Text = "Destination Directory:",
                Category = "Subtext",
                FontSize = 11,
                Foreground = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };
            destGrid.Children.Add(destLabel);

            string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Projects");
            _destPathBox = new TextBox
            {
                Text = defaultDir,
                Background = new SolidColorBrush(Color.FromArgb(40, 10, 10, 20)),
                Foreground = Brushes.LightGray,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            Grid.SetColumn(_destPathBox, 1);
            destGrid.Children.Add(_destPathBox);

            var btnBrowse = CreateStyledButton("📂 Browse", (s, e) =>
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _destPathBox.Text = dlg.SelectedPath;
                }
            }, isPrimary: false, fontSize: 10);
            Grid.SetColumn(btnBrowse, 2);
            destGrid.Children.Add(btnBrowse);
            Grid.SetRow(destGrid, 1);
            grid.Children.Add(destGrid);

            border.Child = grid;
            return border;
        }

        private UIElement BuildOptionsDeck()
        {
            var wrap = new WrapPanel { Margin = new Thickness(4, 0, 4, 8) };

            _autoInstallDepsCheck = new CheckBox
            {
                Content = "Auto-Install Dependencies (npm/pip/dotnet/cargo)",
                IsChecked = true,
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 0, 16, 4)
            };
            wrap.Children.Add(_autoInstallDepsCheck);

            _autoSynthesizeConfigsCheck = new CheckBox
            {
                Content = "Synthesize Configs (.env, .vscode, .gitignore)",
                IsChecked = true,
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 0, 16, 4)
            };
            wrap.Children.Add(_autoSynthesizeConfigsCheck);

            _autoInstallSdksCheck = new CheckBox
            {
                Content = "Auto-Install Missing Host SDKs (via PowerShell)",
                IsChecked = true,
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 0, 16, 4)
            };
            wrap.Children.Add(_autoInstallSdksCheck);

            _autoLaunchCheck = new CheckBox
            {
                Content = "Launch VS Code After Setup",
                IsChecked = false,
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(0, 0, 16, 4)
            };
            wrap.Children.Add(_autoLaunchCheck);

            return wrap;
        }

        private UIElement BuildContentSplit()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Panel: Smart Stack & Environment HUD
            var leftBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 8, 0)
            };

            var leftScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _stackInfoPanel = new StackPanel();

            _stackInfoPanel.Children.Add(CreateHeader("🔍 Smart Tech Stack & Manifests", category: "Headers"));

            _primaryLangText = new OutlinedText
            {
                Text = "Primary Language: Awaiting repository input...",
                Category = "Subtext",
                FontSize = 11,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 4, 0, 6)
            };
            _stackInfoPanel.Children.Add(_primaryLangText);

            _stackInfoPanel.Children.Add(CreateLabel("Detected Frameworks & Tools:", 11, true));
            _frameworksBadgePanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 8) };
            _frameworksBadgePanel.Children.Add(CreateBadge("Awaiting Import", Brushes.Gray));
            _stackInfoPanel.Children.Add(_frameworksBadgePanel);

            _stackInfoPanel.Children.Add(CreateLabel("Synthesized Configurations:", 11, true));
            _synthesizedConfigsPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };
            _synthesizedConfigsPanel.Children.Add(new TextBlock { Text = "- .env / .vscode will be generated automatically", Foreground = Brushes.DarkGray, FontSize = 10 });
            _stackInfoPanel.Children.Add(_synthesizedConfigsPanel);

            _stackInfoPanel.Children.Add(CreateLabel("Detected Execution / Dev Command:", 11, true));
            _runCommandText = new OutlinedText
            {
                Text = "---",
                Category = "Headers",
                FontSize = 11,
                Foreground = Brushes.LightGreen,
                Margin = new Thickness(0, 2, 0, 8)
            };
            _stackInfoPanel.Children.Add(_runCommandText);

            leftScroll.Content = _stackInfoPanel;
            leftBorder.Child = leftScroll;
            grid.Children.Add(leftBorder);

            // Right Panel: Live Operations & Provisioning Log Stream
            var rightBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 10, 10, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8)
            };

            var rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var logHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            var clearBtn = CreateStyledButton("Clear Output", (s, e) => _logConsoleBox.Text = string.Empty, isPrimary: false, fontSize: 10);
            clearBtn.HorizontalAlignment = HorizontalAlignment.Right;
            logHeader.Children.Add(clearBtn);
            logHeader.Children.Add(CreateLabel("📜 Live Setup & PowerShell Stream:", 11, true));
            rightGrid.Children.Add(logHeader);

            _logConsoleBox = new TextBox
            {
                IsReadOnly = true,
                Background = Brushes.Transparent,
                Foreground = Brushes.LightGreen,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Padding = new Thickness(4),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_logConsoleBox, 1);
            rightGrid.Children.Add(_logConsoleBox);

            rightBorder.Child = rightGrid;
            Grid.SetColumn(rightBorder, 1);
            grid.Children.Add(rightBorder);

            return grid;
        }

        private UIElement BuildActionToolbar()
        {
            var wrap = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };

            _btnRunDev = CreateStyledButton("▶️ Launch Dev Server / App", (s, e) => LaunchProjectRunCommand(), isPrimary: true, fontSize: 11);
            _btnRunDev.IsEnabled = false;
            wrap.Children.Add(_btnRunDev);

            _btnOpenVsCode = CreateStyledButton("💻 Open in VS Code", (s, e) => OpenInVsCode(), isPrimary: false, fontSize: 11);
            _btnOpenVsCode.IsEnabled = false;
            wrap.Children.Add(_btnOpenVsCode);

            _btnOpenFolder = CreateStyledButton("📁 Open in Explorer", (s, e) => OpenInExplorer(), isPrimary: false, fontSize: 11);
            _btnOpenFolder.IsEnabled = false;
            wrap.Children.Add(_btnOpenFolder);

            _btnOpenTerminal = CreateStyledButton("🖥️ Open Terminal", (s, e) => OpenInTerminal(), isPrimary: false, fontSize: 11);
            _btnOpenTerminal.IsEnabled = false;
            wrap.Children.Add(_btnOpenTerminal);

            var btnFixSdks = CreateStyledButton("📦 Auto-Install Missing SDKs", async (s, e) =>
            {
                AppendLog("📦 Checking and installing missing developer runtimes via SystemPackageManager...");
                var progress = new Progress<string>(AppendLog);
                await SystemPackageManager.InstallAllMissingPrerequisitesAsync(progress);
            }, isPrimary: false, fontSize: 11);
            wrap.Children.Add(btnFixSdks);

            return wrap;
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

        private async Task ExecuteImportAsync()
        {
            string target = _repoTargetBox.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                AppendLog("❌ Please enter a repository URL, owner/repo, or local directory path.");
                return;
            }

            _btnImportSetup.IsEnabled = false;
            AppendLog($"🚀 Initializing Universal Importer for: {target}...");

            try
            {
                var progress = new Progress<string>(AppendLog);
                string customDest = _destPathBox.Text.Trim();
                bool autoDeps = _autoInstallDepsCheck.IsChecked ?? true;

                var result = await UniversalRepoImporterManager.ImportAndSetupRepoAsync(target, customDest, autoDeps, progress);
                _lastResult = result;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateStackHud(result.Stack, result.LocalPath);
                    _btnRunDev.IsEnabled = !string.IsNullOrEmpty(result.Stack.RunCommand);
                    _btnOpenVsCode.IsEnabled = Directory.Exists(result.LocalPath);
                    _btnOpenFolder.IsEnabled = Directory.Exists(result.LocalPath);
                    _btnOpenTerminal.IsEnabled = Directory.Exists(result.LocalPath);
                });

                if (result.Success)
                {
                    AppendLog($"🎉 Successfully imported and configured [{result.RepoName}] at: {result.LocalPath}");

                    if (_autoLaunchCheck.IsChecked == true)
                    {
                        OpenInVsCode();
                    }
                }
                else
                {
                    AppendLog($"⚠️ Import finished with errors: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Error during repository import: {ex.Message}");
            }
            finally
            {
                _btnImportSetup.IsEnabled = true;
            }
        }

        private void UpdateStackHud(DetectedTechStack stack, string localPath)
        {
            _primaryLangText.Text = $"Primary Language: {stack.PrimaryLanguage} ({stack.BuildSystem})";
            _runCommandText.Text = string.IsNullOrEmpty(stack.RunCommand) ? "(No run command detected)" : stack.RunCommand;

            _frameworksBadgePanel.Children.Clear();
            if (stack.Frameworks.Count == 0)
            {
                _frameworksBadgePanel.Children.Add(CreateBadge(stack.PrimaryLanguage, Brushes.DodgerBlue));
            }
            else
            {
                foreach (var f in stack.Frameworks)
                {
                    _frameworksBadgePanel.Children.Add(CreateBadge(f, Brushes.MediumPurple));
                }
            }

            _synthesizedConfigsPanel.Children.Clear();
            if (stack.SynthesizedConfigs.Count == 0)
            {
                _synthesizedConfigsPanel.Children.Add(new TextBlock { Text = "- Existing configs verified.", Foreground = Brushes.LightGreen, FontSize = 10 });
            }
            else
            {
                foreach (var c in stack.SynthesizedConfigs)
                {
                    _synthesizedConfigsPanel.Children.Add(new TextBlock { Text = $"✓ {c}", Foreground = Brushes.LightGreen, FontSize = 10 });
                }
            }
        }

        private Border CreateBadge(string text, Brush bg)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248)),
                BorderBrush = bg,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 4),
                Child = new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 10, FontWeight = FontWeights.SemiBold }
            };
        }

        private void LaunchProjectRunCommand()
        {
            if (_lastResult == null || string.IsNullOrEmpty(_lastResult.Stack.RunCommand)) return;
            AppendLog($"🚀 Launching dev server: {_lastResult.Stack.RunCommand} in {_lastResult.LocalPath}...");
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k cd /d \"{_lastResult.LocalPath}\" && {_lastResult.Stack.RunCommand}",
                UseShellExecute = true
            });
        }

        private void OpenInVsCode()
        {
            if (_lastResult == null || !Directory.Exists(_lastResult.LocalPath)) return;
            AppendLog($"💻 Opening in Visual Studio Code: {_lastResult.LocalPath}...");
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c code \"{_lastResult.LocalPath}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }

        private void OpenInExplorer()
        {
            if (_lastResult == null || !Directory.Exists(_lastResult.LocalPath)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_lastResult.LocalPath}\"",
                UseShellExecute = true
            });
        }

        private void OpenInTerminal()
        {
            if (_lastResult == null || !Directory.Exists(_lastResult.LocalPath)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k cd /d \"{_lastResult.LocalPath}\"",
                UseShellExecute = true
            });
        }
    }
}