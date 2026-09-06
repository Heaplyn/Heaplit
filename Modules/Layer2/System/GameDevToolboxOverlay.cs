// Developer: heaplyn
// Date: 2026-08-10
// Summary: Interactive Game Dev Toolbox Overlay. Provides Roblox Studio Luau script generators, Ring system dependency validator (Dragon Blox Ultra), and Blender animated texture baking scripts.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class GameDevToolboxOverlay : BaseOverlay
    {
        private static GameDevToolboxOverlay? _instance;

        private readonly Grid _contentGrid;
        private readonly ListBox _categoryListBox;
        
        // Roblox Validator fields
        private TextBox? _robloxProjectPathBox;
        private TextBlock? _validatorStatusLabel;
        private TextBox? _validatorOutputBox;

        // Color Converter fields
        private TextBox? _colorInputBox;
        private Border? _colorPreviewBorder;
        private TextBox? _colorOutputBox;

        // Tween Generator fields
        private ComboBox? _tweenStyleCombo;
        private ComboBox? _tweenDirCombo;
        private TextBox? _tweenTimeBox;
        private TextBox? _tweenRepeatBox;
        private CheckBox? _tweenReversesCheck;
        private TextBox? _tweenDelayBox;
        private TextBox? _tweenOutputBox;

        // VFX & Building fields
        private TextBox? _vfxPartCountBox;
        private TextBox? _vfxRadiusBox;
        private CheckBox? _vfxLookAtCenterCheck;
        private TextBox? _vfxEmitCountBox;
        private TextBox? _vfxLoopsBox;
        private TextBox? _vfxHeightBox;
        private TextBox? _vfxTracerSpeedBox;
        private TextBox? _vfxOutputBox;

        // DBZ Mechanics fields
        private ComboBox? _dbzMechanicCombo;
        private TextBox? _dbzOutputBox;

        // Admin & Player Data Restorer fields
        private TextBox? _admUserIdBox;
        private TextBox? _admDataStoreBox;
        private ComboBox? _admStrategyCombo;
        private ComboBox? _admPresetCombo;
        private TextBox? _admCustomPayloadBox;
        private TextBox? _admOutputBox;

        // AI Context Builder fields
        private ListBox? _aiFilesListBox;
        private TextBox? _aiPromptOutputBox;

        // Port Codebase to Studio fields
        private TextBox? _portSourcePathBox;
        private ComboBox? _portTargetServiceCombo;
        private ComboBox? _portSubfolderCombo;
        private TreeView? _portFileTree;
        private TextBox? _portOutputBox;
        private TextBlock? _portStatusLabel;

        // Roblox Generator fields
        private TextBox? _sprColsBox;
        private TextBox? _sprRowsBox;
        private TextBox? _sprFpsBox;
        private TextBox? _sprResBox;
        private TextBox? _generatedLuauBox;

        // Blender Generator fields
        private TextBox? _blModelBox;
        private TextBox? _blFramesBox;
        private TextBox? _blResXBox;
        private TextBox? _blResYBox;
        private CheckBox? _blTransBgBox;
        private TextBox? _generatedPyBox;

        public static void OpenToolbox()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null)
                {
                    _instance = new GameDevToolboxOverlay();
                }

                _instance.Show();
            });
        }

        private GameDevToolboxOverlay()
            : base("🎮 HEAPLIT GAME CREATOR TOOLBOX", width: 780, height: 530)
        {
            this.Closed += (s, e) => { _instance = null; };

            var mainGrid = new Grid { Margin = new Thickness(8) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Left navigation
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Content Area

            // Left Navigation Menu
            var navBorder = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            navBorder.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush");

            _categoryListBox = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 4, 0),
                ItemContainerStyle = (Style)Application.Current.FindResource("ResultItemStyle")
            };
            _categoryListBox.SetValue(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                ScrollBarVisibility.Disabled);

            var categories = new[]
            {
                "Roblox Code Analyzer",
                "General Roblox Utils",
                "Roblox VFX & Building",
                "DBZ Game Mechanics",
                "Admin & Data Restorer",
                "AI Context Builder",
                "Port Codebase → Studio",
                "Luau Code Generators",
                "Blender Animation Baker"
            };

            foreach (var cat in categories)
            {
                var item = new ListBoxItem
                {
                    Content = cat,
                    FontSize = 13,
                    Padding = new Thickness(10, 8, 10, 8),
                    Cursor = Cursors.Hand,
                    FontWeight = FontWeights.SemiBold
                };
                item.SetResourceReference(ListBoxItem.ForegroundProperty, "TextPrimaryBrush");
                _categoryListBox.Items.Add(item);
            }

            _categoryListBox.SelectionChanged += Navigation_SelectionChanged;
            navBorder.Child = _categoryListBox;
            Grid.SetColumn(navBorder, 0);
            mainGrid.Children.Add(navBorder);

            // Right Content Container
            _contentGrid = new Grid { Margin = new Thickness(8, 0, 0, 0) };
            Grid.SetColumn(_contentGrid, 1);
            mainGrid.Children.Add(_contentGrid);

            this.UserContent = mainGrid;

            // Load initial view
            _categoryListBox.SelectedIndex = 0;
        }

        private void Navigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _contentGrid.Children.Clear();
            int idx = _categoryListBox.SelectedIndex;
            if (idx == 0)
            {
                LoadRobloxAnalyzerView();
            }
            else if (idx == 1)
            {
                LoadGeneralRobloxUtilsView();
            }
            else if (idx == 2)
            {
                LoadRobloxVfxBuildingView();
            }
            else if (idx == 3)
            {
                LoadDbzMechanicsView();
            }
            else if (idx == 4)
            {
                LoadAdminDataRestorerView();
            }
            else if (idx == 5)
            {
                LoadAiContextBuilderView();
            }
            else if (idx == 6)
            {
                LoadPortCodebaseView();
            }
            else if (idx == 7)
            {
                LoadLuauGeneratorsView();
            }
            else if (idx == 8)
            {
                LoadBlenderGeneratorView();
            }
        }

        #region View 1: Roblox Codebase Analyzer
        private void LoadRobloxAnalyzerView()
        {
            var panel = new StackPanel();

            var title = CreateSectionHeader("🛡️ Roblox Studio Project Codebase Analyzer");
            panel.Children.Add(title);

            var hint = CreateHintText("Analyzes Luau script repositories, computes LOC metrics, checks Ring system layers conformance, and generates dependency graphs.");
            panel.Children.Add(hint);

            panel.Children.Add(CreateLabel("📂 Roblox Project / Rings Root Path:"));

            var pathGrid = new Grid();
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _robloxProjectPathBox = CreateTextBox();
            string defaultUserDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _robloxProjectPathBox.Text = System.IO.Path.Combine(defaultUserDir, "Downloads", "Projects", "Dragon Blox Essence");
            Grid.SetColumn(_robloxProjectPathBox, 0);
            pathGrid.Children.Add(_robloxProjectPathBox);

            var browseBtn = CreateButton("Browse...", (s, e) => BrowseProjectPath());
            browseBtn.Margin = new Thickness(8, 0, 0, 4);
            Grid.SetColumn(browseBtn, 1);
            pathGrid.Children.Add(browseBtn);
            panel.Children.Add(pathGrid);

            var actionGrid = new Grid { Margin = new Thickness(0, 10, 0, 10) };
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var ringBtn = CreateButton("🛡️ Validate Rings", (s, e) => RunRingsValidation());
            Grid.SetColumn(ringBtn, 0);
            actionGrid.Children.Add(ringBtn);

            var metricsBtn = CreateButton("📊 Code Metrics", (s, e) => RunCodeMetrics());
            metricsBtn.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(metricsBtn, 1);
            actionGrid.Children.Add(metricsBtn);

            var graphBtn = CreateButton("🧬 Require Graph", (s, e) => RunRequireGraph());
            graphBtn.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(graphBtn, 2);
            actionGrid.Children.Add(graphBtn);

            panel.Children.Add(actionGrid);

            _validatorStatusLabel = new TextBlock
            {
                Text = "Specify directory and select analysis operation.",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _validatorStatusLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            panel.Children.Add(_validatorStatusLabel);

            _validatorOutputBox = new TextBox
            {
                Height = 220,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(6)
            };
            _validatorOutputBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _validatorOutputBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_validatorOutputBox);

            _contentGrid.Children.Add(panel);
        }

        private void BrowseProjectPath()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Roblox Rings Root Folder"
            };
            if (dialog.ShowDialog() == true && _robloxProjectPathBox != null)
            {
                _robloxProjectPathBox.Text = dialog.FolderName;
            }
        }

        private void RunRingsValidation()
        {
            if (_robloxProjectPathBox == null || _validatorStatusLabel == null || _validatorOutputBox == null) return;

            string targetDir = _robloxProjectPathBox.Text.Trim();
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            {
                _validatorStatusLabel.Text = "❌ Path error!";
                _validatorOutputBox.Text = "Please specify a valid, existing directory root.";
                return;
            }

            _validatorStatusLabel.Text = "🔄 Scanning codebase dependencies...";
            _validatorOutputBox.Text = "";

            try
            {
                var files = Directory.GetFiles(targetDir, "*.lua", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(targetDir, "*.luau", SearchOption.AllDirectories))
                    .ToList();

                var violations = new List<string>();
                int checkedFiles = 0;

                var requireRegex = new Regex(@"require\s*\(\s*([A-Za-z0-9_\.\s]+)\s*\)", RegexOptions.Compiled);

                foreach (var file in files)
                {
                    string relativePath = file.Substring(targetDir.Length).TrimStart('\\', '/');
                    int sourceRing = GetRingLevelFromPath(file);

                    if (sourceRing < 0) continue;
                    checkedFiles++;

                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var matches = requireRegex.Matches(lines[i]);
                        foreach (Match match in matches)
                        {
                            string requirePath = match.Groups[1].Value.Replace(" ", "");
                            int targetRing = GetRingLevelFromRequire(requirePath);

                            if (targetRing > sourceRing)
                            {
                                violations.Add($"⚠️ Ring Violation in: {relativePath} [Line {i + 1}]\n" +
                                               $"   Source: Ring {sourceRing} -> Target: Ring {targetRing}\n" +
                                               $"   Code: \"{lines[i].Trim()}\"\n");
                            }
                        }
                    }
                }

                if (violations.Count > 0)
                {
                    _validatorStatusLabel.Text = $"❌ Validation Failed! Found {violations.Count} dependency violations.";
                    _validatorStatusLabel.Foreground = Brushes.Red;
                    _validatorOutputBox.Text = string.Join("\n", violations);
                }
                else
                {
                    _validatorStatusLabel.Text = $"✅ Validation Successful! Scanned {checkedFiles} Ring scripts cleanly.";
                    _validatorStatusLabel.Foreground = Brushes.LightGreen;
                    _validatorOutputBox.Text = "All Ring dependency hierarchy levels (Ring 0 -> Ring 4) conform to layers rules successfully!\n- Ring 0: No higher requires\n- Ring 1: Requires <= Ring 1\n- Ring 2: Requires <= Ring 2\n- Ring 3: Requires <= Ring 3\n- Ring 4: Fully permissive";
                }
            }
            catch (Exception ex)
            {
                _validatorStatusLabel.Text = "⚠️ Scan failed due to exception.";
                _validatorOutputBox.Text = ex.ToString();
            }
        }

        private void RunCodeMetrics()
        {
            if (_robloxProjectPathBox == null || _validatorStatusLabel == null || _validatorOutputBox == null) return;

            string targetDir = _robloxProjectPathBox.Text.Trim();
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            {
                _validatorStatusLabel.Text = "❌ Path error!";
                _validatorOutputBox.Text = "Please specify a valid, existing directory root.";
                return;
            }

            _validatorStatusLabel.Text = "🔄 Gathering code metrics...";
            _validatorOutputBox.Text = "";

            try
            {
                var files = Directory.GetFiles(targetDir, "*.lua", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(targetDir, "*.luau", SearchOption.AllDirectories))
                    .ToList();

                int totalScripts = files.Count;
                long totalLines = 0;
                long commentLines = 0;
                long emptyLines = 0;
                int clientScripts = 0;
                int serverScripts = 0;
                int moduleScripts = 0;

                var serviceCounter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    string name = Path.GetFileName(file).ToLower();
                    if (name.Contains(".client") || name.Contains("localscript")) clientScripts++;
                    else if (name.Contains(".server") || (name.Contains("script") && !name.Contains("module"))) serverScripts++;
                    else moduleScripts++;

                    string[] lines = File.ReadAllLines(file);
                    totalLines += lines.Length;

                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Length == 0)
                        {
                            emptyLines++;
                        }
                        else if (trimmed.StartsWith("--"))
                        {
                            commentLines++;
                        }

                        var match = Regex.Match(trimmed, @"game:GetService\s*\(\s*[""']([^""']+)[""']\s*\)");
                        if (match.Success)
                        {
                            string service = match.Groups[1].Value;
                            if (serviceCounter.ContainsKey(service)) serviceCounter[service]++;
                            else serviceCounter[service] = 1;
                        }
                    }
                }

                long codeLines = totalLines - commentLines - emptyLines;
                double commentPercent = totalLines > 0 ? (double)commentLines / totalLines * 100 : 0;

                var sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine("📊 ROBLOX PROJECT CODEBASE METRICS");
                sb.AppendLine("==================================================");
                sb.AppendLine($"📂 Scanned Root: {targetDir}");
                sb.AppendLine($"📄 Total Scripts: {totalScripts}");
                sb.AppendLine($"   └─ Client/Local Scripts: {clientScripts}");
                sb.AppendLine($"   └─ Server Scripts: {serverScripts}");
                sb.AppendLine($"   └─ Module Scripts: {moduleScripts}");
                sb.AppendLine();
                sb.AppendLine($"📈 Lines of Code (LOC): {totalLines}");
                sb.AppendLine($"   ├─ Pure Code Lines: {codeLines}");
                sb.AppendLine($"   ├─ Comment Lines: {commentLines} ({commentPercent:F1}%)");
                sb.AppendLine($"   └─ Empty Lines: {emptyLines}");
                sb.AppendLine();
                sb.AppendLine("🧬 Service Usage Counts:");
                foreach (var service in serviceCounter.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"   ├─ {service.Key}: {service.Value} times");
                }
                if (serviceCounter.Count == 0) sb.AppendLine("   ├─ No game:GetService() calls detected.");
                sb.AppendLine("==================================================");

                _validatorStatusLabel.Text = "✅ Metrics analysis completed successfully.";
                _validatorStatusLabel.Foreground = Brushes.LightGreen;
                _validatorOutputBox.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                _validatorStatusLabel.Text = "⚠️ Metrics scan failed due to exception.";
                _validatorOutputBox.Text = ex.ToString();
            }
        }

        private void RunRequireGraph()
        {
            if (_robloxProjectPathBox == null || _validatorStatusLabel == null || _validatorOutputBox == null) return;

            string targetDir = _robloxProjectPathBox.Text.Trim();
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            {
                _validatorStatusLabel.Text = "❌ Path error!";
                _validatorOutputBox.Text = "Please specify a valid, existing directory root.";
                return;
            }

            _validatorStatusLabel.Text = "🔄 Generating Require Graph...";
            _validatorOutputBox.Text = "";

            try
            {
                var files = Directory.GetFiles(targetDir, "*.lua", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(targetDir, "*.luau", SearchOption.AllDirectories))
                    .ToList();

                var edges = new HashSet<string>();
                var requireRegex = new Regex(@"require\s*\(\s*([A-Za-z0-9_\.\s]+)\s*\)", RegexOptions.Compiled);

                foreach (var file in files)
                {
                    string sourceName = Path.GetFileNameWithoutExtension(file);
                    string[] lines = File.ReadAllLines(file);

                    foreach (var line in lines)
                    {
                        var matches = requireRegex.Matches(line);
                        foreach (Match match in matches)
                        {
                            string requirePath = match.Groups[1].Value.Replace(" ", "");
                            string targetName = requirePath;
                            int lastDot = requirePath.LastIndexOf('.');
                            if (lastDot >= 0 && lastDot < requirePath.Length - 1)
                            {
                                targetName = requirePath.Substring(lastDot + 1);
                            }

                            if (!string.IsNullOrEmpty(targetName) && sourceName != targetName)
                            {
                                edges.Add($"    {sourceName} --> {targetName}");
                            }
                        }
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine("```mermaid");
                sb.AppendLine("graph TD");
                if (edges.Count > 0)
                {
                    foreach (var edge in edges.Take(150))
                    {
                        sb.AppendLine(edge);
                    }
                    if (edges.Count > 150)
                    {
                        sb.AppendLine("    %% Graph truncated to 150 links for readability");
                    }
                }
                else
                {
                    sb.AppendLine("    NoRequiresDetected --> None");
                }
                sb.AppendLine("```");

                _validatorStatusLabel.Text = "✅ Mermaid Require Graph generated successfully.";
                _validatorStatusLabel.Foreground = Brushes.LightGreen;
                _validatorOutputBox.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                _validatorStatusLabel.Text = "⚠️ Graph generation failed due to exception.";
                _validatorOutputBox.Text = ex.ToString();
            }
        }

        private int GetRingLevelFromPath(string path)
        {
            string clean = path.ToLower();
            if (clean.Contains("ring0") || clean.Contains("ring_0")) return 0;
            if (clean.Contains("ring1") || clean.Contains("ring_1")) return 1;
            if (clean.Contains("ring2") || clean.Contains("ring_2")) return 2;
            if (clean.Contains("ring3") || clean.Contains("ring_3")) return 3;
            if (clean.Contains("ring4") || clean.Contains("ring_4")) return 4;
            return -1;
        }

        private int GetRingLevelFromRequire(string requirePath)
        {
            string clean = requirePath.ToLower();
            if (clean.Contains("ring0")) return 0;
            if (clean.Contains("ring1")) return 1;
            if (clean.Contains("ring2")) return 2;
            if (clean.Contains("ring3")) return 3;
            if (clean.Contains("ring4")) return 4;
            return -1;
        }
        #endregion

        #region View 2: General Roblox Studio Utilities
        private void LoadGeneralRobloxUtilsView()
        {
            var panel = new StackPanel();

            var title = CreateSectionHeader("🛠️ General Roblox Studio Utilities");
            panel.Children.Add(title);

            var hint = CreateHintText("Essential utilities for daily Roblox development, including Color3 conversions and TweenService code generation.");
            panel.Children.Add(hint);

            // 1. Color3 Converter Section
            panel.Children.Add(CreateSubHeader("🎨 Color3 Converter (Hex / RGB)"));

            var colorGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            colorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            colorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            colorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var colLeft = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            colLeft.Children.Add(CreateLabel("Hex/RGB Input (e.g. #FF3366 or 255, 51, 102):"));
            _colorInputBox = CreateTextBox();
            _colorInputBox.Text = "#FF3366";
            _colorInputBox.TextChanged += (s, ev) => UpdateColorConversion();
            colLeft.Children.Add(_colorInputBox);
            Grid.SetColumn(colLeft, 0);
            colorGrid.Children.Add(colLeft);

            var previewPanel = new StackPanel { Margin = new Thickness(0, 12, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            _colorPreviewBorder = new Border
            {
                Width = 40,
                Height = 30,
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(255, 51, 102))
            };
            _colorPreviewBorder.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush");
            previewPanel.Children.Add(_colorPreviewBorder);
            Grid.SetColumn(previewPanel, 1);
            colorGrid.Children.Add(previewPanel);

            var colRight = new StackPanel();
            colRight.Children.Add(CreateLabel("Generated Luau Color3:"));
            _colorOutputBox = CreateTextBox();
            _colorOutputBox.IsReadOnly = true;
            colRight.Children.Add(_colorOutputBox);
            Grid.SetColumn(colRight, 2);
            colorGrid.Children.Add(colRight);

            panel.Children.Add(colorGrid);

            // 2. TweenService Generator Section
            panel.Children.Add(CreateSubHeader("📈 TweenService Info Generator"));

            var tweenGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            tweenGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tweenGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tweenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            tweenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tweenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tweenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tweenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var stylePanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            stylePanel.Children.Add(CreateLabel("Easing Style:"));
            _tweenStyleCombo = CreateComboBox(new[] { "Linear", "Quad", "Cubic", "Quart", "Quint", "Sine", "Back", "Bounce", "Elastic" });
            _tweenStyleCombo.SelectedIndex = 1;
            stylePanel.Children.Add(_tweenStyleCombo);
            Grid.SetRow(stylePanel, 0); Grid.SetColumn(stylePanel, 0);
            tweenGrid.Children.Add(stylePanel);

            var dirPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            dirPanel.Children.Add(CreateLabel("Direction:"));
            _tweenDirCombo = CreateComboBox(new[] { "Out", "In", "InOut" });
            _tweenDirCombo.SelectedIndex = 0;
            dirPanel.Children.Add(_tweenDirCombo);
            Grid.SetRow(dirPanel, 0); Grid.SetColumn(dirPanel, 1);
            tweenGrid.Children.Add(dirPanel);

            var durPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            durPanel.Children.Add(CreateLabel("Duration (s):"));
            _tweenTimeBox = CreateTextBox(); _tweenTimeBox.Text = "0.5";
            durPanel.Children.Add(_tweenTimeBox);
            Grid.SetRow(durPanel, 0); Grid.SetColumn(durPanel, 2);
            tweenGrid.Children.Add(durPanel);

            var repPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            repPanel.Children.Add(CreateLabel("Repeats:"));
            _tweenRepeatBox = CreateTextBox(); _tweenRepeatBox.Text = "0";
            repPanel.Children.Add(_tweenRepeatBox);
            Grid.SetRow(repPanel, 0); Grid.SetColumn(repPanel, 3);
            tweenGrid.Children.Add(repPanel);

            var revPanel = new StackPanel { Margin = new Thickness(0, 16, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            _tweenReversesCheck = new CheckBox
            {
                Content = "Reverses",
                IsChecked = false,
                FontSize = 11
            };
            _tweenReversesCheck.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
            revPanel.Children.Add(_tweenReversesCheck);
            Grid.SetRow(revPanel, 0); Grid.SetColumn(revPanel, 4);
            tweenGrid.Children.Add(revPanel);

            panel.Children.Add(tweenGrid);

            var genTweenBtn = CreateButton("📈 Generate Tween Luau Snippet", (s, e) => GenerateTweenLuau());
            genTweenBtn.Margin = new Thickness(0, 0, 0, 8);
            panel.Children.Add(genTweenBtn);

            _tweenOutputBox = new TextBox
            {
                Height = 110,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(6)
            };
            _tweenOutputBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _tweenOutputBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_tweenOutputBox);

            var copyTweenBtn = CreateButton("📋 Copy Tween Code", (s, e) => CopyToClipboard(_tweenOutputBox.Text));
            copyTweenBtn.Margin = new Thickness(0, 6, 0, 0);
            panel.Children.Add(copyTweenBtn);

            _contentGrid.Children.Add(panel);

            UpdateColorConversion();
            GenerateTweenLuau();
        }

        private void UpdateColorConversion()
        {
            if (_colorInputBox == null || _colorPreviewBorder == null || _colorOutputBox == null) return;

            string input = _colorInputBox.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            try
            {
                Color color = Colors.Transparent;
                if (input.StartsWith("#"))
                {
                    color = (Color)ColorConverter.ConvertFromString(input);
                }
                else
                {
                    string[] parts = input.Split(',');
                    if (parts.Length == 3)
                    {
                        byte r = byte.Parse(parts[0].Trim());
                        byte g = byte.Parse(parts[1].Trim());
                        byte b = byte.Parse(parts[2].Trim());
                        color = Color.FromRgb(r, g, b);
                    }
                }

                if (color != Colors.Transparent)
                {
                    _colorPreviewBorder.Background = new SolidColorBrush(color);
                    _colorOutputBox.Text = $"Color3.fromRGB({color.R}, {color.G}, {color.B})";
                }
            }
            catch
            {
                _colorOutputBox.Text = "Invalid Color format";
            }
        }

        private void GenerateTweenLuau()
        {
            if (_tweenStyleCombo == null || _tweenDirCombo == null || _tweenTimeBox == null || _tweenRepeatBox == null || _tweenReversesCheck == null || _tweenOutputBox == null) return;

            string style = "Quad";
            if (_tweenStyleCombo.SelectedItem is ComboBoxItem itemStyle && itemStyle.Content != null)
            {
                style = itemStyle.Content.ToString() ?? "Quad";
            }

            string dir = "Out";
            if (_tweenDirCombo.SelectedItem is ComboBoxItem itemDir && itemDir.Content != null)
            {
                dir = itemDir.Content.ToString() ?? "Out";
            }

            double.TryParse(_tweenTimeBox.Text, out double duration);
            int.TryParse(_tweenRepeatBox.Text, out int repeats);
            bool reverses = _tweenReversesCheck.IsChecked == true;

            if (duration <= 0) duration = 0.5;

            string code = "local TweenService = game:GetService(\"TweenService\")\n" +
                          "local targetObject = script.Parent -- Target instance\n\n" +
                          "local tweenInfo = TweenInfo.new(\n" +
                          $"    {duration:F2}, -- Time\n" +
                          $"    Enum.EasingStyle.{style}, -- EasingStyle\n" +
                          $"    Enum.EasingDirection.{dir}, -- EasingDirection\n" +
                          $"    {repeats}, -- RepeatCount (less than 0 to loop infinitely)\n" +
                          $"    {reverses.ToString().ToLower()}, -- Reverses\n" +
                          "    0 -- DelayTime\n" +
                          ")\n\n" +
                          "local properties = {\n" +
                          "    Size = UDim2.new(1.2, 0, 1.2, 0) -- Add properties here\n" +
                          "}\n\n" +
                          "local tween = TweenService:Create(targetObject, tweenInfo, properties)\n" +
                          "tween:Play()\n";

            _tweenOutputBox.Text = code;
        }
        #endregion

        #region View 3: Roblox VFX & Building
        private void LoadRobloxVfxBuildingView()
        {
            var panel = new StackPanel();

            var title = CreateSectionHeader("✨ Roblox VFX & Building Script Generators");
            panel.Children.Add(title);

            var hint = CreateHintText("Automates visual effects triggers and architectural geometric arrangements in Roblox Studio.");
            panel.Children.Add(hint);

            panel.Children.Add(CreateSubHeader("🏗️ Geometric Arrangements (Ring & Spiral Helix)"));

            var buildGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            buildGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buildGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buildGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buildGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var countCol = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            countCol.Children.Add(CreateLabel("Part Count:"));
            _vfxPartCountBox = CreateTextBox(); _vfxPartCountBox.Text = "24";
            countCol.Children.Add(_vfxPartCountBox);
            Grid.SetColumn(countCol, 0); buildGrid.Children.Add(countCol);

            var radCol = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            radCol.Children.Add(CreateLabel("Radius (studs):"));
            _vfxRadiusBox = CreateTextBox(); _vfxRadiusBox.Text = "15";
            radCol.Children.Add(_vfxRadiusBox);
            Grid.SetColumn(radCol, 1); buildGrid.Children.Add(radCol);

            var loopsCol = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            loopsCol.Children.Add(CreateLabel("Helix Loops:"));
            _vfxLoopsBox = CreateTextBox(); _vfxLoopsBox.Text = "2";
            loopsCol.Children.Add(_vfxLoopsBox);
            Grid.SetColumn(loopsCol, 2); buildGrid.Children.Add(loopsCol);

            var heightCol = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            heightCol.Children.Add(CreateLabel("Helix Height:"));
            _vfxHeightBox = CreateTextBox(); _vfxHeightBox.Text = "20";
            heightCol.Children.Add(_vfxHeightBox);
            Grid.SetColumn(heightCol, 3); buildGrid.Children.Add(heightCol);

            panel.Children.Add(buildGrid);

            panel.Children.Add(CreateSubHeader("💥 Advanced Particle Burst & Combat VFX"));

            var burstGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            burstGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            burstGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

            var emitCol = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            emitCol.Children.Add(CreateLabel("Burst Count:"));
            _vfxEmitCountBox = CreateTextBox(); _vfxEmitCountBox.Text = "30";
            emitCol.Children.Add(_vfxEmitCountBox);
            Grid.SetColumn(emitCol, 0); burstGrid.Children.Add(emitCol);

            var extraCol = new StackPanel { Margin = new Thickness(0, 16, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            _vfxLookAtCenterCheck = new CheckBox
            {
                Content = "Align Parts facing Center",
                IsChecked = true,
                FontSize = 11
            };
            _vfxLookAtCenterCheck.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
            extraCol.Children.Add(_vfxLookAtCenterCheck);
            Grid.SetColumn(extraCol, 1); burstGrid.Children.Add(extraCol);

            panel.Children.Add(burstGrid);

            var buttonGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var arrangeBtn = CreateButton("🏗️ Ring Code", (s, e) => GenerateCircularRingLuau());
            Grid.SetColumn(arrangeBtn, 0); buttonGrid.Children.Add(arrangeBtn);

            var helixBtn = CreateButton("🌀 Spiral Code", (s, e) => GenerateSpiralHelixLuau());
            helixBtn.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(helixBtn, 1); buttonGrid.Children.Add(helixBtn);

            var burstBtn = CreateButton("💥 Burst Code", (s, e) => GenerateParticleBurstLuau());
            burstBtn.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(burstBtn, 2); buttonGrid.Children.Add(burstBtn);

            var tracerBtn = CreateButton("⚡ Tracer Code", (s, e) => GenerateRaycastTracerLuau());
            tracerBtn.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(tracerBtn, 3); buttonGrid.Children.Add(tracerBtn);

            panel.Children.Add(buttonGrid);

            _vfxOutputBox = new TextBox
            {
                Height = 150,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(6)
            };
            _vfxOutputBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _vfxOutputBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_vfxOutputBox);

            var copyBtn = CreateButton("📋 Copy Generated VFX/Build Code", (s, e) => CopyToClipboard(_vfxOutputBox.Text));
            copyBtn.Margin = new Thickness(0, 8, 0, 0);
            panel.Children.Add(copyBtn);

            _contentGrid.Children.Add(panel);

            GenerateCircularRingLuau();
        }

        private void GenerateCircularRingLuau()
        {
            if (_vfxPartCountBox == null || _vfxRadiusBox == null || _vfxLookAtCenterCheck == null || _vfxOutputBox == null) return;

            int.TryParse(_vfxPartCountBox.Text, out int count);
            double.TryParse(_vfxRadiusBox.Text, out double radius);
            bool lookAtCenter = _vfxLookAtCenterCheck.IsChecked == true;

            if (count <= 0) count = 24;
            if (radius <= 0) radius = 15;

            string code = $"-- CircularPartArranger.lua\n" +
                          $"-- Spawns parts arranged geometrically in a perfect circle/ring layout\n" +
                          $"local count = {count}\n" +
                          $"local radius = {radius:F1}\n" +
                          $"local center = Vector3.new(0, 10, 0) -- Change to your desired center coordinates\n" +
                          $"local parentFolder = workspace:FindFirstChild(\"RingParts\") or Instance.new(\"Folder\", workspace)\n" +
                          $"parentFolder.Name = \"RingParts\"\n\n" +
                          $"for i = 1, count do\n" +
                          $"    local angle = (i / count) * math.pi * 2\n" +
                          $"    local offset = Vector3.new(math.cos(angle) * radius, 0, math.sin(angle) * radius)\n" +
                          $"    local part = Instance.new(\"Part\")\n" +
                          $"    part.Size = Vector3.new(2, 2, 2) -- Change to desired part size\n" +
                          $"    part.Position = center + offset\n" +
                          $"    part.Anchored = true\n";

            if (lookAtCenter)
            {
                code += $"    part.CFrame = CFrame.lookAt(part.Position, center)\n";
            }

            code += $"    part.Parent = parentFolder\n" +
                    $"end\n";

            _vfxOutputBox.Text = code;
        }

        private void GenerateSpiralHelixLuau()
        {
            if (_vfxPartCountBox == null || _vfxRadiusBox == null || _vfxLoopsBox == null || _vfxHeightBox == null || _vfxOutputBox == null) return;

            int.TryParse(_vfxPartCountBox.Text, out int count);
            double.TryParse(_vfxRadiusBox.Text, out double radius);
            double.TryParse(_vfxLoopsBox.Text, out double loops);
            double.TryParse(_vfxHeightBox.Text, out double height);

            if (count <= 0) count = 24;
            if (radius <= 0) radius = 15;
            if (loops <= 0) loops = 2;
            if (height <= 0) height = 20;

            string code = $"-- SpiralHelixArranger.lua\n" +
                          $"-- Spawns parts arranged in a vertical winding spiral helix layout\n" +
                          $"local count = {count}\n" +
                          $"local radius = {radius:F1}\n" +
                          $"local loops = {loops:F1}\n" +
                          $"local height = {height:F1}\n" +
                          $"local center = Vector3.new(0, 10, 0)\n" +
                          $"local parentFolder = workspace:FindFirstChild(\"HelixParts\") or Instance.new(\"Folder\", workspace)\n" +
                          $"parentFolder.Name = \"HelixParts\"\n\n" +
                          $"for i = 1, count do\n" +
                          $"    local progress = i / count\n" +
                          $"    local angle = progress * loops * math.pi * 2\n" +
                          $"    local offset = Vector3.new(math.cos(angle) * radius, progress * height, math.sin(angle) * radius)\n" +
                          $"    local part = Instance.new(\"Part\")\n" +
                          $"    part.Size = Vector3.new(1.5, 1.5, 1.5)\n" +
                          $"    part.Position = center + offset\n" +
                          $"    part.Anchored = true\n" +
                          $"    part.CFrame = CFrame.lookAt(part.Position, center + Vector3.new(0, progress * height, 0))\n" +
                          $"    part.Parent = parentFolder\n" +
                          $"end\n";

            _vfxOutputBox.Text = code;
        }

        private void GenerateParticleBurstLuau()
        {
            if (_vfxEmitCountBox == null || _vfxOutputBox == null) return;

            int.TryParse(_vfxEmitCountBox.Text, out int count);
            if (count <= 0) count = 30;

            string code = $"-- AdvancedParticleBurst.lua\n" +
                          $"-- Recurses hierarchy, parses Delay & Duration attributes, and triggers asynchronous particle bursts\n" +
                          $"local function triggerBurst(targetInstance: Instance)\n" +
                          $"    for _, child in ipairs(targetInstance:GetDescendants()) do\n" +
                          $"        if child:IsA(\"ParticleEmitter\") then\n" +
                          $"            task.spawn(function()\n" +
                          $"                local emitCount = child:GetAttribute(\"EmitCount\") or {count}\n" +
                          $"                local delayTime = child:GetAttribute(\"EmitDelay\") or 0\n" +
                          $"                local duration = child:GetAttribute(\"EmitDuration\") or 0\n\n" +
                          $"                if delayTime > 0 then\n" +
                          $"                    task.wait(delayTime)\n" +
                          $"                end\n\n" +
                          $"                if duration > 0 then\n" +
                          $"                    -- Emit gradually over duration\n" +
                          $"                    local rate = emitCount / duration\n" +
                          $"                    local elapsed = 0\n" +
                          $"                    local interval = 0.05\n" +
                          $"                    while elapsed < duration do\n" +
                          $"                        local stepCount = math.max(1, math.round(rate * interval))\n" +
                          $"                        child:Emit(stepCount)\n" +
                          $"                        task.wait(interval)\n" +
                          $"                        elapsed = elapsed + interval\n" +
                          $"                    end\n" +
                          $"                else\n" +
                          $"                    -- Instant burst\n" +
                          $"                    child:Emit(emitCount)\n" +
                          $"                end\n" +
                          $"            end)\n" +
                          $"        end\n" +
                          $"    end\n" +
                          $"end\n\n" +
                          $"-- Example Usage:\n" +
                          $"-- triggerBurst(workspace.VfxSkillAura)\n";

            _vfxOutputBox.Text = code;
        }

        private void GenerateRaycastTracerLuau()
        {
            if (_vfxOutputBox == null) return;

            string code = $"-- RaycastVisualTracer.lua\n" +
                          $"-- Executes a raycast from origin to target direction and aligns a visual laser/tracer part\n" +
                          $"local TweenService = game:GetService(\"TweenService\")\n\n" +
                          $"local function spawnTracer(origin: Vector3, destination: Vector3)\n" +
                          $"    local distance = (destination - origin).Magnitude\n" +
                          $"    if distance <= 0.1 then return end\n\n" +
                          $"    local tracer = Instance.new(\"Part\")\n" +
                          $"    tracer.Anchored = true\n" +
                          $"    tracer.CanCollide = false\n" +
                          $"    tracer.Size = Vector3.new(0.25, 0.25, distance)\n" +
                          $"    tracer.Color = Color3.fromRGB(255, 80, 50) -- Custom laser beam color\n" +
                          $"    tracer.Material = Enum.Material.Neon\n\n" +
                          $"    -- Center align and point part toward destination\n" +
                          $"    tracer.CFrame = CFrame.lookAt(origin, destination) * CFrame.new(0, 0, -distance / 2)\n" +
                          $"    tracer.Parent = workspace\n\n" +
                          $"    -- Animate the tracer shrinking and disappearing\n" +
                          $"    local tweenInfo = TweenInfo.new(0.25, Enum.EasingStyle.Quad, Enum.EasingDirection.Out)\n" +
                          $"    local tween = TweenService:Create(tracer, tweenInfo, {{\n" +
                          $"        Size = Vector3.new(0, 0, distance),\n" +
                          $"        Transparency = 1\n" +
                          $"    }})\n\n" +
                          $"    tween.Completed:Connect(function()\n" +
                          $"        tracer:Destroy()\n" +
                          $"    end)\n" +
                          $"    tween:Play()\n" +
                          $"end\n\n" +
                          $"-- Example Usage:\n" +
                          $"-- local params = RaycastParams.new()\n" +
                          $"-- local result = workspace:Raycast(originPos, direction * 100, params)\n" +
                          $"-- if result then spawnTracer(originPos, result.Position) end\n";

            _vfxOutputBox.Text = code;
        }
        #endregion

        #region View 4: DBZ Game Mechanics
        private void LoadDbzMechanicsView()
        {
            var panel = new StackPanel();

            var title = CreateSectionHeader("🔥 DBZ Game Mechanics Script Generators");
            panel.Children.Add(title);

            var hint = CreateHintText("Generates production-ready Luau scripts for core Dragon Ball Z action game mechanics.");
            panel.Children.Add(hint);

            panel.Children.Add(CreateLabel("Select Mechanic:"));
            _dbzMechanicCombo = CreateComboBox(new[] { "Ki Energy Charging System", "Vanish / Teleport Behind target", "Ki Blast Projectile Controller" });
            _dbzMechanicCombo.SelectedIndex = 0;
            _dbzMechanicCombo.SelectionChanged += (s, e) => GenerateDbzMechanicCode();
            panel.Children.Add(_dbzMechanicCombo);

            _dbzOutputBox = new TextBox
            {
                Height = 280,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 10, 0, 0)
            };
            _dbzOutputBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _dbzOutputBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_dbzOutputBox);

            var copyBtn = CreateButton("📋 Copy Mechanic Code", (s, e) => CopyToClipboard(_dbzOutputBox.Text));
            copyBtn.Margin = new Thickness(0, 8, 0, 0);
            panel.Children.Add(copyBtn);

            _contentGrid.Children.Add(panel);

            GenerateDbzMechanicCode();
        }

        private void GenerateDbzMechanicCode()
        {
            if (_dbzMechanicCombo == null || _dbzOutputBox == null) return;

            int idx = _dbzMechanicCombo.SelectedIndex;
            string code = "";

            if (idx == 0)
            {
                code = "-- KiChargingManager.lua\n" +
                       "-- Server-side controller managing Ki charging, animation states, and VFX triggers\n" +
                       "local ReplicatedStorage = game:GetService(\"ReplicatedStorage\")\n" +
                       "local TweenService = game:GetService(\"TweenService\")\n" +
                       "local Players = game:GetService(\"Players\")\n\n" +
                       "local KiManager = {}\n" +
                       "local activeChargers = {}\n\n" +
                       "function KiManager.startCharging(player: Player)\n" +
                       "    local char = player.Character\n" +
                       "    if not char then return end\n" +
                       "    \n" +
                       "    if activeChargers[player] then return end\n" +
                       "    activeChargers[player] = true\n\n" +
                       "    -- Play Charging Animation\n" +
                       "    local hum = char:FindFirstChildOfClass(\"Humanoid\")\n" +
                       "    local animTrack = hum and hum:LoadAnimation(ReplicatedStorage.Animations.KiCharge) -- Set animation\n" +
                       "    if animTrack then animTrack:Play(0.2) end\n\n" +
                       "    -- Enable charging particles/auras\n" +
                       "    local aura = char:FindFirstChild(\"KiAura\")\n" +
                       "    if aura then\n" +
                       "        for _, child in ipairs(aura:GetDescendants()) do\n" +
                       "            if child:IsA(\"ParticleEmitter\") then child.Enabled = true end\n" +
                       "        end\n" +
                       "    end\n\n" +
                       "    -- Charge loop\n" +
                       "    task.spawn(function()\n" +
                       "        while activeChargers[player] and char.Parent do\n" +
                       "            local currentKi = player:GetAttribute(\"Ki\") or 0\n" +
                       "            local maxKi = player:GetAttribute(\"MaxKi\") or 100\n" +
                       "            \n" +
                       "            if currentKi < maxKi then\n" +
                       "                player:SetAttribute(\"Ki\", math.min(maxKi, currentKi + (maxKi * 0.05))) -- Add 5% per tick\n" +
                       "            end\n" +
                       "            task.wait(0.25)\n" +
                       "        end\n" +
                       "        \n" +
                       "        -- Cleanup on stop\n" +
                       "        if animTrack then animTrack:Stop(0.2) end\n" +
                       "        if aura then\n" +
                       "            for _, child in ipairs(aura:GetDescendants()) do\n" +
                       "                if child:IsA(\"ParticleEmitter\") then child.Enabled = false end\n" +
                       "            end\n" +
                       "        end\n" +
                       "    end)\n" +
                       "end\n\n" +
                       "function KiManager.stopCharging(player: Player)\n" +
                       "    activeChargers[player] = nil\n" +
                       "end\n\n" +
                       "return KiManager\n";
            }
            else if (idx == 1)
            {
                code = "-- VanishTeleport.lua\n" +
                       "-- Performs a classic vanish teleport directly behind the opponent's back vector\n" +
                       "local Players = game:GetService(\"Players\")\n" +
                       "local TweenService = game:GetService(\"TweenService\")\n\n" +
                       "local function vanishBehind(attacker: Model, target: Model)\n" +
                       "    local attackerRoot = attacker:FindFirstChild(\"HumanoidRootPart\")\n" +
                       "    local targetRoot = target:FindFirstChild(\"HumanoidRootPart\")\n" +
                       "    if not attackerRoot or not targetRoot then return end\n\n" +
                       "    -- Play vanish whoosh sound\n" +
                       "    local sound = attackerRoot:FindFirstChild(\"VanishSound\")\n" +
                       "    if sound then sound:Play() end\n\n" +
                       "    -- Spawn vanish visual particle effects\n" +
                       "    local vanishEmitter = attackerRoot:FindFirstChild(\"VanishEffect\")\n" +
                       "    if vanishEmitter then vanishEmitter:Emit(15) end\n\n" +
                       "    -- Calculate CFrame exactly 3 studs behind target's back looking at target\n" +
                       "    local targetCFrame = targetRoot.CFrame\n" +
                       "    local destination = targetCFrame * CFrame.new(0, 0, 3.5) -- Offset along Z-axis\n" +
                       "    \n" +
                       "    -- Teleport and face target\n" +
                       "    attackerRoot.CFrame = CFrame.lookAt(destination.Position, targetRoot.Position)\n" +
                       "    \n" +
                       "    -- Re-emit particles at destination\n" +
                       "    task.wait(0.05)\n" +
                       "    if vanishEmitter then vanishEmitter:Emit(15) end\n" +
                       "end\n\n" +
                       "-- Usage:\n" +
                       "-- vanishBehind(myCharacter, opponentCharacter)\n";
            }
            else if (idx == 2)
            {
                code = "-- KiBlastController.lua\n" +
                       "-- Handles spawning, moving, and hit detection for Ki energy projectile blasts\n" +
                       "local Debris = game:GetService(\"Debris\")\n" +
                       "local Players = game:GetService(\"Players\")\n\n" +
                       "local function fireKiBlast(creator: Model, originPos: Vector3, targetPos: Vector3)\n" +
                       "    local blast = Instance.new(\"Part\")\n" +
                       "    blast.Size = Vector3.new(2, 2, 2)\n" +
                       "    blast.Color = Color3.fromRGB(100, 200, 255) -- Ki color\n" +
                       "    blast.Material = Enum.Material.Neon\n" +
                       "    blast.Shape = Enum.PartType.Ball\n" +
                       "    blast.CanCollide = false\n" +
                       "    blast.Position = originPos\n" +
                       "    blast.Parent = workspace\n\n" +
                       "    -- Linear Velocity movement setup\n" +
                       "    local attachment = Instance.new(\"Attachment\", blast)\n" +
                       "    local linearVelocity = Instance.new(\"LinearVelocity\", blast)\n" +
                       "    linearVelocity.Attachment0 = attachment\n" +
                       "    linearVelocity.MaxForce = math.huge\n" +
                       "    \n" +
                       "    local direction = (targetPos - originPos).Unit\n" +
                       "    linearVelocity.VectorVelocity = direction * 120 -- Speed studs/sec\n\n" +
                       "    -- Lifetime limit (3 seconds)\n" +
                       "    Debris:AddItem(blast, 3)\n\n" +
                       "    -- Hit detection\n" +
                       "    local hitConn = nil\n" +
                       "    hitConn = blast.Touched:Connect(function(hit)\n" +
                       "        if hit:IsDescendantOf(creator) then return end -- Don't hit self\n" +
                       "        \n" +
                       "        -- Trigger explosion VFX\n" +
                       "        local hitChar = hit.Parent\n" +
                       "        local hitHum = hitChar and hitChar:FindFirstChildOfClass(\"Humanoid\")\n" +
                       "        \n" +
                       "        if hitHum then\n" +
                       "            hitHum:TakeDamage(10) -- Damage opponent\n" +
                       "        end\n\n" +
                       "        -- Spawn explosion part\n" +
                       "        local exp = Instance.new(\"Explosion\")\n" +
                       "        exp.Position = blast.Position\n" +
                       "        exp.BlastRadius = 5\n" +
                       "        exp.BlastPressure = 0\n" +
                       "        exp.Parent = workspace\n" +
                       "        \n" +
                       "        blast:Destroy()\n" +
                       "        if hitConn then hitConn:Disconnect() end\n" +
                       "    end)\n" +
                       "end\n";
            }

            _dbzOutputBox.Text = code;
        }
        #endregion

        #region View 5: Admin Panel & Player Data Restorer
        private void LoadAdminDataRestorerView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel();

            var title = CreateSectionHeader("🛡️ Admin Panel & Player DataStore Restorer");
            panel.Children.Add(title);

            var hint = CreateHintText("Generates production-grade Roblox Studio Luau DataStore restoration scripts, snapshot rollbacks, and in-game :restore admin command handlers.");
            panel.Children.Add(hint);

            var formGrid = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var col1 = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            col1.Children.Add(CreateLabel("Target Player UserId / Key:"));
            _admUserIdBox = CreateTextBox();
            _admUserIdBox.Text = "12345678";
            _admUserIdBox.TextChanged += (s, e) => GenerateAdminRestorerCode();
            col1.Children.Add(_admUserIdBox);

            col1.Children.Add(CreateLabel("DataStore Name:"));
            _admDataStoreBox = CreateTextBox();
            _admDataStoreBox.Text = "PlayerData_v1";
            _admDataStoreBox.TextChanged += (s, e) => GenerateAdminRestorerCode();
            col1.Children.Add(_admDataStoreBox);

            Grid.SetColumn(col1, 0);
            formGrid.Children.Add(col1);

            var col2 = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
            col2.Children.Add(CreateLabel("Data Schema Preset:"));
            _admPresetCombo = CreateComboBox(new[]
            {
                "Dragon Blox / RPG Profile (Stats, Forms, Level, Zeni)",
                "Standard Leaderstats (Coins, Gems, Rebirths)",
                "ProfileStore / Session-Locked Dictionary",
                "Custom JSON / Table Payload"
            });
            _admPresetCombo.SelectedIndex = 0;
            _admPresetCombo.SelectionChanged += (s, e) =>
            {
                UpdateAdmPayloadFromPreset();
                GenerateAdminRestorerCode();
            };
            col2.Children.Add(_admPresetCombo);

            col2.Children.Add(CreateLabel("Restoration Strategy:"));
            _admStrategyCombo = CreateComboBox(new[]
            {
                "Direct Inject / Force Overwrite (Command Bar / Server)",
                "DataStore Version Rollback (Inspect & Restore Snapshot)",
                "In-Game Admin Command Handler (:restoreplayer)",
                "Automated Rolling DataStore Backup Engine"
            });
            _admStrategyCombo.SelectedIndex = 0;
            _admStrategyCombo.SelectionChanged += (s, e) => GenerateAdminRestorerCode();
            col2.Children.Add(_admStrategyCombo);

            Grid.SetColumn(col2, 1);
            formGrid.Children.Add(col2);
            panel.Children.Add(formGrid);

            panel.Children.Add(CreateLabel("Restoration Payload (JSON / Table):"));
            _admCustomPayloadBox = new TextBox
            {
                Height = 65,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 10,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(4),
                Text = "{\n  \"Strength\": 5000000,\n  \"Agility\": 5000000,\n  \"Defense\": 5000000,\n  \"Ki\": 5000000,\n  \"Level\": 500,\n  \"Zeni\": 10000000,\n  \"UnlockedForms\": [\"SuperSaiyan\", \"SSJ2\", \"SSJ3\", \"SSJGod\", \"SSJBlue\"]\n}"
            };
            _admCustomPayloadBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _admCustomPayloadBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            _admCustomPayloadBox.TextChanged += (s, e) => GenerateAdminRestorerCode();
            panel.Children.Add(_admCustomPayloadBox);

            _admOutputBox = new TextBox
            {
                Height = 170,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 10,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 8, 0, 0)
            };
            _admOutputBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _admOutputBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_admOutputBox);

            var copyBtn = CreateButton("📋 Copy Data Restorer Script to Clipboard", (s, e) => CopyToClipboard(_admOutputBox.Text));
            copyBtn.FontWeight = FontWeights.Bold;
            copyBtn.Margin = new Thickness(0, 8, 0, 0);
            panel.Children.Add(copyBtn);

            scroll.Content = panel;
            _contentGrid.Children.Add(scroll);

            GenerateAdminRestorerCode();
        }

        private void UpdateAdmPayloadFromPreset()
        {
            if (_admPresetCombo == null || _admCustomPayloadBox == null) return;
            int idx = _admPresetCombo.SelectedIndex;
            if (idx == 0)
            {
                _admCustomPayloadBox.Text = "{\n  \"Strength\": 5000000,\n  \"Agility\": 5000000,\n  \"Defense\": 5000000,\n  \"Ki\": 5000000,\n  \"Level\": 500,\n  \"Zeni\": 10000000,\n  \"UnlockedForms\": [\"SuperSaiyan\", \"SSJ2\", \"SSJ3\", \"SSJGod\", \"SSJBlue\"]\n}";
            }
            else if (idx == 1)
            {
                _admCustomPayloadBox.Text = "{\n  \"Coins\": 2500000,\n  \"Gems\": 50000,\n  \"Level\": 100,\n  \"Rebirths\": 15,\n  \"Inventory\": [\"DragonBlade\", \"KiArmor\", \"Wings\"]\n}";
            }
            else if (idx == 2)
            {
                _admCustomPayloadBox.Text = "{\n  \"MetaData\": {\"ProfileCreated\": 1690000000, \"SessionLock\": nil},\n  \"Data\": {\n    \"Stats\": {\"Strength\": 1000000, \"Defense\": 1000000},\n    \"Inventory\": {\"Slots\": 50, \"Items\": [\"Item_001\", \"Item_002\"]}\n  }\n}";
            }
        }

        private void GenerateAdminRestorerCode()
        {
            if (_admOutputBox == null || _admUserIdBox == null || _admDataStoreBox == null || _admStrategyCombo == null) return;

            string userId = _admUserIdBox.Text.Trim();
            if (string.IsNullOrEmpty(userId)) userId = "12345678";

            string storeName = _admDataStoreBox.Text.Trim();
            if (string.IsNullOrEmpty(storeName)) storeName = "PlayerData_v1";

            int methodIdx = _admStrategyCombo.SelectedIndex;
            string payloadText = _admCustomPayloadBox?.Text.Trim() ?? "{}";

            var sb = new StringBuilder();

            if (methodIdx == 0) // Direct Inject / Force Overwrite
            {
                sb.AppendLine($"-- HEAPLIT DATASTORE DIRECT RESTORER & OVERRIDE PROTOCOL");
                sb.AppendLine($"-- Target Player UserId: {userId} | DataStore: {storeName}");
                sb.AppendLine($"local DataStoreService = game:GetService(\"DataStoreService\")");
                sb.AppendLine($"local HttpService = game:GetService(\"HttpService\")");
                sb.AppendLine($"local Players = game:GetService(\"Players\")");
                sb.AppendLine($"local dataStore = DataStoreService:GetDataStore(\"{storeName}\")");
                sb.AppendLine($"local TARGET_USER_ID = {userId}");
                sb.AppendLine($"local TARGET_KEY = \"Player_\" .. TARGET_USER_ID");
                sb.AppendLine();
                sb.AppendLine($"local RAW_PAYLOAD_JSON = [==[{payloadText}]==]");
                sb.AppendLine($"local success, restoredData = pcall(function() return HttpService:JSONDecode(RAW_PAYLOAD_JSON) end)");
                sb.AppendLine($"if not success then warn(\"❌ Failed to decode payload JSON\") return end");
                sb.AppendLine();
                sb.AppendLine($"-- Snapshot backup prior to write");
                sb.AppendLine($"pcall(function()");
                sb.AppendLine($"    local old = dataStore:GetAsync(TARGET_KEY)");
                sb.AppendLine($"    if old then dataStore:SetAsync(\"BACKUP_\" .. TARGET_KEY .. \"_\" .. os.time(), old) end");
                sb.AppendLine($"end)");
                sb.AppendLine();
                sb.AppendLine($"local setSuccess, setErr = pcall(function()");
                sb.AppendLine($"    local setOptions = Instance.new(\"DataStoreSetOptions\")");
                sb.AppendLine($"    setOptions:SetMetadata({{ RestoredBy = \"HeaplitAdminRestorer\", RestoreTime = os.time() }})");
                sb.AppendLine($"    dataStore:SetAsync(TARGET_KEY, restoredData, {{ TARGET_USER_ID }}, setOptions)");
                sb.AppendLine($"end)");
                sb.AppendLine();
                sb.AppendLine($"if setSuccess then");
                sb.AppendLine($"    print(\"✅ Restored DataStore for UserId \" .. TARGET_USER_ID)");
                sb.AppendLine($"    local player = Players:GetPlayerByUserId(TARGET_USER_ID)");
                sb.AppendLine($"    if player then player:Kick(\"Your data has been restored by Server Admin. Rejoin to refresh.\") end");
                sb.AppendLine($"else");
                sb.AppendLine($"    warn(\"❌ Failed to restore: \" .. tostring(setErr))");
                sb.AppendLine($"end");
            }
            else if (methodIdx == 1) // Version Rollback
            {
                sb.AppendLine($"-- HEAPLIT DATASTORE VERSION ROLLBACK & SNAPSHOT RECOVERY");
                sb.AppendLine($"local DataStoreService = game:GetService(\"DataStoreService\")");
                sb.AppendLine($"local dataStore = DataStoreService:GetDataStore(\"{storeName}\")");
                sb.AppendLine($"local TARGET_KEY = \"Player_{userId}\"");
                sb.AppendLine();
                sb.AppendLine($"local success, pages = pcall(function() return dataStore:ListVersionsAsync(TARGET_KEY, Enum.SortDirection.Descending) end)");
                sb.AppendLine($"if not success then warn(\"❌ ListVersionsAsync failed\") return end");
                sb.AppendLine();
                sb.AppendLine($"local versions = pages:GetCurrentPage()");
                sb.AppendLine($"for i, v in ipairs(versions) do");
                sb.AppendLine($"    print(string.format(\"[%d] VersionId: %s | Created: %s\", i, v.Version, os.date(\"%Y-%m-%d %H:%M:%S\", math.floor(v.CreatedTime / 1000))))");
                sb.AppendLine($"end");
                sb.AppendLine();
                sb.AppendLine($"if #versions >= 2 then");
                sb.AppendLine($"    local preCorruptionVersion = versions[2].Version");
                sb.AppendLine($"    local getSuccess, versionData = pcall(function() return dataStore:GetVersionAsync(TARGET_KEY, preCorruptionVersion) end)");
                sb.AppendLine($"    if getSuccess and versionData ~= nil then");
                sb.AppendLine($"        dataStore:SetAsync(TARGET_KEY, versionData)");
                sb.AppendLine($"        print(\"✅ Rolled back to version: \" .. preCorruptionVersion)");
                sb.AppendLine($"    end");
                sb.AppendLine($"end");
            }
            else if (methodIdx == 2) // In-Game Admin Command
            {
                sb.AppendLine($"-- Place in ServerScriptService -> AdminDataCommands");
                sb.AppendLine($"local Players = game:GetService(\"Players\")");
                sb.AppendLine($"local DataStoreService = game:GetService(\"DataStoreService\")");
                sb.AppendLine($"local dataStore = DataStoreService:GetDataStore(\"{storeName}\")");
                sb.AppendLine($"local ADMINS = {{ [game.CreatorId] = true }}");
                sb.AppendLine($"local RESTORE_PAYLOAD = [==[{payloadText}]==]");
                sb.AppendLine();
                sb.AppendLine($"Players.PlayerAdded:Connect(function(player)");
                sb.AppendLine($"    player.Chatted:Connect(function(msg)");
                sb.AppendLine($"        if not ADMINS[player.UserId] and player.UserId ~= game.CreatorId then return end");
                sb.AppendLine($"        local tokens = string.split(msg, \" \")");
                sb.AppendLine($"        if tokens[1]:lower() == \":restore\" and tokens[2] then");
                sb.AppendLine($"            local targetId = tonumber(tokens[2]) or (Players:FindFirstChild(tokens[2]) and Players[tokens[2]].UserId)");
                sb.AppendLine($"            if targetId then");
                sb.AppendLine($"                local decoded = game:GetService(\"HttpService\"):JSONDecode(RESTORE_PAYLOAD)");
                sb.AppendLine($"                dataStore:SetAsync(\"Player_\" .. targetId, decoded)");
                sb.AppendLine($"                print(\"⚡ Restored data for: \" .. targetId)");
                sb.AppendLine($"            end");
                sb.AppendLine($"        end");
                sb.AppendLine($"    end)");
                sb.AppendLine($"end)");
            }
            else // Rolling Backup System
            {
                sb.AppendLine($"-- Place in ServerScriptService -> DataBackupService");
                sb.AppendLine($"local DataStoreService = game:GetService(\"DataStoreService\")");
                sb.AppendLine($"local PRIMARY_STORE = DataStoreService:GetDataStore(\"{storeName}\")");
                sb.AppendLine($"local BACKUP_STORE = DataStoreService:GetDataStore(\"{storeName}_Snapshots\")");
                sb.AppendLine();
                sb.AppendLine($"local DataBackup = {{}}");
                sb.AppendLine($"function DataBackup.SaveSnapshot(userId: number, data: table)");
                sb.AppendLine($"    pcall(function() BACKUP_STORE:SetAsync(string.format(\"Player_%d_%d\", userId, os.time()), data) end)");
                sb.AppendLine($"end");
                sb.AppendLine($"function DataBackup.RestoreLatestSnapshot(userId: number): boolean");
                sb.AppendLine($"    local s, pages = pcall(function() return BACKUP_STORE:ListKeysAsync(\"Player_\" .. userId, 5) end)");
                sb.AppendLine($"    if not s then return false end");
                sb.AppendLine($"    local keys = pages:GetCurrentPage()");
                sb.AppendLine($"    if #keys == 0 then return false end");
                sb.AppendLine($"    local s2, data = pcall(function() return BACKUP_STORE:GetAsync(keys[#keys].KeyName) end)");
                sb.AppendLine($"    if s2 and data then PRIMARY_STORE:SetAsync(\"Player_\" .. userId, data) return true end");
                sb.AppendLine($"    return false");
                sb.AppendLine($"end");
                sb.AppendLine($"return DataBackup");
            }

            _admOutputBox.Text = sb.ToString();
        }
        #endregion

        #region View 6: AI Context Builder
        private void LoadAiContextBuilderView()
        {
            var panel = new StackPanel();

            var title = CreateSectionHeader("🤖 Local AI Context Builder Prompt Assembler");
            panel.Children.Add(title);

            var hint = CreateHintText("Select local Luau files from your workspace directory. This tool automatically bundles them into a structured AI context prompt that you can copy-paste to get instant coding advice.");
            panel.Children.Add(hint);

            panel.Children.Add(CreateLabel("Select codebase scripts to bundle:"));
            var scroll = new ScrollViewer();
            _aiFilesListBox = new ListBox
            {
                Height = 120,
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 8),
                ItemContainerStyle = (Style)Application.Current.FindResource("ResultItemStyle")
            };
            ScrollViewer.SetVerticalScrollBarVisibility(_aiFilesListBox, ScrollBarVisibility.Auto);
            _aiFilesListBox.SetResourceReference(ListBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_aiFilesListBox);

            var scanBtn = CreateButton("🔄 Load Workspace Files List", (s, e) => ScanWorkspaceFilesForAi());
            panel.Children.Add(scanBtn);

            var assembleBtn = CreateButton("🤖 Assemble AI Prompt & Copy Context", (s, e) => AssembleAiPrompt());
            assembleBtn.Height = 30;
            assembleBtn.FontWeight = FontWeights.Bold;
            assembleBtn.Margin = new Thickness(0, 10, 0, 10);
            panel.Children.Add(assembleBtn);

            _aiPromptOutputBox = new TextBox
            {
                Height = 150,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 10,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(6)
            };
            _aiPromptOutputBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _aiPromptOutputBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_aiPromptOutputBox);

            _contentGrid.Children.Add(panel);

            ScanWorkspaceFilesForAi();
        }

        private void ScanWorkspaceFilesForAi()
        {
            if (_aiFilesListBox == null || _robloxProjectPathBox == null) return;

            string targetDir = _robloxProjectPathBox.Text.Trim();
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            {
                var label = new ListBoxItem { Content = "Please configure a valid Rings Root Path first in the Code Analyzer tab.", FontSize = 11 };
                label.SetResourceReference(ListBoxItem.ForegroundProperty, "TextSecondaryBrush");
                _aiFilesListBox.Items.Clear();
                _aiFilesListBox.Items.Add(label);
                return;
            }

            try
            {
                var files = Directory.GetFiles(targetDir, "*.lua", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(targetDir, "*.luau", SearchOption.AllDirectories))
                    .OrderBy(x => x)
                    .ToList();

                _aiFilesListBox.Items.Clear();
                foreach (var file in files)
                {
                    string relativePath = file.Substring(targetDir.Length).TrimStart('\\', '/');
                    var checkBox = new CheckBox
                    {
                        Content = relativePath,
                        Tag = file,
                        FontSize = 11,
                        Margin = new Thickness(2)
                    };
                    checkBox.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
                    _aiFilesListBox.Items.Add(checkBox);
                }

                if (files.Count == 0)
                {
                    var label = new ListBoxItem { Content = "No .lua/.luau files found in the directory.", FontSize = 11 };
                    label.SetResourceReference(ListBoxItem.ForegroundProperty, "TextSecondaryBrush");
                    _aiFilesListBox.Items.Clear();
                    _aiFilesListBox.Items.Add(label);
                }
            }
            catch (Exception ex)
            {
                _aiFilesListBox.Items.Clear();
                _aiFilesListBox.Items.Add(new ListBoxItem { Content = $"Error listing files: {ex.Message}" });
            }
        }

        private void AssembleAiPrompt()
        {
            if (_aiFilesListBox == null || _aiPromptOutputBox == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("I am building a Roblox DBZ (Dragon Ball Z) action game.");
            sb.AppendLine("Please help me refactor or write a new feature based on the following local script context:");
            sb.AppendLine();

            int included = 0;
            foreach (var item in _aiFilesListBox.Items)
            {
                if (item is CheckBox cb && cb.IsChecked == true && cb.Tag is string filePath && File.Exists(filePath))
                {
                    string relativePath = cb.Content.ToString() ?? Path.GetFileName(filePath);
                    sb.AppendLine($"### File: `{relativePath}`");
                    sb.AppendLine("```lua");
                    try
                    {
                        sb.AppendLine(File.ReadAllText(filePath));
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"-- Error reading file: {ex.Message}");
                    }
                    sb.AppendLine("```");
                    sb.AppendLine();
                    included++;
                }
            }

            if (included == 0)
            {
                _aiPromptOutputBox.Text = "Please check one or more files in the list first!";
                return;
            }

            sb.AppendLine("What is the best way to implement...");
            string promptText = sb.ToString();
            _aiPromptOutputBox.Text = promptText;

            CopyToClipboard(promptText);
        }
        #endregion

        #region View 6: Port Codebase → Studio
        private void LoadPortCodebaseView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };

            panel.Children.Add(CreateSectionHeader("📦 Port Codebase → Studio"));

            panel.Children.Add(CreateSubHeader("STEP 1 — Load Source Codebase into Heaplit"));
            panel.Children.Add(CreateHintText("Point to any folder or .rbxlx file. Heaplit stores all scripts in memory until you restart."));

            var srcGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            srcGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            srcGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            srcGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _portSourcePathBox = CreateTextBox();
            string defaultUserDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _portSourcePathBox.Text = System.IO.Path.Combine(defaultUserDir, "Downloads", "Projects", "Dragon Blox Essence");
            _portSourcePathBox.ToolTip = "Folder of .lua/.luau files  OR  a .rbxlx / .rbxl place file";
            Grid.SetColumn(_portSourcePathBox, 0);
            srcGrid.Children.Add(_portSourcePathBox);

            var bfBtn = MakeSmallButton("📂 Folder", () =>
            {
                var d = new System.Windows.Forms.FolderBrowserDialog { UseDescriptionForTitle = true, Description = "Pick source project root" };
                if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) _portSourcePathBox.Text = d.SelectedPath;
            }, leftMargin: 6);
            Grid.SetColumn(bfBtn, 1); srcGrid.Children.Add(bfBtn);

            var bxBtn = MakeSmallButton("🗂️ .rbxlx", () =>
            {
                var d = new Microsoft.Win32.OpenFileDialog { Title = "Select place file", Filter = "Roblox Place|*.rbxlx;*.rbxl", InitialDirectory = System.IO.Path.Combine(defaultUserDir, "Downloads") };
                if (d.ShowDialog() == true) _portSourcePathBox.Text = d.FileName;
            }, leftMargin: 4);
            Grid.SetColumn(bxBtn, 2); srcGrid.Children.Add(bxBtn);
            panel.Children.Add(srcGrid);

            _portStatusLabel = new TextBlock { FontSize = 11, Margin = new Thickness(0, 4, 0, 2), TextWrapping = TextWrapping.Wrap };
            _portStatusLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            panel.Children.Add(_portStatusLabel);
            RefreshPortStatus();

            var loadBtn = CreateButton("📥 Load & Store Codebase in Heaplit", (s, e) => LoadPortCurrent());
            loadBtn.FontWeight = FontWeights.Bold;
            panel.Children.Add(loadBtn);

            panel.Children.Add(CreateLabel("Stored script tree:"));
            _portFileTree = new TreeView
            {
                Height = 110,
                Margin = new Thickness(0, 0, 0, 6),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1)
            };
            _portFileTree.SetResourceReference(TreeView.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_portFileTree);
            RebuildPortTree();

            panel.Children.Add(CreateSubHeader("STEP 2 — Paste into Target Game"));
            panel.Children.Add(CreateHintText("Pick where to install, then copy the installer script. Open the target game in Roblox Studio, go to View → Command Bar, paste, and hit Enter."));

            var targetGrid = new Grid { Margin = new Thickness(0, 4, 0, 6) };
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var svcCol = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            svcCol.Children.Add(CreateLabel("Target Service:"));
            _portTargetServiceCombo = CreateComboBox(new[] { "ReplicatedStorage", "ServerScriptService", "ServerStorage", "StarterPlayerScripts", "StarterCharacterScripts", "StarterGui" });
            _portTargetServiceCombo.SelectedIndex = 0;
            svcCol.Children.Add(_portTargetServiceCombo);
            Grid.SetColumn(svcCol, 0); targetGrid.Children.Add(svcCol);

            var subCol = new StackPanel();
            subCol.Children.Add(CreateLabel("Subfolder (leave blank for root):"));
            _portSubfolderCombo = CreateComboBox(new[] { "", "Modules", "Shared", "Utils", "Client", "Server", "Systems", "RingWorld" });
            _portSubfolderCombo.IsEditable = true;
            _portSubfolderCombo.SelectedIndex = 0;
            subCol.Children.Add(_portSubfolderCombo);
            Grid.SetColumn(subCol, 1); targetGrid.Children.Add(subCol);
            panel.Children.Add(targetGrid);

            var genBtn = CreateButton("⚙️ Generate & Copy Installer Script", (s, e) => GenerateAndCopyInstaller());
            genBtn.FontWeight = FontWeights.Bold;
            panel.Children.Add(genBtn);

            _portOutputBox = new TextBox
            {
                Height = 120,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 10,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 6, 0, 0)
            };
            _portOutputBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _portOutputBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_portOutputBox);

            panel.Children.Add(CreateSubHeader("BONUS — Export Stored Codebase as Rojo Project"));
            panel.Children.Add(CreateHintText("Writes the stored scripts to a local folder as .lua files + default.project.json ready to sync with Rojo."));

            var rojoGrid = new Grid { Margin = new Thickness(0, 4, 0, 6) };
            rojoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rojoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var _rojoOutBox = CreateTextBox();
            _rojoOutBox.Text = System.IO.Path.Combine(defaultUserDir, "Downloads", "Projects", "StarfallRojo");
            _rojoOutBox.ToolTip = "Output folder for the Rojo project";
            Grid.SetColumn(_rojoOutBox, 0); rojoGrid.Children.Add(_rojoOutBox);

            var rojoPickBtn = MakeSmallButton("📂", () =>
            {
                var d = new System.Windows.Forms.FolderBrowserDialog { UseDescriptionForTitle = true, Description = "Pick Rojo output folder" };
                if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) _rojoOutBox.Text = d.SelectedPath;
            }, leftMargin: 4);
            Grid.SetColumn(rojoPickBtn, 1); rojoGrid.Children.Add(rojoPickBtn);
            panel.Children.Add(rojoGrid);

            var rojoBtn = CreateButton("📁 Write Rojo Project to Disk", (s, e) => ExportRojoProject(_rojoOutBox.Text));
            panel.Children.Add(rojoBtn);

            scroll.Content = panel;
            _contentGrid.Children.Add(scroll);
        }

        private Button MakeSmallButton(string label, Action onClick, int leftMargin = 0)
        {
            var btn = new Button
            {
                Content = label,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(leftMargin, 0, 0, 0),
                Cursor = Cursors.Hand,
                FontSize = 11
            };
            btn.SetResourceReference(Button.BackgroundProperty, "ButtonBackgroundBrush");
            btn.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
            btn.Click += (_, _) => onClick();
            return btn;
        }

        private readonly record struct PortEntry(string RelativePath, string FullPath, string ScriptType, string Source);
        private List<PortEntry> _portEntries = new();
        private string _portLoadedFrom = "";

        private void RefreshPortStatus()
        {
            if (_portStatusLabel == null) return;
            if (_portEntries.Count == 0)
                _portStatusLabel.Text = "Nothing loaded yet.";
            else
                _portStatusLabel.Text = $"✅ {_portEntries.Count} scripts loaded from: {_portLoadedFrom}";
        }

        private void RebuildPortTree()
        {
            if (_portFileTree == null) return;
            _portFileTree.Items.Clear();
            if (_portEntries.Count == 0) return;

            var root = new TreeViewItem { Header = $"📦 {Path.GetFileName(_portLoadedFrom)} ({_portEntries.Count} scripts)", IsExpanded = true };
            root.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");

            var dirMap = new Dictionary<string, TreeViewItem>();

            foreach (var e in _portEntries)
            {
                string[] parts = e.RelativePath.Replace('\\', '/').Split('/');
                TreeViewItem parent = root;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string key = string.Join("/", parts.Take(i + 1));
                    if (!dirMap.TryGetValue(key, out var dir))
                    {
                        dir = new TreeViewItem { Header = $"📁 {parts[i]}", IsExpanded = true };
                        dir.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");
                        parent.Items.Add(dir);
                        dirMap[key] = dir;
                    }
                    parent = dir;
                }

                string icon = e.ScriptType == "LocalScript" ? "🟦" : e.ScriptType == "Script" ? "🟩" : "🟨";
                var leaf = new TreeViewItem { Header = $"{icon} {Path.GetFileNameWithoutExtension(parts.Last())} [{e.ScriptType}]" };
                leaf.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");
                parent.Items.Add(leaf);
            }

            _portFileTree.Items.Add(root);
        }

        private void LoadPortCurrent()
        {
            if (_portSourcePathBox == null) return;
            string src = _portSourcePathBox.Text.Trim();
            _portEntries.Clear();

            if (string.IsNullOrEmpty(src)) { _portStatusLabel!.Text = "❌ Enter a path first."; return; }

            bool isPlace = (src.EndsWith(".rbxlx", StringComparison.OrdinalIgnoreCase) ||
                            src.EndsWith(".rbxl",  StringComparison.OrdinalIgnoreCase)) && File.Exists(src);

            if (isPlace)
                LoadFromRbxlx(src);
            else if (Directory.Exists(src))
                LoadFromFolder(src);
            else
            {
                _portStatusLabel!.Text = "❌ Path not found.";
                return;
            }

            _portLoadedFrom = src;
            RefreshPortStatus();
            RebuildPortTree();
        }

        private void LoadFromFolder(string root)
        {
            var files = Directory.GetFiles(root, "*.lua", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(root, "*.luau", SearchOption.AllDirectories))
                .OrderBy(f => f);

            foreach (var file in files)
            {
                string rel = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
                string first = File.ReadLines(file).FirstOrDefault() ?? "";
                string cls = first.Contains("@LocalScript") ? "LocalScript"
                           : first.Contains("@Script")      ? "Script"
                           : "ModuleScript";
                _portEntries.Add(new PortEntry(rel, file, cls, File.ReadAllText(file)));
            }
        }

        private void LoadFromRbxlx(string filePath)
        {
            try
            {
                var doc = XDocument.Load(filePath);
                var scriptClasses = new HashSet<string> { "Script", "LocalScript", "ModuleScript" };

                foreach (var item in doc.Descendants("Item").Where(el => scriptClasses.Contains(el.Attribute("class")?.Value ?? "")))
                {
                    string cls  = item.Attribute("class")?.Value ?? "ModuleScript";
                    string name = item.Descendants("string").FirstOrDefault(p => p.Attribute("name")?.Value == "Name")?.Value ?? "Unknown";
                    string src  = item.Descendants("ProtectedString").Concat(item.Descendants("string"))
                                      .FirstOrDefault(p => p.Attribute("name")?.Value == "Source")?.Value ?? "";

                    var parts = new List<string> { name };
                    var anc = item.Parent?.Parent;
                    while (anc?.Name.LocalName == "Item")
                    {
                        string aname = anc.Descendants("string").FirstOrDefault(p => p.Attribute("name")?.Value == "Name")?.Value ?? "?";
                        parts.Insert(0, aname);
                        anc = anc.Parent?.Parent;
                    }

                    string rel = string.Join("/", parts);
                    _portEntries.Add(new PortEntry(rel, filePath, cls, src));
                }
            }
            catch (Exception ex)
            {
                _portStatusLabel!.Text = $"❌ XML error: {ex.Message}";
            }
        }

        private void GenerateAndCopyInstaller()
        {
            if (_portOutputBox == null || _portTargetServiceCombo == null || _portSubfolderCombo == null) return;

            if (_portEntries.Count == 0)
            {
                _portOutputBox.Text = "-- Nothing loaded. Do Step 1 first.";
                return;
            }

            string svc = (_portTargetServiceCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                       ?? _portTargetServiceCombo.Text ?? "ReplicatedStorage";
            string sub = _portSubfolderCombo.Text.Trim();
            bool hasSub = !string.IsNullOrEmpty(sub);

            var sb = new StringBuilder();
            sb.AppendLine($"-- HEAPLIT CODEBASE PORTER | {_portEntries.Count} scripts | {Path.GetFileName(_portLoadedFrom)} → {svc}{(hasSub ? "/" + sub : "")} | {DateTime.Now:HH:mm}");
            sb.AppendLine($"local svc = game:GetService(\"{svc}\")");

            if (hasSub)
            {
                sb.AppendLine($"local root = svc:FindFirstChild(\"{sub}\") or Instance.new(\"Folder\", svc)");
                sb.AppendLine($"root.Name = \"{sub}\"");
            }
            else
            {
                sb.AppendLine("local root = svc");
            }

            sb.AppendLine("local function mkf(p, n, c) local x=p:FindFirstChild(n) if x then return x end local i=Instance.new(c or\"Folder\") i.Name=n i.Parent=p return i end");
            sb.AppendLine();

            int i = 0;
            foreach (var e in _portEntries)
            {
                i++;
                string[] parts = e.RelativePath.Replace('\\', '/').Split('/');
                string scriptName = Path.GetFileNameWithoutExtension(parts.Last());

                if (parts.Length > 1)
                {
                    sb.Append($"do local c=root");
                    for (int j = 0; j < parts.Length - 1; j++)
                        sb.Append($" c=mkf(c,\"{parts[j]}\")");
                    string esc = EscapeLuaString(e.Source);
                    sb.Append($" local s=mkf(c,\"{scriptName}\",\"{e.ScriptType}\")");
                    if (esc.Length <= 900) sb.Append($" s.Source=\"{esc}\"");
                    sb.AppendLine(" end");
                }
                else
                {
                    string esc = EscapeLuaString(e.Source);
                    sb.Append($"do local s=mkf(root,\"{scriptName}\",\"{e.ScriptType}\")");
                    if (esc.Length <= 900) sb.Append($" s.Source=\"{esc}\"");
                    sb.AppendLine(" end");
                }
            }

            sb.AppendLine($"print(\"[HEAPLIT] ✅ Ported {_portEntries.Count} scripts into {svc}{(hasSub ? "/" + sub : "")}\")");

            string result = sb.ToString();
            _portOutputBox.Text = result;
            CopyToClipboard(result);
        }

        private static string EscapeLuaString(string s) =>
            s.Replace("\\", "\\\\")
             .Replace("\"", "\\\"")
             .Replace("\r\n", "\\n")
             .Replace("\n",   "\\n")
             .Replace("\t",   "\\t");

        private void ExportRojoProject(string outputDir)
        {
            if (_portEntries.Count == 0)
            {
                MessageBox.Show("Load a codebase first (Step 1).", "No codebase loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(outputDir);
                string srcDir = Path.Combine(outputDir, "src");
                Directory.CreateDirectory(srcDir);

                foreach (var e in _portEntries)
                {
                    string rel = e.RelativePath.Replace('/', Path.DirectorySeparatorChar);
                    string destPath = Path.Combine(srcDir, rel.EndsWith(".lua") || rel.EndsWith(".luau") ? rel : rel + ".lua");
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.WriteAllText(destPath, e.Source);
                }

                string svc = (_portTargetServiceCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString()
                           ?? _portTargetServiceCombo?.Text ?? "ReplicatedStorage";
                string sub = _portSubfolderCombo?.Text.Trim() ?? "";
                bool hasSub = !string.IsNullOrEmpty(sub);

                string treePath = hasSub ? $"\"{svc}\": {{\"{sub}\": {{\"$path\": \"src\"}}}}" : $"\"{svc}\": {{\"$path\": \"src\"}}";

                string projectJson = $@"{{
  ""name"": ""{Path.GetFileName(_portLoadedFrom).Replace(".rbxlx","").Replace(".rbxl","")}_port"",
  ""tree"": {{
    ""$className"": ""DataModel"",
    {treePath}
  }}
}}";
                File.WriteAllText(Path.Combine(outputDir, "default.project.json"), projectJson);

                File.WriteAllText(Path.Combine(outputDir, "aftman.toml"),
                    "[tools]\nrojo = \"rojo-rbx/rojo@7.4.4\"\n");

                MessageBox.Show(
                    $"✅ Rojo project written to:\n{outputDir}\n\nFiles: {_portEntries.Count} scripts\nSync command:\n  rojo serve default.project.json",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region View 7: Luau Code Generators
        private void ScanPortSourceAndPreview()
        {
            if (_portSourcePathBox == null || _portFileTree == null || _portStatusLabel == null) return;

            string sourcePath = _portSourcePathBox.Text.Trim();
            _portFileTree.Items.Clear();
            _portEntries.Clear();

            if (string.IsNullOrEmpty(sourcePath) || (!File.Exists(sourcePath) && !Directory.Exists(sourcePath)))
            {
                _portStatusLabel.Text = "❌ Path does not exist. Enter a valid folder or .rbxlx file path above.";
                return;
            }

            if ((sourcePath.EndsWith(".rbxlx", StringComparison.OrdinalIgnoreCase) ||
                 sourcePath.EndsWith(".rbxl",  StringComparison.OrdinalIgnoreCase)) && File.Exists(sourcePath))
            {
                ScanRbxlx(sourcePath);
            }
            else if (Directory.Exists(sourcePath))
            {
                ScanFolder(sourcePath);
            }
            else
            {
                _portStatusLabel.Text = "❌ Not recognised as a folder or .rbxlx/.rbxl file.";
                return;
            }

            _portStatusLabel.Text = $"✅ Found {_portEntries.Count} scripts.";
        }

        private void ScanFolder(string root)
        {
            var allFiles = Directory.GetFiles(root, "*.lua", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(root, "*.luau", SearchOption.AllDirectories))
                .OrderBy(x => x);

            var rootNode = new TreeViewItem { Header = $"📁 {Path.GetFileName(root)}", IsExpanded = true };
            _portFileTree!.Items.Add(rootNode);

            var nodeMap = new Dictionary<string, TreeViewItem> { { root, rootNode } };

            foreach (var file in allFiles)
            {
                string rel = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
                string firstLine = File.ReadLines(file).FirstOrDefault() ?? "";
                string scriptType = firstLine.Contains("@LocalScript") ? "LocalScript"
                    : firstLine.Contains("@Script") ? "Script"
                    : "ModuleScript";

                _portEntries.Add(new PortEntry(rel, file, scriptType, File.ReadAllText(file)));

                string[] parts = rel.Split(Path.DirectorySeparatorChar);
                TreeViewItem parent = rootNode;
                string running = root;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    running = Path.Combine(running, parts[i]);
                    if (!nodeMap.TryGetValue(running, out var dirNode))
                    {
                        dirNode = new TreeViewItem { Header = $"📁 {parts[i]}", IsExpanded = true };
                        dirNode.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");
                        parent.Items.Add(dirNode);
                        nodeMap[running] = dirNode;
                    }
                    parent = dirNode;
                }

                var leaf = new TreeViewItem
                {
                    Header = $"📄 {Path.GetFileNameWithoutExtension(parts.Last())}  [{scriptType}]"
                };
                leaf.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");
                parent.Items.Add(leaf);
            }
        }

        private void ScanRbxlx(string filePath)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Load(filePath);

                var scriptClasses = new HashSet<string> { "Script", "LocalScript", "ModuleScript" };
                var items = doc.Descendants("Item")
                    .Where(el => scriptClasses.Contains(el.Attribute("class")?.Value ?? ""))
                    .ToList();

                var rootNode = new TreeViewItem
                {
                    Header = $"🗂️ {Path.GetFileName(filePath)}  ({items.Count} scripts)",
                    IsExpanded = true
                };
                rootNode.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");
                _portFileTree!.Items.Add(rootNode);

                foreach (var item in items)
                {
                    string cls = item.Attribute("class")?.Value ?? "ModuleScript";
                    string name = item.Descendants("string")
                        .FirstOrDefault(p => p.Attribute("name")?.Value == "Name")?.Value ?? "Unknown";
                    string source = item.Descendants("ProtectedString")
                        .Concat(item.Descendants("string"))
                        .FirstOrDefault(p => p.Attribute("name")?.Value == "Source")?.Value ?? "";

                    var pathParts = new List<string> { name };
                    var ancestor = item.Parent?.Parent;
                    while (ancestor?.Name.LocalName == "Item")
                    {
                        string aname = ancestor.Descendants("string")
                            .FirstOrDefault(p => p.Attribute("name")?.Value == "Name")?.Value ?? "?";
                        pathParts.Insert(0, aname);
                        ancestor = ancestor.Parent?.Parent;
                    }

                    string rel = string.Join("/", pathParts);
                    _portEntries.Add(new PortEntry(rel, filePath, cls, source));

                    var leaf = new TreeViewItem
                    {
                        Header = $"📄 {rel}  [{cls}]"
                    };
                    leaf.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");
                    rootNode.Items.Add(leaf);
                }
            }
            catch (Exception ex)
            {
                _portStatusLabel!.Text = $"❌ XML parse error: {ex.Message}";
            }
        }

        private void GeneratePortInstallerScript()
        {
            if (_portOutputBox == null || _portTargetServiceCombo == null || _portSubfolderCombo == null) return;

            if (_portEntries.Count == 0)
            {
                _portOutputBox.Text = "-- No scripts scanned yet. Click 'Scan Source & Preview Script Tree' first.";
                return;
            }

            string service = (_portTargetServiceCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? _portTargetServiceCombo.Text ?? "ReplicatedStorage";

            string subfolder = (_portSubfolderCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? _portSubfolderCombo.Text ?? "(root)";
            bool hasSubfolder = !string.IsNullOrEmpty(subfolder) && subfolder != "(root)";

            var sb = new StringBuilder();
            sb.AppendLine("-- =====================================================");
            sb.AppendLine($"-- HEAPLIT PORT INSTALLER  |  Source: {Path.GetFileName(_portSourcePathBox?.Text ?? "?")}");
            sb.AppendLine($"-- Target: {service}{(hasSubfolder ? "/" + subfolder : "")}");
            sb.AppendLine($"-- Scripts: {_portEntries.Count}  |  Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("-- Paste into Studio Command Bar and press Enter.");
            sb.AppendLine("-- =====================================================");
            sb.AppendLine();
            sb.AppendLine($"local svc = game:GetService(\"{service}\")");

            if (hasSubfolder)
            {
                sb.AppendLine($"local root = svc:FindFirstChild(\"{subfolder}\") or Instance.new(\"Folder\")");
                sb.AppendLine($"root.Name = \"{subfolder}\"");
                sb.AppendLine("root.Parent = svc");
            }
            else
            {
                sb.AppendLine("local root = svc");
            }

            sb.AppendLine();
            sb.AppendLine("-- Helper: get-or-create a folder chain");
            sb.AppendLine("local function getOrCreate(parent, name, cls)");
            sb.AppendLine("    local existing = parent:FindFirstChild(name)");
            sb.AppendLine("    if existing then return existing end");
            sb.AppendLine("    local inst = Instance.new(cls or \"Folder\")");
            sb.AppendLine("    inst.Name = name; inst.Parent = parent");
            sb.AppendLine("    return inst");
            sb.AppendLine("end");
            sb.AppendLine();

            int idx = 0;
            foreach (var entry in _portEntries)
            {
                idx++;
                string[] parts = entry.RelativePath.Replace('\\', '/').Split('/');
                string scriptName = Path.GetFileNameWithoutExtension(parts.Last());

                sb.AppendLine($"-- [{idx}/{_portEntries.Count}] {entry.RelativePath}");

                if (parts.Length > 1)
                {
                    sb.AppendLine("do");
                    sb.Append("    local cur = root");
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        sb.AppendLine();
                        sb.Append($"    cur = getOrCreate(cur, \"{parts[i]}\", \"Folder\")");
                    }
                    sb.AppendLine();

                    string escapedSource = entry.Source
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r\n", "\\n")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t");

                    const int maxSourceChars = 1200;
                    string sourceSnippet = escapedSource.Length > maxSourceChars
                        ? $"-- Source truncated ({entry.Source.Length} chars). Paste full file manually."
                        : escapedSource;

                    sb.AppendLine($"    local s{idx} = getOrCreate(cur, \"{scriptName}\", \"{entry.ScriptType}\")");
                    if (!sourceSnippet.StartsWith("-- Source truncated"))
                        sb.AppendLine($"    s{idx}.Source = \"{sourceSnippet}\"");
                    else
                        sb.AppendLine($"    -- {sourceSnippet}");
                    sb.AppendLine("end");
                }
                else
                {
                    string escapedSource = entry.Source
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r\n", "\\n")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t");

                    const int maxSourceChars = 1200;
                    string sourceSnippet = escapedSource.Length > maxSourceChars
                        ? $"-- Source truncated ({entry.Source.Length} chars). Paste full file manually."
                        : escapedSource;

                    sb.AppendLine($"local s{idx} = getOrCreate(root, \"{scriptName}\", \"{entry.ScriptType}\")");
                    if (!sourceSnippet.StartsWith("-- Source truncated"))
                        sb.AppendLine($"s{idx}.Source = \"{sourceSnippet}\"");
                    else
                        sb.AppendLine($"-- {sourceSnippet}");
                }

                sb.AppendLine();
            }

            sb.AppendLine($"print(\"[HEAPLIT] Ported {_portEntries.Count} scripts into \" .. \"{service}{(hasSubfolder ? "/" + subfolder : "")}\")");

            _portOutputBox.Text = sb.ToString();
        }

        private void LoadLuauGeneratorsView()
        {
            var panel = new StackPanel();

            var title = CreateSectionHeader("⚡ Roblox Luau Generator");
            panel.Children.Add(title);

            var formGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var colPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            colPanel.Children.Add(CreateLabel("Spritesheet Cols:"));
            _sprColsBox = CreateTextBox(); _sprColsBox.Text = "4";
            colPanel.Children.Add(_sprColsBox);
            Grid.SetRow(colPanel, 0); Grid.SetColumn(colPanel, 0);
            formGrid.Children.Add(colPanel);

            var rowPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            rowPanel.Children.Add(CreateLabel("Spritesheet Rows:"));
            _sprRowsBox = CreateTextBox(); _sprRowsBox.Text = "4";
            rowPanel.Children.Add(_sprRowsBox);
            Grid.SetRow(rowPanel, 0); Grid.SetColumn(rowPanel, 1);
            formGrid.Children.Add(rowPanel);

            var fpsPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            fpsPanel.Children.Add(CreateLabel("Playback FPS:"));
            _sprFpsBox = CreateTextBox(); _sprFpsBox.Text = "12";
            fpsPanel.Children.Add(_sprFpsBox);
            Grid.SetRow(fpsPanel, 0); Grid.SetColumn(fpsPanel, 2);
            formGrid.Children.Add(fpsPanel);

            var sizePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            sizePanel.Children.Add(CreateLabel("Resolution (px):"));
            _sprResBox = CreateTextBox(); _sprResBox.Text = "512";
            sizePanel.Children.Add(_sprResBox);
            Grid.SetRow(sizePanel, 0); Grid.SetColumn(sizePanel, 3);
            formGrid.Children.Add(sizePanel);

            panel.Children.Add(formGrid);

            var buttonGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var genSprBtn = CreateButton("🎬 Generate Spritesheet Animator", (s, e) => GenerateSpritesheetLuau());
            Grid.SetColumn(genSprBtn, 0);
            buttonGrid.Children.Add(genSprBtn);

            var genSufBtn = CreateButton("🔢 Generate Format Suffix Util", (s, e) => GenerateSuffixLuau());
            genSufBtn.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(genSufBtn, 1);
            buttonGrid.Children.Add(genSufBtn);

            panel.Children.Add(buttonGrid);

            _generatedLuauBox = new TextBox
            {
                Height = 220,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(6)
            };
            _generatedLuauBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _generatedLuauBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_generatedLuauBox);

            var copyBtn = CreateButton("📋 Copy Generated Luau Code", (s, e) => CopyToClipboard(_generatedLuauBox.Text));
            copyBtn.Margin = new Thickness(0, 10, 0, 0);
            panel.Children.Add(copyBtn);

            _contentGrid.Children.Add(panel);
        }

        private void GenerateSpritesheetLuau()
        {
            if (_sprColsBox == null || _sprRowsBox == null || _sprFpsBox == null || _sprResBox == null || _generatedLuauBox == null) return;

            int.TryParse(_sprColsBox.Text, out int cols);
            int.TryParse(_sprRowsBox.Text, out int rows);
            int.TryParse(_sprFpsBox.Text, out int fps);
            int.TryParse(_sprResBox.Text, out int res);

            if (cols <= 0) cols = 4;
            if (rows <= 0) rows = 4;
            if (fps <= 0) fps = 12;
            if (res <= 0) res = 512;

            string code = $"-- SpritesheetAnimator.lua\n" +
                          $"-- Parent this script to an ImageLabel representing the spritesheet texture\n" +
                          $"local COLS = {cols}          -- columns in spritesheet\n" +
                          $"local ROWS = {rows}          -- rows in spritesheet\n" +
                          $"local TOTAL_FRAMES = {cols * rows} -- total frames\n" +
                          $"local FPS = {fps}\n\n" +
                          $"local imageLabel = script.Parent\n" +
                          $"local index = 0\n\n" +
                          $"imageLabel.ImageRectSize = Vector2.new(\n" +
                          $"    imageLabel.AbsoluteSize.X * COLS,\n" +
                          $"    imageLabel.AbsoluteSize.Y * ROWS\n" +
                          $")\n\n" +
                          $"while true do\n" +
                          $"    local col = index % COLS\n" +
                          $"    local row = math.floor(index / COLS)\n" +
                          $"    imageLabel.ImageRectOffset = Vector2.new(col * {res}, row * {res})\n" +
                          $"    index = (index + 1) % TOTAL_FRAMES\n" +
                          $"    task.wait(1 / FPS)\n" +
                          $"end\n";

            _generatedLuauBox.Text = code;
        }

        private void GenerateSuffixLuau()
        {
            if (_generatedLuauBox == null) return;

            string code = "-- SuffixFormatNumber.lua\n" +
                          "-- Reference utilizing Ring0.Suffixes.FormatNumber canonical wrapper\n" +
                          "local Rings = game:GetService(\"ReplicatedStorage\"):WaitForChild(\"RingWorld\"):WaitForChild(\"Rings\")\n" +
                          "local FormatNumber = require(Rings.Ring0.Suffixes.FormatNumber)\n\n" +
                          "-- Usage Examples:\n" +
                          "-- local score = 1500000\n" +
                          "-- print(FormatNumber.abbreviate(score)) -> \"1.5 Mil\"\n" +
                          "-- print(FormatNumber.abbreviate(99000000000)) -> \"99 Bil\"\n\n" +
                          "local SuffixUtil = {}\n\n" +
                          "function SuffixUtil.formatDisplayValue(val: number): string\n" +
                          "    return FormatNumber.abbreviate(val)\n" +
                          "end\n\n" +
                          "return SuffixUtil\n";

            _generatedLuauBox.Text = code;
        }
        #endregion

        #region View 8: Blender Animation script
        private void LoadBlenderGeneratorView()
        {
            var panel = new StackPanel();

            var title = CreateSectionHeader("📐 Blender Multi-Angle Texture Baker");
            panel.Children.Add(title);

            var hint = CreateHintText("Generates Blender bpy python code to render multi-angle rotation frames of an object. Bake results to importable PNG sequences.");
            panel.Children.Add(hint);

            var formGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var namePanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            namePanel.Children.Add(CreateLabel("Blender Model Name:"));
            _blModelBox = CreateTextBox(); _blModelBox.Text = "MyModel";
            namePanel.Children.Add(_blModelBox);
            Grid.SetRow(namePanel, 0); Grid.SetColumn(namePanel, 0);
            formGrid.Children.Add(namePanel);

            var framesPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            framesPanel.Children.Add(CreateLabel("Bake Frames:"));
            _blFramesBox = CreateTextBox(); _blFramesBox.Text = "16";
            framesPanel.Children.Add(_blFramesBox);
            Grid.SetRow(framesPanel, 0); Grid.SetColumn(framesPanel, 1);
            formGrid.Children.Add(framesPanel);

            var rxPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            rxPanel.Children.Add(CreateLabel("Resolution X:"));
            _blResXBox = CreateTextBox(); _blResXBox.Text = "512";
            rxPanel.Children.Add(_blResXBox);
            Grid.SetRow(rxPanel, 0); Grid.SetColumn(rxPanel, 2);
            formGrid.Children.Add(rxPanel);

            var ryPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            ryPanel.Children.Add(CreateLabel("Resolution Y:"));
            _blResYBox = CreateTextBox(); _blResYBox.Text = "512";
            ryPanel.Children.Add(_blResYBox);
            Grid.SetRow(ryPanel, 0); Grid.SetColumn(ryPanel, 3);
            formGrid.Children.Add(ryPanel);

            var transPanel = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
            _blTransBgBox = new CheckBox
            {
                Content = "Alpha Alpha",
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            };
            _blTransBgBox.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
            transPanel.Children.Add(_blTransBgBox);
            Grid.SetRow(transPanel, 0); Grid.SetColumn(transPanel, 4);
            formGrid.Children.Add(transPanel);

            panel.Children.Add(formGrid);

            var genBtn = CreateButton("🎬 Generate Python Baking Script", (s, e) => GenerateBlenderPython());
            genBtn.Margin = new Thickness(0, 0, 0, 10);
            panel.Children.Add(genBtn);

            _generatedPyBox = new TextBox
            {
                Height = 220,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(6)
            };
            _generatedPyBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _generatedPyBox.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            panel.Children.Add(_generatedPyBox);

            var copyBtn = CreateButton("📋 Copy Generated Python Script", (s, e) => CopyToClipboard(_generatedPyBox.Text));
            copyBtn.Margin = new Thickness(0, 10, 0, 0);
            panel.Children.Add(copyBtn);

            _contentGrid.Children.Add(panel);
        }

        private void GenerateBlenderPython()
        {
            if (_blModelBox == null || _blFramesBox == null || _blResXBox == null || _blResYBox == null || _blTransBgBox == null || _generatedPyBox == null) return;

            string model = _blModelBox.Text.Trim();
            int.TryParse(_blFramesBox.Text, out int frames);
            int.TryParse(_blResXBox.Text, out int rx);
            int.TryParse(_blResYBox.Text, out int ry);
            bool trans = _blTransBgBox.IsChecked == true;

            if (string.IsNullOrEmpty(model)) model = "MyModel";
            if (frames <= 0) frames = 16;
            if (rx <= 0) rx = 512;
            if (ry <= 0) ry = 512;

            string pyCode = $"import bpy, math, os\n\n" +
                            $"# Configurations\n" +
                            $"MODEL_NAME = \"{model}\"\n" +
                            $"FRAME_COUNT = {frames}\n" +
                            $"OUTPUT_DIR = os.path.join(os.path.expanduser('~'), 'Downloads', 'animated_textures')\n" +
                            $"RESOLUTION_X = {rx}\n" +
                            $"RESOLUTION_Y = {ry}\n" +
                            $"TRANSPARENT_BG = { (trans ? "True" : "False") }\n\n" +
                            "os.makedirs(OUTPUT_DIR, exist_ok=True)\n\n" +
                            "scene = bpy.context.scene\n" +
                            "scene.render.resolution_x = RESOLUTION_X\n" +
                            "scene.render.resolution_y = RESOLUTION_Y\n" +
                            "scene.render.image_settings.file_format = 'PNG'\n" +
                            "scene.render.image_settings.color_mode = 'RGBA' if TRANSPARENT_BG else 'RGB'\n" +
                            "scene.render.film_transparent = TRANSPARENT_BG\n" +
                            "scene.render.engine = 'BLENDER_EEVEE_NEXT'\n\n" +
                            "# Camera Setup\n" +
                            "cam = bpy.data.objects.get(\"BakeCamera\")\n" +
                            "if not cam:\n" +
                            "    cam_data = bpy.data.cameras.new(\"BakeCamera\")\n" +
                            "    cam = bpy.data.objects.new(\"BakeCamera\", cam_data)\n" +
                            "    bpy.context.collection.objects.link(cam)\n\n" +
                            "obj = bpy.data.objects.get(MODEL_NAME)\n" +
                            "if obj:\n" +
                            "    cam.location = (0, -3, 1.2)\n" +
                            "    cam.rotation_euler = (math.radians(80), 0, 0)\n" +
                            "    scene.camera = cam\n" +
                            "    track = cam.constraints.get(\"Track To\") or cam.constraints.new('TRACK_TO')\n" +
                            "    track.target = obj\n" +
                            "    track.track_axis = 'TRACK_NEGATIVE_Z'\n" +
                            "    track.up_axis = 'UP_Y'\n" +
                            "    \n" +
                            "    angle_step = 360.0 / FRAME_COUNT\n" +
                            "    for i in range(FRAME_COUNT):\n" +
                            "        obj.rotation_euler = (0, 0, math.radians(i * angle_step))\n" +
                            "        scene.render.filepath = os.path.join(OUTPUT_DIR, f\"frame_{i:04d}.png\")\n" +
                            "        bpy.ops.render.render(write_still=True)\n" +
                            "    print(f\"Success: Rendered {frames} frames to {OUTPUT_DIR}\")\n" +
                            "else:\n" +
                            "    print(f\"Error: Model '{model}' not found in scene.\")\n";

            _generatedPyBox.Text = pyCode;
        }
        #endregion

        #region Visual Helpers
        private TextBlock CreateSectionHeader(string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            return tb;
        }

        private TextBlock CreateHintText(string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            return tb;
        }

        private Button CreateButton(string content, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = content,
                Padding = new Thickness(12, 4, 12, 4),
                Cursor = Cursors.Hand,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI")
            };
            btn.SetResourceReference(Button.BackgroundProperty, "HoverBackgroundBrush");
            btn.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
            btn.Click += onClick;
            return btn;
        }

        private TextBlock CreateSubHeader(string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 4)
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            return tb;
        }

        private ComboBox CreateComboBox(string[] items)
        {
            var cb = new ComboBox
            {
                Height = 24,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8)
            };
            cb.SetResourceReference(ComboBox.BackgroundProperty, "HoverBackgroundBrush");
            cb.SetResourceReference(ComboBox.ForegroundProperty, "TextPrimaryBrush");

            foreach (var item in items)
            {
                cb.Items.Add(new ComboBoxItem
                {
                    Content = item,
                    FontSize = 11
                });
            }
            return cb;
        }

        private void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                Clipboard.SetText(text);
                TextOverlay.Show("📋 Copied to Clipboard!", 2000);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Copy failed: {ex.Message}", 2500);
            }
        }
        #endregion
    }
}