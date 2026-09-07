// Developer: heaplyn
// Date: 2026-09-06
// Summary: Interactive Glassmorphic Font Studio Overlay for Heaplit.
//          Allows users to drag & drop or browse any PNG font sheet, alphabet grid, or icon graphic,
//          automatically vectorizes and generates TrueType (.ttf) font files with live preview,
//          and offers 1-click application as the active Heaplit HUD font.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace HeaplitLauncher
{
    public class FontStudioOverlay : BaseOverlay
    {
        private static FontStudioOverlay? _instance;

        private TextBox _inputBox = null!;
        private TextBox _fontNameBox = null!;
        private ComboBox _modeCombo = null!;
        private Slider _thresholdSlider = null!;
        private TextBlock _thresholdValText = null!;
        private CheckBox _invertCheck = null!;
        private CheckBox _autoApplyCheck = null!;
        private Button _convertBtn = null!;
        private TextBlock _statusText = null!;
        private TextBlock _previewSampleText = null!;
        private Border _previewContainer = null!;
        private Image _sourceImagePreview = null!;
        private string _lastGeneratedTtf = "";

        public static void ShowOverlay(string initialInput = "")
        {
            if (_instance == null || !_instance.IsLoaded || !_instance.IsVisible)
            {
                _instance = new FontStudioOverlay(initialInput);
                _instance.Show();
            }
            else
            {
                if (!string.IsNullOrEmpty(initialInput))
                {
                    _instance._inputBox.Text = initialInput;
                    _instance.UpdateImagePreview(initialInput);
                }
                _instance.Activate();
                _instance.BringToFront();
                _instance.Focus();
            }
        }

        public FontStudioOverlay(string initialInput = "")
            : base("🔤 PNG ➔ TTF FONT VECTORIZER & STUDIO", width: 680, height: 620)
        {
            this.Closed += (s, e) => { _instance = null; };

            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var root = new StackPanel { Margin = new Thickness(10) };
            scroll.Content = root;

            root.Children.Add(CreateHeader("✨ Auto Convert PNG Font Sheets to TrueType (.TTF)"));

            var desc = new TextBlock
            {
                Text = "Drag & drop or select any PNG font sheet (black on white, white on black, or transparent). " +
                       "Heaplit automatically traces vector contours, slices character grids, compiles a valid Unicode .TTF font, and lets you use it anywhere.",
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            desc.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            root.Children.Add(desc);

            // Input File Row
            root.Children.Add(CreateLabel("Source Image or Character Folder:"));
            var fileGrid = new Grid { Margin = new Thickness(0, 2, 0, 8) };
            fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _inputBox = new TextBox
            {
                Text = initialInput,
                Padding = new Thickness(6),
                FontSize = 12,
                AllowDrop = true
            };
            _inputBox.PreviewDragOver += (s, e) => { e.Handled = true; e.Effects = DragDropEffects.Copy; };
            _inputBox.Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        _inputBox.Text = files[0];
                        AutoDetectFontName(files[0]);
                        UpdateImagePreview(files[0]);
                    }
                }
            };
            _inputBox.TextChanged += (s, e) => UpdateImagePreview(_inputBox.Text.Trim().Trim('"', '\''));
            Grid.SetColumn(_inputBox, 0);
            fileGrid.Children.Add(_inputBox);

            var browseFileBtn = CreateButton("📂 Browse Image...");
            browseFileBtn.Margin = new Thickness(6, 0, 0, 0);
            browseFileBtn.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select PNG Font Sheet or Image",
                    Filter = "PNG & Image Files|*.png;*.webp;*.jpg;*.jpeg;*.bmp|All Files|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    _inputBox.Text = dlg.FileName;
                    AutoDetectFontName(dlg.FileName);
                    UpdateImagePreview(dlg.FileName);
                }
            };
            Grid.SetColumn(browseFileBtn, 1);
            fileGrid.Children.Add(browseFileBtn);

            var browseFolderBtn = CreateButton("📁 Folder...");
            browseFolderBtn.Margin = new Thickness(6, 0, 0, 0);
            browseFolderBtn.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select any file in glyphs folder to pick directory",
                    CheckFileExists = false,
                    FileName = "Select Folder"
                };
                if (dlg.ShowDialog() == true)
                {
                    string dir = Path.GetDirectoryName(dlg.FileName) ?? "";
                    if (!string.IsNullOrEmpty(dir))
                    {
                        _inputBox.Text = dir;
                        AutoDetectFontName(dir);
                    }
                }
            };
            Grid.SetColumn(browseFolderBtn, 2);
            fileGrid.Children.Add(browseFolderBtn);
            root.Children.Add(fileGrid);

            // Font Settings Grid
            var settingsGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 2, 0, 8) };

            var fontNameStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            fontNameStack.Children.Add(CreateLabel("Font Family Name:"));
            _fontNameBox = new TextBox { Text = "HexAlphabetFont", Padding = new Thickness(6), FontSize = 12 };
            fontNameStack.Children.Add(_fontNameBox);
            settingsGrid.Children.Add(fontNameStack);

            var modeStack = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
            modeStack.Children.Add(CreateLabel("Recognition Mode:"));
            _modeCombo = new ComboBox { Padding = new Thickness(6), FontSize = 12 };
            _modeCombo.Items.Add("Auto-Detect (Recommended)");
            _modeCombo.Items.Add("Spritesheet Grid / Alphabet Sheet");
            _modeCombo.Items.Add("Single Glyph / Icon");
            _modeCombo.Items.Add("Folder of Character Images");
            _modeCombo.SelectedIndex = 0;
            modeStack.Children.Add(_modeCombo);
            settingsGrid.Children.Add(modeStack);

            root.Children.Add(settingsGrid);

            // Threshold and Invert Controls
            var tracePanel = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };
            var threshHeader = new Grid();
            threshHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            threshHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var threshLbl = CreateLabel("Vector Binarization Threshold:");
            Grid.SetColumn(threshLbl, 0);
            threshHeader.Children.Add(threshLbl);
            _thresholdValText = new TextBlock { Text = "128", FontSize = 11, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            _thresholdValText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            Grid.SetColumn(_thresholdValText, 1);
            threshHeader.Children.Add(_thresholdValText);
            tracePanel.Children.Add(threshHeader);

            _thresholdSlider = new Slider { Minimum = 10, Maximum = 245, Value = 128, SmallChange = 1, LargeChange = 10, Margin = new Thickness(0, 2, 0, 4) };
            _thresholdSlider.ValueChanged += (s, e) => { if (_thresholdValText != null) _thresholdValText.Text = ((int)_thresholdSlider.Value).ToString(); };
            tracePanel.Children.Add(_thresholdSlider);

            var checkRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            _invertCheck = new CheckBox { Content = "Invert Foreground / Background Mask", FontSize = 11, Margin = new Thickness(0, 0, 16, 0) };
            _invertCheck.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
            checkRow.Children.Add(_invertCheck);

            _autoApplyCheck = new CheckBox { Content = "Immediately Apply as Heaplit HUD Font", IsChecked = true, FontSize = 11 };
            _autoApplyCheck.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
            checkRow.Children.Add(_autoApplyCheck);
            tracePanel.Children.Add(checkRow);

            root.Children.Add(tracePanel);

            // Source Image Thumbnail / Preview
            _sourceImagePreview = new Image { MaxHeight = 120, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 8) };
            root.Children.Add(_sourceImagePreview);

            // Action Buttons
            var actionRow = new UniformGrid { Columns = 2, Margin = new Thickness(0, 6, 0, 8) };

            _convertBtn = CreateButton("⚡ Generate TrueType (.TTF) Font");
            _convertBtn.Height = 36;
            _convertBtn.FontWeight = FontWeights.Bold;
            _convertBtn.Click += async (s, e) => await ExecuteConvertAsync();
            actionRow.Children.Add(_convertBtn);

            var applyBtn = CreateButton("🎨 Apply to Heaplit HUD");
            applyBtn.Height = 36;
            applyBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_lastGeneratedTtf) && File.Exists(_lastGeneratedTtf))
                {
                    ThemeManager.ApplyFont(_lastGeneratedTtf);
                    TextOverlay.Show($"✨ Applied Font: {Path.GetFileName(_lastGeneratedTtf)}", 2500);
                }
                else
                {
                    TextOverlay.Show("⚠️ Convert a font first!", 2000);
                }
            };
            actionRow.Children.Add(applyBtn);

            root.Children.Add(actionRow);

            // Live Font Rendering Preview Card
            root.Children.Add(CreateHeader("👁️ Live Generated Font Preview"));
            _previewContainer = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4, 0, 8),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
            };
            _previewContainer.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush");

            var previewStack = new StackPanel();
            _previewSampleText = new TextBlock
            {
                Text = "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG\n1234567890 !?\"'-+/*&@#",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            };
            _previewSampleText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            previewStack.Children.Add(_previewSampleText);
            _previewContainer.Child = previewStack;
            root.Children.Add(_previewContainer);

            _statusText = new TextBlock
            {
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };
            _statusText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            root.Children.Add(_statusText);

            if (!string.IsNullOrEmpty(initialInput))
            {
                AutoDetectFontName(initialInput);
                UpdateImagePreview(initialInput);
            }

            this.UserContent = scroll;
        }

        private void AutoDetectFontName(string path)
        {
            string clean = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(clean))
            {
                _fontNameBox.Text = clean.Replace(" ", "") + "Font";
            }
        }

        private void UpdateImagePreview(string path)
        {
            try
            {
                if (File.Exists(path) && (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                          path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                          path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                                          path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelHeight = 160;
                    bmp.EndInit();
                    _sourceImagePreview.Source = bmp;
                    _sourceImagePreview.Visibility = Visibility.Visible;
                }
                else
                {
                    _sourceImagePreview.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                _sourceImagePreview.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ExecuteConvertAsync()
        {
            string inputPath = _inputBox.Text.Trim().Trim('"', '\'');
            if (string.IsNullOrEmpty(inputPath) || (!File.Exists(inputPath) && !Directory.Exists(inputPath)))
            {
                TextOverlay.Show("⚠️ Select a valid PNG image or folder!", 2500);
                return;
            }

            string fontName = _fontNameBox.Text.Trim();
            if (string.IsNullOrEmpty(fontName)) fontName = "HeaplitCustomFont";

            string outDir = Directory.Exists(inputPath) ? inputPath : (Path.GetDirectoryName(inputPath) ?? "");
            string outputTtf = Path.Combine(outDir, $"{fontName}.ttf");

            int threshold = (int)_thresholdSlider.Value;
            bool invert = _invertCheck.IsChecked == true;

            _convertBtn.IsEnabled = false;
            _statusText.Text = $"⏳ Vectorizing contours and compiling '{fontName}.ttf'...";
            TextOverlay.Show($"⏳ Compiling TTF font '{fontName}'...", 2500);

            var (success, resultPath, error) = await PngToTtfEngine.ConvertAsync(
                inputPath,
                outputTtf,
                fontName,
                threshold,
                invert
            );

            _convertBtn.IsEnabled = true;

            if (success && File.Exists(resultPath))
            {
                _lastGeneratedTtf = resultPath;
                _statusText.Text = $"✅ Font Built Successfully!\nSaved to: {resultPath}";
                TextOverlay.Show($"✅ Font Created: {Path.GetFileName(resultPath)}", 3000);

                // Update live preview with new font
                try
                {
                    string dir = Path.GetDirectoryName(resultPath) ?? "";
                    string file = Path.GetFileName(resultPath);
                    string folderUriStr = "file:///" + dir.Replace("\\", "/") + "/";
                    var folderUri = new Uri(folderUriStr, UriKind.Absolute);
                    var customFamily = new FontFamily(folderUri, "./" + file + "#" + fontName);
                    _previewSampleText.FontFamily = customFamily;
                }
                catch { }

                // Auto-apply if checked
                if (_autoApplyCheck.IsChecked == true)
                {
                    ThemeManager.ApplyFont(resultPath);
                }

                // Open folder
                try
                {
                    Process.Start("explorer.exe", $"/select,\"{resultPath}\"");
                }
                catch { }
            }
            else
            {
                _statusText.Text = $"❌ Font Generation Failed: {error}";
                TextOverlay.Show("❌ Font Generation Failed!", 3000);
            }
        }

        private static TextBlock CreateHeader(string title)
        {
            var header = new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 4)
            };
            header.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            return header;
        }

        private static TextBlock CreateLabel(string text)
        {
            var lbl = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 2)
            };
            lbl.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            return lbl;
        }

        private static Button CreateButton(string content)
        {
            var btn = new Button
            {
                Content = content,
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 11,
                Cursor = Cursors.Hand
            };
            return btn;
        }
    }
}
