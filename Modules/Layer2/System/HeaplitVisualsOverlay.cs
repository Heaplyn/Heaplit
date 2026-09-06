// Developer: heaplyn
// Date: 2026-09-05
// Summary: Heaplit Visuals [MASTER] - Unified HUD Customization Suite.
//          Combines Aesthetics, Typography, Shapes, Motion, FX, and System settings
//          with bulletproof null-safety and dynamic category profiling.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace HeaplitLauncher
{
    public class HeaplitVisualsOverlay : BaseOverlay
    {
        private static HeaplitVisualsOverlay? _instance;

        // Aesthetics Tab Controls
        private string _bgHex = "#1A1A1A", _textHex = "#FFFFFF", _accentHex = "#00FFFF";
        private string _gradStartHex = "#FF007F", _gradEndHex = "#7F00FF";
        private string _gifPath = string.Empty;
        private ComboBox _bgModeCombo = null!;
        private Slider _gifOpacitySlider = null!;
        private Slider _gifFpsSlider = null!;
        private CheckBox _textGradientCheck = null!;

        // Typography Tab Controls
        private ComboBox _profileSelector = null!;
        private string _activeProfile = "Labels";
        private StackPanel _strokeListPanel = null!;
        private CheckBox _strokeCheck = null!;
        private CheckBox _italicCheck = null!;
        private CheckBox _shadowCheck = null!;
        private CheckBox _profileShadowCheck = null!;
        private Slider _shadowOffsetX = null!;
        private Slider _shadowOffsetY = null!;
        private Slider _shadowBlurSlider = null!;
        private string _shadowHex = "#FF000000";
        private Slider _glowAmountSlider = null!;
        private string _glowHex = "#FF00FFFF";
        private Slider _textSizeSlider = null!;

        // Motion Tab Controls
        private CheckBox _enableAnimCheck = null!;
        private ComboBox _speedCombo = null!;
        private Slider _winWobbleSlider = null!;
        private Slider _winWobbleMaxSkewSlider = null!;
        private CheckBox _winWobbleCheck = null!;
        private Slider _textWobbleSlider = null!;
        private Slider _textWobbleSpeedSlider = null!;

        // Shapes & Frames Tab Controls
        private Slider _cornerRadiusSlider = null!;
        private Slider _borderThicknessSlider = null!;
        private ComboBox _shapeModeCombo = null!;
        private CheckBox _winGlowCheck = null!;
        private Slider _winGlowRadiusSlider = null!;
        private CheckBox _rainbowBorderCheck = null!;
        private Slider _rainbowSpeedSlider = null!;

        // FX Tab Controls
        private CheckBox _scanlineCheck = null!;
        private Slider _scanlineOpacitySlider = null!;
        private Slider _scanlineFreqSlider = null!;
        private CheckBox _vignetteCheck = null!;
        private Slider _vignetteIntensitySlider = null!;
        private CheckBox _grainCheck = null!;
        private Slider _grainOpacitySlider = null!;
        private CheckBox _glowPulseCheck = null!;
        private Slider _glowPulseSpeedSlider = null!;
        private CheckBox _chromaCheck = null!;
        private Slider _chromaAmountSlider = null!;
        private CheckBox _clickDarkSpotCheck = null!;

        // System Tab Controls
        private Slider _guiScaleSlider = null!;
        private CheckBox _autoScaleCheck = null!;
        private Slider _winOpacitySlider = null!;
        private TextBlock _winOpacityValText = null!;
        private Slider _textOpacitySlider = null!;
        private TextBlock _textOpacityValText = null!;

        private TextBlock _statusText = null!;
        private TabControl _tabControl = null!;

        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_instance == null || !_instance.IsLoaded) _instance = new HeaplitVisualsOverlay();
                    _instance.Show();
                    _instance.BringToFront();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening Heaplit Visuals: {ex.Message}\n{ex.StackTrace}", "Heaplit Visuals Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private HeaplitVisualsOverlay() : base("🎨 HEAPLIT VISUALS [MASTER SUITE]", 850, 780)
        {
            _instance = this;
            this.Closed += (s, e) => _instance = null;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;

            var set = SettingsManager.Current ?? new SystemSettings();
            _bgHex = set.THEME_BG_COLOR ?? "#1A1A1A";
            _textHex = set.THEME_TEXT_COLOR ?? "#FFFFFF";
            _accentHex = set.THEME_ACCENT_COLOR ?? "#00FFFF";
            _gifPath = set.BACKGROUND_GIF_PATH ?? string.Empty;
            _gradStartHex = set.TEXT_GRADIENT_START ?? "#FF007F";
            _gradEndHex = set.TEXT_GRADIENT_END ?? "#7F00FF";
            _shadowHex = set.TEXT_SHADOW_COLOR ?? "#FF000000";
            _glowHex = set.TEXT_GLOW_COLOR ?? "#FF00FFFF";

            if (set.TEXT_PROFILES == null)
            {
                set.TEXT_PROFILES = new Dictionary<string, TextVisualProfile>(StringComparer.OrdinalIgnoreCase);
            }

            var allDefaultCats = new[] { "Titles", "Headers", "Labels", "Search", "Cards", "Values", "Subtext", "Code", "Accents" };
            foreach (var cat in allDefaultCats)
            {
                if (!set.TEXT_PROFILES.ContainsKey(cat))
                    set.TEXT_PROFILES[cat] = new TextVisualProfile { Name = cat };
            }

            if (!set.TEXT_PROFILES.ContainsKey(_activeProfile))
            {
                _activeProfile = set.TEXT_PROFILES.Keys.FirstOrDefault() ?? "Labels";
            }

            var mainGrid = new Grid { Margin = new Thickness(15) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _tabControl = new TabControl();
            StyleTabControl(_tabControl);
            _tabControl.Items.Add(new TabItem { Header = "🎨 Aesthetics", Content = BuildAestheticsTab() });
            _tabControl.Items.Add(new TabItem { Header = "🔡 Typography", Content = BuildTypographyTab() });
            _tabControl.Items.Add(new TabItem { Header = "📐 Shapes", Content = BuildShapesTab() });
            _tabControl.Items.Add(new TabItem { Header = "✨ Motion", Content = BuildMotionTab() });
            _tabControl.Items.Add(new TabItem { Header = "🧬 FX", Content = BuildFxTab() });
            _tabControl.Items.Add(new TabItem { Header = "⚙️ System", Content = BuildSystemTab() });

            Grid.SetRow(_tabControl, 0);
            mainGrid.Children.Add(_tabControl);

            var footerGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusText = new TextBlock { Text = "Heaplit Visuals Master Suite ready.", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(_statusText, 0); footerGrid.Children.Add(_statusText);

            var saveBtn = CreateStyledButton("🚀 SYNCHRONIZE PROTOCOLS", (s, e) => SaveAndApply(), isPrimary: true, fontSize: 14);
            saveBtn.Height = 45; Grid.SetColumn(saveBtn, 1); footerGrid.Children.Add(saveBtn);

            Grid.SetRow(footerGrid, 1);
            mainGrid.Children.Add(footerGrid);

            this.UserContent = mainGrid;
        }

        private UIElement BuildAestheticsTab()
        {
            var stack = new StackPanel();
            var set = SettingsManager.Current ?? new SystemSettings();

            stack.Children.Add(CreateHeader("System Theme & Color Matrix"));
            var customThemeCheck = new CheckBox { Content = "Override Built-in Themes with My Colors", IsChecked = set.THEME == "custom", Foreground = Brushes.Cyan, Margin = new Thickness(0,0,0,10) };
            customThemeCheck.Checked += (s, e) => { var sCur = SettingsManager.Current; if (sCur != null) sCur.THEME = "custom"; };
            customThemeCheck.Unchecked += (s, e) => { var sCur = SettingsManager.Current; if (sCur != null) sCur.THEME = "purple"; };
            stack.Children.Add(customThemeCheck);

            AddColorEditor(stack, "Primary Backdrop (HEX):", _bgHex, h => _bgHex = h);
            AddColorEditor(stack, "Global Text Body (HEX):", _textHex, h => _textHex = h);
            AddColorEditor(stack, "HUD Accent & Borders (HEX):", _accentHex, h => _accentHex = h);

            stack.Children.Add(CreateHeader("Universal Canvas Logic"));
            stack.Children.Add(CreateLabel("Active Background Engine:"));
            _bgModeCombo = new ComboBox { Margin = new Thickness(0,0,0,10), Height = 30 };
            _bgModeCombo.Items.Add("Gradient"); _bgModeCombo.Items.Add("Solid"); _bgModeCombo.Items.Add("Radial"); _bgModeCombo.Items.Add("RGB"); _bgModeCombo.Items.Add("Starfield");
            _bgModeCombo.SelectedItem = set.BACKGROUND_MODE ?? "Gradient";
            if (_bgModeCombo.SelectedIndex < 0) _bgModeCombo.SelectedIndex = 0;
            stack.Children.Add(_bgModeCombo);

            stack.Children.Add(CreateHeader("High-Fidelity Text Gradients"));
            _textGradientCheck = new CheckBox { Content = "Enable Universal Text Gradients", IsChecked = set.USE_TEXT_GRADIENT, Foreground = Brushes.White, Margin = new Thickness(0,10,0,5) };
            stack.Children.Add(_textGradientCheck);
            AddColorEditor(stack, "Gradient Start Color (HEX):", _gradStartHex, h => _gradStartHex = h);
            AddColorEditor(stack, "Gradient End Color (HEX):", _gradEndHex, h => _gradEndHex = h);

            stack.Children.Add(CreateHeader("Cinematic Background Media"));
            var gifGrid = new Grid { Margin = new Thickness(0,5,0,10) };
            gifGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gifGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var gifBox = CreateTextBox(); gifBox.Text = _gifPath; gifBox.TextChanged += (s, e) => _gifPath = gifBox.Text;
            var pickBtn = CreateStyledButton("📁", (s, e) => { var dlg = new OpenFileDialog { Filter = "GIF Files|*.gif|All Files|*.*" }; if (dlg.ShowDialog() == true) gifBox.Text = dlg.FileName; });
            pickBtn.Width = 40; pickBtn.Height = 32; Grid.SetColumn(gifBox, 0); gifGrid.Children.Add(gifBox); Grid.SetColumn(pickBtn, 1); gifGrid.Children.Add(pickBtn);
            stack.Children.Add(new TextBlock { Text = "Background GIF Path:", FontSize = 11, Foreground = Brushes.Gray });
            stack.Children.Add(gifGrid);

            stack.Children.Add(CreateLabel("GIF Layer Transparency:"));
            _gifOpacitySlider = CreateSlider(0, 1.0, set.BACKGROUND_GIF_OPACITY, 0.05); stack.Children.Add(_gifOpacitySlider);
            stack.Children.Add(CreateLabel("Target Refresh Rate (FPS):"));
            _gifFpsSlider = CreateSlider(1, 60, set.BACKGROUND_GIF_FPS, 1); stack.Children.Add(_gifFpsSlider);

            return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildTypographyTab()
        {
            var stack = new StackPanel();
            var set = SettingsManager.Current ?? new SystemSettings();

            stack.Children.Add(CreateHeader("Linguistic Profiles (Text Categories)"));
            var profStack = new StackPanel { Margin = new Thickness(0,0,0,15) };

            // Pre-initialize child containers to prevent null reference on event cascades
            _strokeListPanel = new StackPanel();
            _profileShadowCheck = new CheckBox
            {
                Content = $"Enable Shadow for {_activeProfile} Category",
                IsChecked = set.TEXT_PROFILES != null && set.TEXT_PROFILES.TryGetValue(_activeProfile, out var p) && p.EnableShadow,
                Foreground = Brushes.Pink,
                Margin = new Thickness(0,0,0,10),
                FontWeight = FontWeights.Bold
            };
            _profileShadowCheck.Checked += (s, e) => {
                var sCur = SettingsManager.Current;
                if (sCur?.TEXT_PROFILES != null && sCur.TEXT_PROFILES.TryGetValue(_activeProfile, out var prof))
                    prof.EnableShadow = true;
            };
            _profileShadowCheck.Unchecked += (s, e) => {
                var sCur = SettingsManager.Current;
                if (sCur?.TEXT_PROFILES != null && sCur.TEXT_PROFILES.TryGetValue(_activeProfile, out var prof))
                    prof.EnableShadow = false;
            };

            _profileSelector = new ComboBox { Margin = new Thickness(0,0,0,10), Height = 30 };
            if (set.TEXT_PROFILES != null)
            {
                foreach (var catKey in set.TEXT_PROFILES.Keys) _profileSelector.Items.Add(catKey);
            }
            _profileSelector.SelectedItem = _activeProfile;
            if (_profileSelector.SelectedIndex < 0 && _profileSelector.Items.Count > 0) _profileSelector.SelectedIndex = 0;

            _profileSelector.SelectionChanged += (s, e) => {
                if (_profileSelector.SelectedItem != null)
                {
                    _activeProfile = _profileSelector.SelectedItem.ToString()!;
                    RefreshStrokeList();
                    RefreshProfileOptions();
                }
            };
            profStack.Children.Add(CreateLabel("Select Text Category to Calibrate:"));
            profStack.Children.Add(_profileSelector);
            profStack.Children.Add(_profileShadowCheck);
            stack.Children.Add(profStack);

            stack.Children.Add(CreateHeader("High-Fidelity Text Outlining (N-Strokes)"));
            _strokeCheck = new CheckBox { Content = "Enable Multi-Layer Geometry Outlining", IsChecked = set.ENABLE_TEXT_STROKE, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_strokeCheck);

            _italicCheck = new CheckBox { Content = "Enable Global Italic Transformation", IsChecked = set.TEXT_IS_ITALIC, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_italicCheck);

            stack.Children.Add(CreateLabel("Global System HUD Font size:"));
            _textSizeSlider = CreateSlider(8, 32, set.GLOBAL_TEXT_SIZE, 1); stack.Children.Add(_textSizeSlider);

            stack.Children.Add(CreateLabel("Stroke Line Join Type:"));
            var joinCombo = new ComboBox { Margin = new Thickness(0,0,0,10) };
            joinCombo.Items.Add("Round"); joinCombo.Items.Add("Bevel"); joinCombo.Items.Add("Miter");
            joinCombo.SelectedItem = set.TEXT_STROKE_LINE_JOIN ?? "Round";
            if (joinCombo.SelectedIndex < 0) joinCombo.SelectedIndex = 0;
            joinCombo.SelectionChanged += (s, e) => {
                var sCur = SettingsManager.Current;
                if (sCur != null && joinCombo.SelectedItem != null)
                    sCur.TEXT_STROKE_LINE_JOIN = joinCombo.SelectedItem.ToString() ?? "Round";
            };
            stack.Children.Add(joinCombo);

            RefreshStrokeList();
            stack.Children.Add(_strokeListPanel);

            var addStrokeBtn = CreateStyledButton("+ ADD STROKE LAYER", (s, e) => {
                var sCur = SettingsManager.Current;
                if (sCur?.TEXT_PROFILES != null && sCur.TEXT_PROFILES.TryGetValue(_activeProfile, out var prof))
                {
                    if (prof.Strokes == null) prof.Strokes = new List<TextStroke>();
                    prof.Strokes.Add(new TextStroke { Thickness = 1.0, Color = "#FF000000" });
                    RefreshStrokeList();
                }
            }, fontSize: 10);
            addStrokeBtn.HorizontalAlignment = HorizontalAlignment.Left;
            stack.Children.Add(addStrokeBtn);

            stack.Children.Add(CreateHeader("Drop Shadow Protocols"));
            _shadowCheck = new CheckBox { Content = "Enable Global Text Shadows", IsChecked = set.ENABLE_TEXT_SHADOW, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_shadowCheck);
            stack.Children.Add(CreateLabel("Shadow Offset X:"));
            _shadowOffsetX = CreateSlider(-15, 15, set.TEXT_SHADOW_OFFSET_X, 0.1); stack.Children.Add(_shadowOffsetX);
            stack.Children.Add(CreateLabel("Shadow Offset Y:"));
            _shadowOffsetY = CreateSlider(-15, 15, set.TEXT_SHADOW_OFFSET_Y, 0.1); stack.Children.Add(_shadowOffsetY);
            stack.Children.Add(CreateLabel("Shadow Blur Radius:"));
            _shadowBlurSlider = CreateSlider(0, 30, set.TEXT_SHADOW_BLUR, 0.5); stack.Children.Add(_shadowBlurSlider);
            AddColorEditor(stack, "Shadow Color:", _shadowHex, h => _shadowHex = h);

            stack.Children.Add(CreateHeader("Glow & Radiance"));
            stack.Children.Add(CreateLabel("Glow Amount:"));
            _glowAmountSlider = CreateSlider(0, 30, set.TEXT_GLOW_AMOUNT, 0.1); stack.Children.Add(_glowAmountSlider);
            AddColorEditor(stack, "Glow Color:", _glowHex, h => _glowHex = h);

            return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildMotionTab()
        {
            var stack = new StackPanel();
            var set = SettingsManager.Current ?? new SystemSettings();

            stack.Children.Add(CreateHeader("Visual Animations & HUD Motion Settings"));
            _enableAnimCheck = new CheckBox { Content = "Enable Motion Animations (Slide-in / Fade-out transitions)", IsChecked = set.ENABLE_ANIMATIONS, FontSize = 12, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 14) };
            stack.Children.Add(_enableAnimCheck);

            var speedGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var speedLabel = new TextBlock { Text = "Transition Speed Preset:", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(speedLabel, 0); speedGrid.Children.Add(speedLabel);
            _speedCombo = new ComboBox { Padding = new Thickness(6, 4, 6, 4), FontSize = 12 };
            _speedCombo.Items.Add("⚡ Fast (120ms)"); _speedCombo.Items.Add("🎬 Standard (220ms)"); _speedCombo.Items.Add("✨ Smooth Spring (350ms)");
            _speedCombo.SelectedIndex = 1;
            Grid.SetColumn(_speedCombo, 1); speedGrid.Children.Add(_speedCombo);
            stack.Children.Add(speedGrid);

            stack.Children.Add(CreateHeader("Kinetic HUD Physics (Window Wobble)"));
            _winWobbleCheck = new CheckBox { Content = "Enable Window Drag Wobble (Spring Elasticity)", IsChecked = set.ENABLE_WINDOW_DRAG_WOBBLE, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_winWobbleCheck);
            stack.Children.Add(CreateLabel("Drag Wobble Intensity:"));
            _winWobbleSlider = CreateSlider(0.1, 5.0, set.WINDOW_DRAG_WOBBLE, 0.1); stack.Children.Add(_winWobbleSlider);
            stack.Children.Add(CreateLabel("Maximum Skew Angle:"));
            _winWobbleMaxSkewSlider = CreateSlider(1.0, 15.0, set.WINDOW_DRAG_WOBBLE_MAX_SKEW, 0.5); stack.Children.Add(_winWobbleMaxSkewSlider);

            stack.Children.Add(CreateHeader("Linguistic Wobbliness"));
            stack.Children.Add(CreateLabel("Text Wobbliness (Sin/Cos Offset):"));
            _textWobbleSlider = CreateSlider(0, 20, set.TEXT_WOBBLINESS, 0.1); stack.Children.Add(_textWobbleSlider);
            stack.Children.Add(CreateLabel("Text Wobble Speed:"));
            _textWobbleSpeedSlider = CreateSlider(0.1, 10.0, set.TEXT_WOBBLE_SPEED, 0.1); stack.Children.Add(_textWobbleSpeedSlider);

            return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildShapesTab()
        {
            var stack = new StackPanel();
            var set = SettingsManager.Current ?? new SystemSettings();

            stack.Children.Add(CreateHeader("Window Geometry & Shape Variety"));
            stack.Children.Add(CreateLabel("Window Base Shape:"));
            _shapeModeCombo = new ComboBox { Margin = new Thickness(0,0,0,10), Height = 30 };
            _shapeModeCombo.Items.Add("Rounded"); _shapeModeCombo.Items.Add("Flat"); _shapeModeCombo.Items.Add("Capsule"); _shapeModeCombo.Items.Add("Cut"); _shapeModeCombo.Items.Add("Slanted"); _shapeModeCombo.Items.Add("Diamond"); _shapeModeCombo.Items.Add("Octagon");
            _shapeModeCombo.SelectedItem = set.WINDOW_SHAPE_MODE ?? "Rounded";
            if (_shapeModeCombo.SelectedIndex < 0) _shapeModeCombo.SelectedIndex = 0;
            stack.Children.Add(_shapeModeCombo);

            stack.Children.Add(CreateLabel("Global Corner Radius:"));
            _cornerRadiusSlider = CreateSlider(0, 60, set.WINDOW_CORNER_RADIUS, 1); stack.Children.Add(_cornerRadiusSlider);

            stack.Children.Add(CreateLabel("HUD Outer Frame Thickness:"));
            _borderThicknessSlider = CreateSlider(0, 10, set.WINDOW_BORDER_THICKNESS, 0.5); stack.Children.Add(_borderThicknessSlider);

            stack.Children.Add(CreateHeader("Glow & Radiance Effects"));
            _winGlowCheck = new CheckBox { Content = "Enable Outer Window Glow (DropShadow)", IsChecked = set.ENABLE_WINDOW_GLOW, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_winGlowCheck);
            stack.Children.Add(CreateLabel("Glow Blur Radius:"));
            _winGlowRadiusSlider = CreateSlider(0, 100, set.WINDOW_GLOW_RADIUS, 1); stack.Children.Add(_winGlowRadiusSlider);

            stack.Children.Add(CreateHeader("Advanced Frame Rendering"));
            _rainbowBorderCheck = new CheckBox { Content = "Enable Dynamic Rainbow Border", IsChecked = set.ENABLE_RAINBOW_BORDER, Foreground = Brushes.Cyan, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_rainbowBorderCheck);
            stack.Children.Add(CreateLabel("Rainbow Rotation Speed:"));
            _rainbowSpeedSlider = CreateSlider(0.1, 20.0, set.RAINBOW_BORDER_SPEED, 0.1); stack.Children.Add(_rainbowSpeedSlider);

            return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildFxTab()
        {
            var stack = new StackPanel();
            var set = SettingsManager.Current ?? new SystemSettings();

            stack.Children.Add(CreateHeader("Retro Scanline Overlays"));
            _scanlineCheck = new CheckBox { Content = "Enable CRT-Style Scanlines", IsChecked = set.ENABLE_SCANLINES, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_scanlineCheck);
            stack.Children.Add(CreateLabel("Scanline Opacity:"));
            _scanlineOpacitySlider = CreateSlider(0, 0.5, set.SCANLINE_OPACITY, 0.01); stack.Children.Add(_scanlineOpacitySlider);
            stack.Children.Add(CreateLabel("Scanline Frequency (Step):"));
            _scanlineFreqSlider = CreateSlider(2, 20, set.SCANLINE_FREQUENCY, 1); stack.Children.Add(_scanlineFreqSlider);

            stack.Children.Add(CreateHeader("Cinematic Optics"));
            _vignetteCheck = new CheckBox { Content = "Enable Screen Vignette (Edge Darkening)", IsChecked = set.ENABLE_VIGNETTE, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_vignetteCheck);
            stack.Children.Add(CreateLabel("Vignette Intensity:"));
            _vignetteIntensitySlider = CreateSlider(0, 1.0, set.VIGNETTE_INTENSITY, 0.05); stack.Children.Add(_vignetteIntensitySlider);

            _grainCheck = new CheckBox { Content = "Enable Film Grain / Noise", IsChecked = set.ENABLE_GRAIN, Foreground = Brushes.White, Margin = new Thickness(0,10,0,10) };
            stack.Children.Add(_grainCheck);
            stack.Children.Add(CreateLabel("Grain Opacity:"));
            _grainOpacitySlider = CreateSlider(0, 0.2, set.GRAIN_OPACITY, 0.01); stack.Children.Add(_grainOpacitySlider);

            stack.Children.Add(CreateHeader("Linguistic Glitch (Chroma Shift)"));
            _chromaCheck = new CheckBox { Content = "Enable Chromatic Aberration (Text Offset)", IsChecked = set.ENABLE_CHROMA_SHIFT, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_chromaCheck);
            stack.Children.Add(CreateLabel("Shift Magnitude (px):"));
            _chromaAmountSlider = CreateSlider(0, 5.0, set.CHROMA_SHIFT_AMOUNT, 0.1); stack.Children.Add(_chromaAmountSlider);

            stack.Children.Add(CreateHeader("Luminescent Pulsation"));
            _glowPulseCheck = new CheckBox { Content = "Enable Outer Glow Breathing Effect", IsChecked = set.ENABLE_GLOW_PULSE, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_glowPulseCheck);
            stack.Children.Add(CreateLabel("Pulse Frequency (Speed):"));
            _glowPulseSpeedSlider = CreateSlider(0.1, 10.0, set.GLOW_PULSE_SPEED, 0.1); stack.Children.Add(_glowPulseSpeedSlider);

            stack.Children.Add(CreateHeader("Interactive Visual Feedback"));
            _clickDarkSpotCheck = new CheckBox { Content = "Enable Click Ripple Dark Spots", IsChecked = set.ENABLE_CLICK_DARK_SPOT, Foreground = Brushes.Cyan, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_clickDarkSpotCheck);

            return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildSystemTab()
        {
            var stack = new StackPanel();
            var set = SettingsManager.Current ?? new SystemSettings();

            stack.Children.Add(CreateHeader("Universal HUD Scaling"));
            _autoScaleCheck = new CheckBox { Content = "Adaptive Auto-Scaling (Sync to Resolution)", IsChecked = set.AUTO_GUI_SCALE_TO_SCREEN, Foreground = Brushes.White, Margin = new Thickness(0,0,0,10) };
            stack.Children.Add(_autoScaleCheck);
            stack.Children.Add(CreateLabel("Global Scale Factor:"));
            _guiScaleSlider = CreateSlider(0.3, 4.0, set.GUI_SCALE, 0.1); stack.Children.Add(_guiScaleSlider);

            stack.Children.Add(CreateHeader("Global Transparency & Legibility"));
            stack.Children.Add(CreateLabel("Overall Overlay Window Fill Opacity:"));
            _winOpacitySlider = CreateSlider(0.3, 1.0, set.WINDOW_OPACITY, 0.05);
            _winOpacityValText = new TextBlock { Text = $"{Math.Round(set.WINDOW_OPACITY * 100)}%", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, HorizontalAlignment = HorizontalAlignment.Right };
            _winOpacitySlider.ValueChanged += (s, e) => {
                if (_winOpacityValText != null) _winOpacityValText.Text = $"{Math.Round(_winOpacitySlider.Value * 100)}%";
                var sCur = SettingsManager.Current;
                if (sCur != null) sCur.WINDOW_OPACITY = Math.Round(_winOpacitySlider.Value, 2);
                ThemeManager.ApplyVisualOverrides();
            };
            stack.Children.Add(_winOpacitySlider); stack.Children.Add(_winOpacityValText);

            stack.Children.Add(CreateLabel("Text & Foreground Opacity Level:"));
            _textOpacitySlider = CreateSlider(0.2, 1.0, 1.0, 0.05);
            _textOpacityValText = new TextBlock { Text = "100%", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, HorizontalAlignment = HorizontalAlignment.Right };
            _textOpacitySlider.ValueChanged += (s, e) => {
                if (_textOpacityValText != null) _textOpacityValText.Text = $"{Math.Round(_textOpacitySlider.Value * 100)}%";
                CommandParser.TriggerTextOpacityChange(Math.Round(_textOpacitySlider.Value, 2));
            };
            stack.Children.Add(_textOpacitySlider); stack.Children.Add(_textOpacityValText);

            stack.Children.Add(CreateHeader("Memory & Resource Management"));
            var purgeBtn = CreateStyledButton("🧹 PURGE SYSTEM CACHE", (s, e) => {
                BaseOverlay.PurgeSystemMemory();
                TextOverlay.Show("⚡ MEMORY OPTIMIZED", 2000);
            }, isPrimary: true);
            purgeBtn.Height = 40;
            stack.Children.Add(purgeBtn);
            stack.Children.Add(new TextBlock { Text = "Releases heavy textures, clears text geometry cache, and forces Garbage Collection.", FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(0,5,0,0) });

            return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private void RefreshStrokeList()
        {
            if (_strokeListPanel == null) return;
            _strokeListPanel.Children.Clear();
            var set = SettingsManager.Current;
            if (set?.TEXT_PROFILES == null) return;

            if (!set.TEXT_PROFILES.ContainsKey(_activeProfile))
                set.TEXT_PROFILES[_activeProfile] = new TextVisualProfile { Name = _activeProfile };

            var prof = set.TEXT_PROFILES[_activeProfile];
            if (prof == null)
            {
                prof = new TextVisualProfile { Name = _activeProfile };
                set.TEXT_PROFILES[_activeProfile] = prof;
            }

            var strokes = prof.Strokes ??= new List<TextStroke>();

            for (int i = 0; i < strokes.Count; i++)
            {
                int idx = i; var stroke = strokes[i];
                var strokeRow = new Border { Margin = new Thickness(0,0,0,10), Padding = new Thickness(12), Background = new SolidColorBrush(Color.FromArgb(35, 0, 0, 0)), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), BorderBrush = Brushes.DimGray };
                var stack = new StackPanel();
                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var title = new OutlinedText { Text = $"LAYER #{idx + 1} ({_activeProfile.ToUpper()})", FontSize = 10, Foreground = Brushes.Cyan, FontWeight = FontWeights.Bold };
                var delBtn = new Button { Content = "×", Background = Brushes.Transparent, Foreground = Brushes.Red, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, FontWeight = FontWeights.Bold, FontSize = 14 };
                delBtn.Click += (s, e) => { strokes.RemoveAt(idx); RefreshStrokeList(); };
                Grid.SetColumn(title, 0); header.Children.Add(title); Grid.SetColumn(delBtn, 1); header.Children.Add(delBtn);
                stack.Children.Add(header);

                stack.Children.Add(new OutlinedText { Text = "Thickness Factor:", FontSize = 9, Foreground = Brushes.Gray, Margin = new Thickness(0,5,0,0) });
                var tSlider = new Slider { Minimum = 0.5, Maximum = 15.0, Value = stroke.Thickness, TickFrequency = 0.1, IsSnapToTickEnabled = true };
                tSlider.ValueChanged += (s, e) => stroke.Thickness = tSlider.Value;
                stack.Children.Add(tSlider);

                AddColorEditor(stack, "Layer Color:", stroke.Color, h => stroke.Color = h);
                strokeRow.Child = stack; _strokeListPanel.Children.Add(strokeRow);
            }
        }

        private void RefreshProfileOptions()
        {
            if (_profileShadowCheck == null) return;
            var set = SettingsManager.Current;
            if (set?.TEXT_PROFILES != null && set.TEXT_PROFILES.TryGetValue(_activeProfile, out var prof) && prof != null)
            {
                _profileShadowCheck.Content = $"Enable Shadow for {_activeProfile} Category";
                _profileShadowCheck.IsChecked = prof.EnableShadow;
            }
        }

        private void SaveAndApply()
        {
            try {
                var s = SettingsManager.Current;
                if (s == null) return;

                s.THEME = "custom";
                s.BACKGROUND_MODE = _bgModeCombo?.SelectedItem?.ToString() ?? "Gradient";
                s.THEME_BG_COLOR = _bgHex; s.THEME_TEXT_COLOR = _textHex; s.THEME_ACCENT_COLOR = _accentHex;
                s.USE_TEXT_GRADIENT = _textGradientCheck?.IsChecked == true;
                s.TEXT_GRADIENT_START = _gradStartHex;
                s.TEXT_GRADIENT_END = _gradEndHex;
                s.BACKGROUND_GIF_PATH = _gifPath;
                if (_gifOpacitySlider != null) s.BACKGROUND_GIF_OPACITY = _gifOpacitySlider.Value;
                if (_gifFpsSlider != null) s.BACKGROUND_GIF_FPS = _gifFpsSlider.Value;

                if (_strokeCheck != null) s.ENABLE_TEXT_STROKE = _strokeCheck.IsChecked == true;
                if (_italicCheck != null) s.TEXT_IS_ITALIC = _italicCheck.IsChecked == true;
                if (_textSizeSlider != null) s.GLOBAL_TEXT_SIZE = _textSizeSlider.Value;
                if (_shadowCheck != null) s.ENABLE_TEXT_SHADOW = _shadowCheck.IsChecked == true;
                if (_shadowOffsetX != null) s.TEXT_SHADOW_OFFSET_X = _shadowOffsetX.Value;
                if (_shadowOffsetY != null) s.TEXT_SHADOW_OFFSET_Y = _shadowOffsetY.Value;
                if (_shadowBlurSlider != null) s.TEXT_SHADOW_BLUR = _shadowBlurSlider.Value;
                s.TEXT_SHADOW_COLOR = _shadowHex;
                if (_glowAmountSlider != null) s.TEXT_GLOW_AMOUNT = _glowAmountSlider.Value;
                s.TEXT_GLOW_COLOR = _glowHex;

                if (s.TEXT_PROFILES != null && s.TEXT_PROFILES.TryGetValue(_activeProfile, out var activeP) && activeP?.Strokes?.Count > 0)
                {
                    s.TEXT_STROKES = new List<TextStroke>(activeP.Strokes.Select(st => new TextStroke { Thickness = st.Thickness, Color = st.Color }));
                }

                if (_enableAnimCheck != null) s.ENABLE_ANIMATIONS = _enableAnimCheck.IsChecked == true;
                if (_winWobbleCheck != null) s.ENABLE_WINDOW_DRAG_WOBBLE = _winWobbleCheck.IsChecked == true;
                if (_winWobbleSlider != null) s.WINDOW_DRAG_WOBBLE = _winWobbleSlider.Value;
                if (_winWobbleMaxSkewSlider != null) s.WINDOW_DRAG_WOBBLE_MAX_SKEW = _winWobbleMaxSkewSlider.Value;
                if (_textWobbleSlider != null) s.TEXT_WOBBLINESS = _textWobbleSlider.Value;
                if (_textWobbleSpeedSlider != null) s.TEXT_WOBBLE_SPEED = _textWobbleSpeedSlider.Value;

                s.WINDOW_SHAPE_MODE = _shapeModeCombo?.SelectedItem?.ToString() ?? "Rounded";
                if (_cornerRadiusSlider != null) s.WINDOW_CORNER_RADIUS = _cornerRadiusSlider.Value;
                if (_borderThicknessSlider != null) s.WINDOW_BORDER_THICKNESS = _borderThicknessSlider.Value;
                if (_winGlowCheck != null) s.ENABLE_WINDOW_GLOW = _winGlowCheck.IsChecked == true;
                if (_winGlowRadiusSlider != null) s.WINDOW_GLOW_RADIUS = _winGlowRadiusSlider.Value;
                if (_rainbowBorderCheck != null) s.ENABLE_RAINBOW_BORDER = _rainbowBorderCheck.IsChecked == true;
                if (_rainbowSpeedSlider != null) s.RAINBOW_BORDER_SPEED = _rainbowSpeedSlider.Value;

                if (_scanlineCheck != null) s.ENABLE_SCANLINES = _scanlineCheck.IsChecked == true;
                if (_scanlineOpacitySlider != null) s.SCANLINE_OPACITY = _scanlineOpacitySlider.Value;
                if (_scanlineFreqSlider != null) s.SCANLINE_FREQUENCY = _scanlineFreqSlider.Value;
                if (_vignetteCheck != null) s.ENABLE_VIGNETTE = _vignetteCheck.IsChecked == true;
                if (_vignetteIntensitySlider != null) s.VIGNETTE_INTENSITY = _vignetteIntensitySlider.Value;
                if (_grainCheck != null) s.ENABLE_GRAIN = _grainCheck.IsChecked == true;
                if (_grainOpacitySlider != null) s.GRAIN_OPACITY = _grainOpacitySlider.Value;
                if (_chromaCheck != null) s.ENABLE_CHROMA_SHIFT = _chromaCheck.IsChecked == true;
                if (_chromaAmountSlider != null) s.CHROMA_SHIFT_AMOUNT = _chromaAmountSlider.Value;
                if (_glowPulseCheck != null) s.ENABLE_GLOW_PULSE = _glowPulseCheck.IsChecked == true;
                if (_glowPulseSpeedSlider != null) s.GLOW_PULSE_SPEED = _glowPulseSpeedSlider.Value;
                if (_clickDarkSpotCheck != null) s.ENABLE_CLICK_DARK_SPOT = _clickDarkSpotCheck.IsChecked == true;

                if (_autoScaleCheck != null) s.AUTO_GUI_SCALE_TO_SCREEN = _autoScaleCheck.IsChecked == true;
                if (_guiScaleSlider != null) s.GUI_SCALE = _guiScaleSlider.Value;
                if (_winOpacitySlider != null) s.WINDOW_OPACITY = Math.Round(_winOpacitySlider.Value, 2);
                if (_textOpacitySlider != null) CommandParser.TriggerTextOpacityChange(Math.Round(_textOpacitySlider.Value, 2));

                ThemeManager.ApplyVisualOverrides();
                BaseOverlay.UpdateAllScales();
                BaseOverlay.GlobalRefreshBackgroundMedia();
                BaseOverlay.GlobalApplyVisualConfig();
                OutlinedText.InvalidateAll();
                SettingsManager.Save();
                TextOverlay.Show("✨ VISUAL PARAMETERS SYNCHRONIZED", 2000);
                UpdateStatus("System visual matrix re-synchronized.");
                this.ApplyGuiScale();
                this.ApplyVisualConfig();
            } catch (Exception ex) { MessageBox.Show("Failed to synchronize: " + ex.Message); }
        }

        private void AddColorEditor(StackPanel stack, string labelText, string initialHex, Action<string> onPicked)
        {
            stack.Children.Add(CreateLabel(labelText));
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var hexBox = new TextBox
            {
                Text = initialHex,
                Height = 32,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(6, 4, 6, 4),
                Background = new SolidColorBrush(Color.FromArgb(40, 20, 20, 30)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.DimGray
            };
            hexBox.TextChanged += (s, e) => onPicked(hexBox.Text.Trim());
            Grid.SetColumn(hexBox, 0);
            grid.Children.Add(hexBox);

            var pickerBtn = CreateStyledButton("🎨 Pick", null, isPrimary: false, fontSize: 10);
            pickerBtn.Width = 72;
            pickerBtn.Height = 32;
            pickerBtn.Margin = new Thickness(5, 0, 0, 0);
            pickerBtn.Click += (s, e) =>
            {
                RgbColorPickerOverlay.Show(hexBox.Text.Trim(), color =>
                {
                    hexBox.Text = color;
                    onPicked(color);
                });
            };
            Grid.SetColumn(pickerBtn, 1);
            grid.Children.Add(pickerBtn);

            stack.Children.Add(grid);
        }

        private Slider CreateSlider(double min, double max, double val, double tick) => new Slider {
            Minimum = min,
            Maximum = max,
            Value = val,
            TickFrequency = tick,
            IsSnapToTickEnabled = true,
            SmallChange = tick,
            LargeChange = tick * 10,
            Margin = new Thickness(0, 5, 0, 10),
            AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft
        };

        private void UpdateStatus(string msg) { if (_statusText != null) { _statusText.Text = $"✅ {msg}"; _statusText.Foreground = Brushes.LightGreen; } }
    }
}
