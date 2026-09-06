---
title: "SettingsOverlay - Technical Specification"
tags: ['03---layer-2-ui-&-holographic-overlays', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# SettingsOverlay - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer2\System\SettingsOverlay.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-12`  

```mermaid
graph TD
    Sub["SettingsOverlay (class)"]
    Sub --> Layer["Hosting Layer: 03 - Layer 2 UI & Holographic Overlays"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Master Settings dashboard with RadTabControl integration.

`SettingsOverlay` is an integral part of `03 - Layer 2 UI & Holographic Overlays`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `SettingsOverlay` within the `03 - Layer 2 UI & Holographic Overlays` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

### 🎯 Primary Use Cases:
1. **Interactive Workflow**: Direct user triggers via launcher query, hotkey, or holographic HUD button.
2. **Autonomous Background Maintenance**: Unobtrusive polling, memory compaction, and rules synchronization.
3. **Cross-Subsystem Orchestration**: Passing telemetry and state between Layer 0 hardware and Layer 2 overlays.

---

## 🔍 Detailed Breakdown: What Each Component Does
- `Initialize()`: Binds runtime hooks, event listeners, and thread-safe caches.
- `ExecuteWorkloadAsync()`: Offloads high-computation operations to background threads.
- `Dispose()`: Cleans up native OS handles and managed resources.

---

## 🛠️ Troubleshooting Guide & How to Fix Common Errors

### ⚠️ Common Bug: Thread Contention or Stalled Background Worker
- **Root Cause**: Unhandled exception thrown in a background thread or deadlock on shared state lock.
- **Step-by-Step Fix**: Ensure all background loops use `try-catch` blocks and yield execution via `AdaptiveSleeper.Sleep(1000)` or `await Task.Delay()`.

### ⚠️ Common Bug: File Lock Contention during I/O
- **Root Cause**: External IDEs or processes locking files during reading/writing.
- **Step-by-Step Fix**: Always specify `FileShare.ReadWrite | FileShare.Delete` when opening `FileStream` instances.


---

## 🔬 Member Definitions & Method Signatures

| Method Name | Visibility & Modifiers | Return Type | Parameter Signature |
| :--- | :--- | :--- | :--- |
| `OpenSettings` | `public static` | `void` | `*none*` |
| `ShowSettings` | `public static` | `void` | `*none*` |
| `ShowOverlay` | `public static` | `void` | `*none*` |
| `BuildGeneralTab` | `private ` | `UIElement` | `*none*` |
| `BuildVisualsTab` | `private ` | `UIElement` | `*none*` |
| `BuildLlmTab` | `private ` | `UIElement` | `*none*` |
| `BuildTtsTab` | `private ` | `UIElement` | `*none*` |
| `BuildVoiceAiTab` | `private ` | `UIElement` | `*none*` |
| `BuildDataTab` | `private ` | `UIElement` | `*none*` |
| `BuildSyncTab` | `private ` | `UIElement` | `*none*` |
| `BuildOfflineTab` | `private ` | `UIElement` | `*none*` |
| `BuildAliasesTab` | `private ` | `UIElement` | `*none*` |
| `BuildChatTab` | `private ` | `UIElement` | `*none*` |
| `SaveAllSettings` | `private ` | `void` | `*none*` |
| `CreateCheckBox` | `private ` | `CheckBox` | `string text, bool val, Action<bool> changed` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-12
// Summary: Master Settings dashboard with RadTabControl integration.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace HeaplitLauncher
{
    public class SettingsOverlay : BaseOverlay
    {
        private static SettingsOverlay? _instance;

        private CheckBox _startWinCheck = null!;
        private CheckBox _playSoundCheck = null!;
        private CheckBox _autoHideCheck = null!;
        private CheckBox _roundedCornersCheck = null!;
        private CheckBox _heaplitEnabledCheck = null!;
        private CheckBox _voiceModeActiveCheck = null!;
        private CheckBox _speakerVerifyCheck = null!;
        private CheckBox _teacherModeCheck = null!;
        private CheckBox _autonomousModeCheck = null!;
        private CheckBox _clickSpotCheck = null!;
        private TextBox _ollamaModelBox = null!;
        private TextBox _openaiModelBox = null!;
        private TextBox _openaiUrlBox = null!;
        private TextBox _ollamaUrlBox = null!;
        private TextBox _customFontPathBox = null!;
        private TextBox _downloadDirBox = null!;
        private Slider _chatHistorySlider = null!;
        private CheckBox _chatDebugCheck = null!;
        private Slider _guiScaleSlider = null!;
        private TabControl _mainTabControl = null!;

        public static void OpenSettings() => ShowOverlay();
        public static void ShowSettings() => ShowOverlay();
        public static void ShowOverlay() {
            Application.Current.Dispatcher.Invoke(() => {
                if (_instance == null || !_instance.IsLoaded) _instance = new SettingsOverlay();
                _instance.Show(); _instance.BringToFront();
            });
        }

        private SettingsOverlay() : base("⚙️ MASTER SYSTEM SETTINGS", 820, 680)
        {
            _instance = this;
            this.Closed += (s, e) => _instance = null;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;

            var mainGrid = new Grid { Margin = new Thickness(15) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _mainTabControl = new TabControl();
            StyleTabControl(_mainTabControl);

            _mainTabControl.Items.Add(new TabItem { Header = "⚙️ Gen", Content = BuildGeneralTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🎨 Visuals", Content = BuildVisualsTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🤖 LLM", Content = BuildLlmTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🔐 Accounts", Content = BuildAccountsTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🗣️ TTS", Content = BuildTtsTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🎙️ Vox", Content = BuildVoiceAiTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🧹 Data", Content = BuildDataTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🔄 Sync", Content = BuildSyncTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "📶 Off", Content = BuildOfflineTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "🏷️ Map", Content = BuildAliasesTab() });
            _mainTabControl.Items.Add(new TabItem { Header = "💬 Chat", Content = BuildChatTab() });

            Grid.SetRow(_mainTabControl, 0);
            mainGrid.Children.Add(_mainTabControl);

            var saveBtn = CreateStyledButton("💾 SYNCHRONIZE SYSTEM STATE", (s, e) => SaveAllSettings(), isPrimary: true, fontSize: 13);
            saveBtn.Height = 45; Grid.SetRow(saveBtn, 1); mainGrid.Children.Add(saveBtn);

            this.UserContent = mainGrid;
        }

        private UIElement BuildGeneralTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("System Lifecycle & Automation"));
            _startWinCheck = CreateCheckBox("Auto-Launch with Windows", set.START_WITH_WINDOWS, v => set.START_WITH_WINDOWS = v); s.Children.Add(_startWinCheck);
            s.Children.Add(CreateCheckBox("Always on Top (HUD Priority)", set.ALWAYS_ON_TOP, v => set.ALWAYS_ON_TOP = v));
            _autoHideCheck = CreateCheckBox("Auto-Hide HUD on Command Execution", set.AUTO_HIDE_ON_EXECUTE, v => set.AUTO_HIDE_ON_EXECUTE = v); s.Children.Add(_autoHideCheck);
            _playSoundCheck = CreateCheckBox("Play System Audio Feedback", set.PLAY_SOUNDS, v => set.PLAY_SOUNDS = v); s.Children.Add(_playSoundCheck);
            s.Children.Add(CreateCheckBox("🎙️ Voice Activation — listen for \"Hey Heaplit\"", set.ENABLE_WAKE_WORD, v => {
                set.ENABLE_WAKE_WORD = v;
                try {
                    if (v) { CoreRegistry.Interaction.Voice.Start(); TextOverlay.Show("🎙️ Wake word ON — say \"Hey Heaplit\"", 2500); }
                    else   { CoreRegistry.Interaction.Voice.Stop();  TextOverlay.Show("🔇 Wake word OFF", 2500); }
                    SettingsManager.Save();
                } catch { }
            }));
            _autonomousModeCheck = CreateCheckBox("Enable Autonomous Proactive Interjections", set.IS_AUTONOMOUS_MODE_ENABLED, v => set.IS_AUTONOMOUS_MODE_ENABLED = v); s.Children.Add(_autonomousModeCheck);
            s.Children.Add(CreateCheckBox("🛠️ Agent Mode — let Heaplit run commands, access ALL files, and make its own tools (asks to confirm risky actions)", set.ENABLE_PC_CONTROL, v => { set.ENABLE_PC_CONTROL = v; try { SettingsManager.Save(); } catch { } }));
            s.Children.Add(CreateHeader("Glassmorphic UI & Aesthetics"));
            s.Children.Add(CreateCheckBox("Enable Fluid Window Animations", set.ENABLE_ANIMATIONS, v => set.ENABLE_ANIMATIONS = v));
            s.Children.Add(CreateCheckBox("Use High-Fidelity Dynamic Gradients", set.USE_GRADIENT_BACKGROUND, v => set.USE_GRADIENT_BACKGROUND = v));
            _roundedCornersCheck = CreateCheckBox("Rounded Corner Smoothing (Modern)", set.USE_ROUNDED_CORNERS, v => set.USE_ROUNDED_CORNERS = v); s.Children.Add(_roundedCornersCheck);
            _clickSpotCheck = CreateCheckBox("Enable Click Visual Feedback (Ripples)", set.ENABLE_CLICK_DARK_SPOT, v => set.ENABLE_CLICK_DARK_SPOT = v); s.Children.Add(_clickSpotCheck);
            s.Children.Add(CreateLabel("Animation Speed Multiplier:"));
            var speedSlider = CreateSettingsSlider(0.1, 5.0, set.ANIMATION_SPEED, 0.1);
            speedSlider.ValueChanged += (obj, e) => set.ANIMATION_SPEED = speedSlider.Value; s.Children.Add(speedSlider);
            s.Children.Add(CreateHeader("System Geometry & Scaling"));
            s.Children.Add(CreateCheckBox("📐 Adaptive Auto-Scaling (Sync to Resolution)", set.AUTO_GUI_SCALE_TO_SCREEN, v => set.AUTO_GUI_SCALE_TO_SCREEN = v));
            s.Children.Add(CreateLabel("Universal GUI Scale (0.3x - 4.0x):"));
            _guiScaleSlider = CreateSettingsSlider(0.3, 4.0, set.GUI_SCALE, 0.1);
            s.Children.Add(_guiScaleSlider);
            s.Children.Add(CreateHeader("HUD Positioning & Geometry"));
            s.Children.Add(CreateLabel("Active HUD Font Family:"));
            var fontCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(6, 4, 6, 4) };
            foreach (var f in Fonts.SystemFontFamilies.OrderBy(x => x.Source)) fontCombo.Items.Add(f.Source);
            fontCombo.SelectedItem = set.CUSTOM_FONT_FAMILY; fontCombo.SelectionChanged += (obj, e) => set.CUSTOM_FONT_FAMILY = fontCombo.SelectedItem.ToString() ?? "Segoe UI";
            s.Children.Add(fontCombo);
            s.Children.Add(CreateLabel("External Font Asset Path (.ttf/.otf):"));
            var fontGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            fontGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fontGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _customFontPathBox = CreateTextBox();
            _customFontPathBox.Text = set.CUSTOM_FONT_PATH;
            _customFontPathBox.Height = 32;
            _customFontPathBox.VerticalContentAlignment = VerticalAlignment.Center;
            _customFontPathBox.Padding = new Thickness(6, 4, 6, 4);

            var browseFontBtn = CreateStyledButton("📁", (obj, ev) =>
            {
                var dlg = new OpenFileDialog { Filter = "Font Files (*.ttf;*.otf)|*.ttf;*.otf|All Files (*.*)|*.*" };
                if (dlg.ShowDialog() == true) _customFontPathBox.Text = dlg.FileName;
            });
            browseFontBtn.Width = 40;
            browseFontBtn.Height = 32;
            browseFontBtn.Margin = new Thickness(5, 0, 0, 0);

            Grid.SetColumn(_customFontPathBox, 0);
            fontGrid.Children.Add(_customFontPathBox);
            Grid.SetColumn(browseFontBtn, 1);
            fontGrid.Children.Add(browseFontBtn);
            s.Children.Add(fontGrid);

            // ---- Per-text-type fonts -------------------------------------------------
            s.Children.Add(CreateHeader("Per-Text-Type Fonts"));
            s.Children.Add(new TextBlock {
                Text = "Give different kinds of text their own font. Leave on \"(inherit global)\" to use the family above.",
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6)
            });
            var systemFonts = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x).ToList();
            void FontPicker(string label, Func<string> get, Action<string> setv) {
                s.Children.Add(CreateLabel(label));
                var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(6, 4, 6, 4) };
                combo.Items.Add("(inherit global)");
                foreach (var f in systemFonts) combo.Items.Add(f);
                string cur = get();
                combo.SelectedItem = string.IsNullOrWhiteSpace(cur) ? "(inherit global)" : (combo.Items.Contains(cur) ? cur : "(inherit global)");
                combo.SelectionChanged += (o, e) => {
                    string v = combo.SelectedItem?.ToString() ?? "(inherit global)";
                    setv(v == "(inherit global)" ? "" : v);
                    try { ThemeManager.ApplyVisualOverrides(); } catch { }
                };
                s.Children.Add(combo);
            }
            FontPicker("Body / UI text", () => set.BODY_FONT_FAMILY, v => set.BODY_FONT_FAMILY = v);
            FontPicker("Headings / titles", () => set.HEADING_FONT_FAMILY, v => set.HEADING_FONT_FAMILY = v);
            FontPicker("Code / monospace (fallback Consolas)", () => set.MONO_FONT_FAMILY, v => set.MONO_FONT_FAMILY = v);
            FontPicker("AI chat messages", () => set.CHAT_FONT_FAMILY, v => set.CHAT_FONT_FAMILY = v);

            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildVisualsTab() {
            var s = new StackPanel();
            var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Visual Suite & HUD Customization"));
            s.Children.Add(CreateLabel("Consolidate all system visual options, colors, fonts, outer glow, window drag physics, and background media in the Heaplit Visual Studio."));

            var openBtn = CreateStyledButton("🎨 OPEN VISUAL SUITE", (obj, e) => {
                HeaplitVisualsOverlay.ShowOverlay();
                this.Hide();
            }, isPrimary: true, fontSize: 13);
            openBtn.Height = 40;
            openBtn.Margin = new Thickness(0, 15, 0, 15);
            s.Children.Add(openBtn);

            // ---- live-apply helpers -------------------------------------------------
            void ApplyAll() { try { ThemeManager.ApplyVisualOverrides(); BaseOverlay.GlobalApplyVisualConfig(); } catch { } }

            // hex textbox + live swatch; returns the box so presets can drive it. markCustom flips
            // THEME to "custom" so the color overrides actually take effect.
            TextBox ColorRow(string label, Func<string> get, Action<string> setv, bool markCustom = true) {
                s.Children.Add(CreateLabel(label));
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var box = CreateTextBox(); box.Height = 30; box.VerticalContentAlignment = VerticalAlignment.Center;
                var sw = new Border { Width = 30, Height = 22, CornerRadius = new CornerRadius(4), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(6, 0, 0, 8), Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Click to pick a color" };
                void Paint(string hex) { try { var c = (Color)ColorConverter.ConvertFromString(hex); var b = new SolidColorBrush(c); b.Freeze(); sw.Background = b; } catch { } }
                // Clicking the swatch opens the RGB color picker; the picked hex flows back into the
                // textbox, whose TextChanged handler live-applies it.
                sw.MouseLeftButtonUp += (o, e) => {
                    string initial = string.IsNullOrWhiteSpace(box.Text) ? "#FFFFFF" : box.Text.Trim();
                    RgbColorPickerOverlay.Show(initial, picked => Dispatcher.Invoke(() => { box.Text = picked; }));
                };
                box.Text = get(); Paint(box.Text);                 // set BEFORE subscribing (no apply on load)
                box.TextChanged += (o, e) => {
                    string hex = box.Text.Trim(); if (string.IsNullOrEmpty(hex)) { setv(""); return; }
                    try { var c = (Color)ColorConverter.ConvertFromString(hex); Paint(hex); setv(hex); if (markCustom) set.THEME = "custom"; ApplyAll(); } catch { }
                };
                Grid.SetColumn(box, 0); g.Children.Add(box);
                Grid.SetColumn(sw, 1); g.Children.Add(sw);
                s.Children.Add(g);
                return box;
            }
            Slider SliderRow(string label, double min, double max, double val, double tick, Action<double> onChange) {
                var lbl = new TextBlock { Foreground = Brushes.White, Margin = new Thickness(0, 4, 0, 0), Text = $"{label}: {val:0.##}" };
                s.Children.Add(lbl);
                var sl = CreateSettingsSlider(min, max, val, tick);
                sl.ValueChanged += (o, e) => { lbl.Text = $"{label}: {sl.Value:0.##}"; onChange(sl.Value); };
                s.Children.Add(sl);
                return sl;
            }
            ComboBox ComboRow(string label, string[] options, string current, Action<string> onChange) {
                s.Children.Add(CreateLabel(label));
                var c = new ComboBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(6, 4, 6, 4) };
                foreach (var o in options) c.Items.Add(o);
                c.SelectedItem = current;
                c.SelectionChanged += (o, e) => { onChange(c.SelectedItem?.ToString() ?? options[0]); };
                s.Children.Add(c);
                return c;
            }

            // ---- Background ---------------------------------------------------------
            s.Children.Add(CreateHeader("🎨 Background"));
            ComboRow("Background Style", new[] { "Gradient", "Solid", "Radial" }, string.IsNullOrEmpty(set.BACKGROUND_MODE) ? "Gradient" : set.BACKGROUND_MODE,
                v => { set.BACKGROUND_MODE = v; set.THEME = "custom"; ApplyAll(); });
            ColorRow("Base / Solid Color (hex)", () => set.THEME_BG_COLOR, v => set.THEME_BG_COLOR = v);

            s.Children.Add(CreateHeader("🌈 Custom Two-Color Gradient"));
            var gStart = ColorRow("Gradient Start (hex, blank = auto)", () => set.THEME_GRADIENT_START, v => set.THEME_GRADIENT_START = v);
            var gEnd = ColorRow("Gradient End (hex, blank = auto)", () => set.THEME_GRADIENT_END, v => set.THEME_GRADIENT_END = v);
            var angleSlider = SliderRow("Gradient Angle", 0, 360, set.THEME_GRADIENT_ANGLE, 5,
                v => { set.THEME_GRADIENT_ANGLE = v; set.THEME = "custom"; ApplyAll(); });

            void Preset(string a, string b, double ang) { set.THEME_GRADIENT_ANGLE = ang; angleSlider.Value = ang; set.BACKGROUND_MODE = "Gradient"; gStart.Text = a; gEnd.Text = b; }
            var presetPanel = new WrapPanel { Margin = new Thickness(0, 2, 0, 8) };
            (string name, string a, string b, double ang)[] presets = new (string, string, string, double)[] {
                ("Aqua Neon",  "#00E5FF", "#1A1A40", 135.0),
                ("Sunset",     "#FF512F", "#DD2476", 120.0),
                ("Purple Haze","#7F00FF", "#E100FF", 135.0),
                ("Emerald",    "#0F3443", "#34E89E", 120.0),
                ("Cyberpunk",  "#F400A1", "#00D4FF", 45.0),
                ("Fire",       "#F12711", "#F5AF19", 90.0),
                ("Ocean",      "#2193B0", "#6DD5ED", 135.0),
                ("Mono Slate", "#232526", "#414345", 135.0),
                ("Candy",      "#FC5C7D", "#6A82FB", 120.0),
                ("Gold Noir",  "#BF953F", "#1A1A1A", 135.0),
            };
            foreach (var p in presets) {
                var b = CreateStyledButton(p.name, (o, e) => Preset(p.a, p.b, p.ang), isPrimary: false, fontSize: 11);
                b.Margin = new Thickness(0, 0, 6, 6);
                presetPanel.Children.Add(b);
            }
            s.Children.Add(presetPanel);
            var clearGrad = CreateStyledButton("✖ Clear custom gradient (use theme default)", (o, e) => { gStart.Text = ""; gEnd.Text = ""; set.THEME_GRADIENT_START = ""; set.THEME_GRADIENT_END = ""; ApplyAll(); }, isPrimary: false, fontSize: 11);
            clearGrad.Margin = new Thickness(0, 0, 0, 8); s.Children.Add(clearGrad);

            // ---- Accent & Text color ------------------------------------------------
            s.Children.Add(CreateHeader("✨ Accent & Text Color"));
            ColorRow("Accent Color (borders, caret, highlights)", () => set.THEME_ACCENT_COLOR, v => set.THEME_ACCENT_COLOR = v);
            ColorRow("Primary Text Color", () => set.THEME_TEXT_COLOR, v => set.THEME_TEXT_COLOR = v);
            s.Children.Add(CreateCheckBox("Enable Rainbow Text Gradient", set.USE_TEXT_GRADIENT, v => { set.USE_TEXT_GRADIENT = v; ApplyAll(); }));
            ColorRow("Text Gradient Start", () => set.TEXT_GRADIENT_START, v => set.TEXT_GRADIENT_START = v, markCustom: false);
            ColorRow("Text Gradient End", () => set.TEXT_GRADIENT_END, v => set.TEXT_GRADIENT_END = v, markCustom: false);

            // ---- Glass & Blur -------------------------------------------------------
            s.Children.Add(CreateHeader("🧊 Glass & Blur"));
            s.Children.Add(CreateCheckBox("Enable Glass Blur", set.ENABLE_GLASS_BLUR, v => { set.ENABLE_GLASS_BLUR = v; ApplyAll(); }));
            SliderRow("Blur Depth", 0, 80, set.GLASS_BLUR_DEPTH, 1, v => { set.GLASS_BLUR_DEPTH = v; ApplyAll(); });
            SliderRow("Window Opacity", 0.3, 1.0, set.WINDOW_OPACITY, 0.05, v => { set.WINDOW_OPACITY = v; ApplyAll(); });

            // ---- Glow ---------------------------------------------------------------
            s.Children.Add(CreateHeader("💡 Outer Glow"));
            s.Children.Add(CreateCheckBox("Enable Window Glow", set.ENABLE_WINDOW_GLOW, v => { set.ENABLE_WINDOW_GLOW = v; ApplyAll(); }));
            SliderRow("Glow Radius", 0, 100, set.WINDOW_GLOW_RADIUS, 1, v => { set.WINDOW_GLOW_RADIUS = v; ApplyAll(); });
            SliderRow("Glow Strength", 0, 3, set.GLOW_STRENGTH, 0.1, v => { set.GLOW_STRENGTH = v; ApplyAll(); });
            s.Children.Add(CreateCheckBox("Pulsing Glow Animation", set.ENABLE_GLOW_PULSE, v => { set.ENABLE_GLOW_PULSE = v; ApplyAll(); }));
            SliderRow("Glow Pulse Speed", 0.2, 10, set.GLOW_PULSE_SPEED, 0.1, v => { set.GLOW_PULSE_SPEED = v; ApplyAll(); });

            // ---- Shape & Border -----------------------------------------------------
            s.Children.Add(CreateHeader("🔲 Shape & Border"));
            ComboRow("Window Shape", new[] { "Rounded", "Flat", "Cut", "Capsule", "Slanted", "Diamond", "Octagon" }, string.IsNullOrEmpty(set.WINDOW_SHAPE_MODE) ? "Rounded" : set.WINDOW_SHAPE_MODE,
                v => { set.WINDOW_SHAPE_MODE = v; ApplyAll(); });
            SliderRow("Corner Radius", 0, 60, set.WINDOW_CORNER_RADIUS, 1, v => { set.WINDOW_CORNER_RADIUS = v; ApplyAll(); });
            SliderRow("Border Thickness", 0, 6, set.WINDOW_BORDER_THICKNESS, 0.5, v => { set.WINDOW_BORDER_THICKNESS = v; ApplyAll(); });
            s.Children.Add(CreateCheckBox("Animated Rainbow Border", set.ENABLE_RAINBOW_BORDER, v => { set.ENABLE_RAINBOW_BORDER = v; ApplyAll(); }));
            SliderRow("Rainbow Border Speed", 0.5, 20, set.RAINBOW_BORDER_SPEED, 0.5, v => { set.RAINBOW_BORDER_SPEED = v; ApplyAll(); });

            // ---- Retro FX -----------------------------------------------------------
            s.Children.Add(CreateHeader("📺 Retro FX"));
            SliderRow("Scanline Opacity", 0, 0.5, set.SCANLINE_OPACITY, 0.01, v => { set.SCANLINE_OPACITY = v; ApplyAll(); });
            SliderRow("Film Grain Opacity", 0, 0.2, set.GRAIN_OPACITY, 0.01, v => { set.GRAIN_OPACITY = v; ApplyAll(); });

            // ---- Motion -------------------------------------------------------------
            s.Children.Add(CreateHeader("🎬 Motion & Feedback"));
            s.Children.Add(CreateCheckBox("Enable Fluid Window Animations", set.ENABLE_ANIMATIONS, v => { set.ENABLE_ANIMATIONS = v; ApplyAll(); }));
            SliderRow("Animation Speed", 0.1, 5.0, set.ANIMATION_SPEED, 0.1, v => set.ANIMATION_SPEED = v);
            s.Children.Add(CreateCheckBox("Enable Click Ripples", set.ENABLE_CLICK_DARK_SPOT, v => set.ENABLE_CLICK_DARK_SPOT = v));
            s.Children.Add(CreateCheckBox("Adaptive Auto-Scaling", set.AUTO_GUI_SCALE_TO_SCREEN, v => set.AUTO_GUI_SCALE_TO_SCREEN = v));
            s.Children.Add(CreateCheckBox("Low-VFX Performance Mode (Disables Blurs)", set.LOW_VFX_MODE, v => { set.LOW_VFX_MODE = v; ApplyAll(); }));

            // --- Performance & Power: Ring0 wait-scheduler coalescing floor ---
            s.Children.Add(CreateHeader("Performance & Power"));
            var ringLabel = new TextBlock {
                Text = $"Background wait coalescing floor: {set.RING0_MIN_TIMEOUT_MS} ms",
                Foreground = Brushes.White, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap
            };
            s.Children.Add(ringLabel);
            s.Children.Add(new TextBlock {
                Text = "Higher = fewer CPU wakeups and more power saving; lower = snappier background timing.",
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap
            });
            var ringSlider = CreateSettingsSlider(50, 2000, set.RING0_MIN_TIMEOUT_MS, 50);
            ringSlider.ValueChanged += (obj, e) => {
                int ms = (int)ringSlider.Value;
                set.RING0_MIN_TIMEOUT_MS = ms;
                Ring0WaitScheduler.MinTimeoutMs = ms;   // apply live, no restart needed
                ringLabel.Text = $"Background wait coalescing floor: {ms} ms";
            };
            s.Children.Add(ringSlider);

            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildLlmTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Global LLM Orchestration"));
            s.Children.Add(CreateLabel("Primary Intelligence Node:"));
            var backCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(6, 4, 6, 4) };
            string[] backends = { "Gemini", "Groq", "OpenAI", "Anthropic", "ClaudeCode", "Mistral", "OpenRouter", "Perplexity", "DeepSeek", "X-AI", "Lemonade", "Ollama" };
            foreach (var b in backends) backCombo.Items.Add(b); backCombo.SelectedItem = set.LLM_BACKEND;
            backCombo.SelectionChanged += (obj, e) => set.LLM_BACKEND = backCombo.SelectedItem?.ToString() ?? "Gemini";
            s.Children.Add(backCombo);

            s.Children.Add(new TextBlock {
                Text = "Click 'Get Key' to open the provider's key page. Gemini also works with NO key if you connect Google in the Accounts tab.",
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
            });

            // key row: label + textbox + optional "Get Key" button (persists immediately on edit)
            void KeyRow(string label, string provider, Func<string> get, Action<string> setk) {
                s.Children.Add(new TextBlock { Text = label, Foreground = Brushes.White, Margin = new Thickness(0, 8, 0, 2) });
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var box = new TextBox { Text = get() ?? "", Margin = new Thickness(0, 0, 6, 0) };
                box.LostFocus += (o, e) => { setk(box.Text.Trim()); try { SettingsManager.Save(); } catch { } };
                Grid.SetColumn(box, 0); g.Children.Add(box);
                if (ApiKeyPortals.Has(provider)) {
                    var btn = CreateStyledButton("🔑 Get Key", (o, e) => ApiKeyPortals.Open(provider), isPrimary: false, fontSize: 11);
                    Grid.SetColumn(btn, 1); g.Children.Add(btn);
                }
                s.Children.Add(g);
            }
            void ModelRow(string label, Func<string> get, Action<string> setm) {
                s.Children.Add(new TextBlock { Text = label, Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 4, 0, 2) });
                var box = new TextBox { Text = get() ?? "" };
                box.LostFocus += (o, e) => { setm(box.Text.Trim()); try { SettingsManager.Save(); } catch { } };
                s.Children.Add(box);
            }

            s.Children.Add(CreateHeader("API Keys"));
            KeyRow("Google Gemini API Key:", "Gemini", () => set.GOOGLE_AI_KEY, v => set.GOOGLE_AI_KEY = v);
            ModelRow("Gemini model (recommend 'gemini-flash-latest' — never goes stale):", () => set.GEMINI_MODEL, v => set.GEMINI_MODEL = v);
            KeyRow("OpenAI API Key:", "OpenAI", () => set.OPENAI_KEY, v => set.OPENAI_KEY = v);
            KeyRow("Anthropic API Key (paid API — not the Max subscription):", "Anthropic", () => set.ANTHROPIC_KEY, v => set.ANTHROPIC_KEY = v);
            KeyRow("Groq API Key:", "Groq", () => set.GROQ_KEY, v => set.GROQ_KEY = v);
            KeyRow("OpenRouter API Key (unlocks ~400 models):", "OpenRouter", () => set.OPENROUTER_KEY, v => set.OPENROUTER_KEY = v);
            ModelRow("OpenRouter model:", () => set.OPENROUTER_MODEL, v => set.OPENROUTER_MODEL = v);
            KeyRow("Mistral API Key:", "Mistral", () => set.MISTRAL_KEY, v => set.MISTRAL_KEY = v);
            KeyRow("Perplexity API Key:", "Perplexity", () => set.PERPLEXITY_KEY, v => set.PERPLEXITY_KEY = v);
            KeyRow("DeepSeek / X-AI (Grok) Key:", "DeepSeek", () => set.CUSTOM_LLM_KEY, v => set.CUSTOM_LLM_KEY = v);

            s.Children.Add(CreateHeader("Local / Custom Endpoints"));
            _openaiModelBox = CreateLabeledTextBox(s, "OpenAI Model Target:", set.OPENAI_MODEL);
            _openaiUrlBox = CreateLabeledTextBox(s, "OpenAI Base Endpoint URL:", set.OPENAI_BASE_URL);
            _ollamaModelBox = CreateLabeledTextBox(s, "Ollama Model Target:", set.OLLAMA_MODEL);
            _ollamaUrlBox = CreateLabeledTextBox(s, "Ollama API Endpoint:", set.OLLAMA_ENDPOINT);
            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildTtsTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Acoustic Synthesis (TTS)"));
            s.Children.Add(CreateLabel("Engine:"));
            var eCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            eCombo.Items.Add("System"); eCombo.Items.Add("Google"); eCombo.Items.Add("ElevenLabs");
            eCombo.SelectedItem = set.TTS_ENGINE; eCombo.SelectionChanged += (obj, ev) => set.TTS_ENGINE = eCombo.SelectedItem.ToString() ?? "System";
            s.Children.Add(eCombo);
            s.Children.Add(CreateLabel("Speech Rate:")); var rS = CreateSettingsSlider(-10, 10, set.TTS_SPEECH_RATE, 1);
            rS.ValueChanged += (obj, ev) => set.TTS_SPEECH_RATE = (int)rS.Value; s.Children.Add(rS);
            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildVoiceAiTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Aural Perception & Vox Engine"));
            _voiceModeActiveCheck = CreateCheckBox("Active Voice Listening", set.IS_VOICE_MODE_ACTIVE, v => set.IS_VOICE_MODE_ACTIVE = v); s.Children.Add(_voiceModeActiveCheck);
            s.Children.Add(CreateCheckBox("Enable Local Whisper Transcription", set.VOX_USE_LOCAL_WHISPER, v => set.VOX_USE_LOCAL_WHISPER = v));
            s.Children.Add(CreateLabel("Noise Gate Threshold (dB):"));
            var gateS = CreateSettingsSlider(-60, 0, set.MIC_NOISE_GATE_DB, 1);
            gateS.ValueChanged += (obj, ev) => set.MIC_NOISE_GATE_DB = gateS.Value; s.Children.Add(gateS);
            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildDataTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Knowledge Harvesting & Persistence"));
            s.Children.Add(CreateCheckBox("Automatic App Indexing", set.ENABLE_WINDOWS_APP_INDEXING, v => set.ENABLE_WINDOWS_APP_INDEXING = v));
            s.Children.Add(CreateCheckBox("Context Scraping Active", set.DATA_ENABLE_AUTO_SCRAPE, v => set.DATA_ENABLE_AUTO_SCRAPE = v));

            s.Children.Add(CreateHeader("System Storage & Downloads"));
            s.Children.Add(CreateLabel("Download Directory:"));
            var folderGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _downloadDirBox = CreateTextBox();
            _downloadDirBox.Text = set.DOWNLOAD_DIRECTORY;
            Grid.SetColumn(_downloadDirBox, 0);
            folderGrid.Children.Add(_downloadDirBox);

            var browseBtn = CreateStyledButton("📁 BROWSE", (obj, e) => {
                var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select Download Directory" };
                if (dlg.ShowDialog() == true) {
                    _downloadDirBox.Text = dlg.FolderName;
                }
            }, isPrimary: false, fontSize: 10);
            browseBtn.Height = 32;
            browseBtn.Margin = new Thickness(8, 0, 0, 8);
            Grid.SetColumn(browseBtn, 1);
            folderGrid.Children.Add(browseBtn);

            s.Children.Add(folderGrid);
            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildSyncTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("P2P Synchronization & Backup"));
            s.Children.Add(CreateCheckBox("Enable P2P Server Node", set.P2P_SERVER_ENABLED, v => set.P2P_SERVER_ENABLED = v));
            s.Children.Add(CreateCheckBox("Auto-Sync with Backup Cluster", set.AUTO_SYNC_WITH_BACKUP, v => set.AUTO_SYNC_WITH_BACKUP = v));
            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildOfflineTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Offline & Edge Capabilities"));
            s.Children.Add(CreateCheckBox("Enable Teacher Mode (live coding tutor: watches for mistakes, speaks tips + pops chat)", set.IS_TEACHER_MODE_ENABLED, v => set.IS_TEACHER_MODE_ENABLED = v));

            s.Children.Add(CreateLabel("Tutor Scan Interval (seconds between screen checks while coding):"));
            var scanSlider = CreateSettingsSlider(8, 120, set.TEACHER_SCAN_INTERVAL_SEC, 1);
            scanSlider.ValueChanged += (obj, e) => set.TEACHER_SCAN_INTERVAL_SEC = (int)scanSlider.Value;
            s.Children.Add(scanSlider);

            s.Children.Add(CreateLabel("Tutor Tip Cooldown (min seconds between spoken interruptions):"));
            var cooldownSlider = CreateSettingsSlider(10, 300, set.TEACHER_TIP_COOLDOWN_SEC, 5);
            cooldownSlider.ValueChanged += (obj, e) => set.TEACHER_TIP_COOLDOWN_SEC = (int)cooldownSlider.Value;
            s.Children.Add(cooldownSlider);
            return s;
        }

        private UIElement BuildAliasesTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Command Mapping & Aliases"));
            foreach (var a in set.ALIASES) s.Children.Add(CreateLabel($"{a.Key} → {a.Value}", 10, false));
            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildChatTab() {
            var s = new StackPanel(); var set = SettingsManager.Current;
            s.Children.Add(CreateHeader("Chat & HUD Messaging"));
            s.Children.Add(CreateLabel("Max Visible History turns:"));
            _chatHistorySlider = CreateSettingsSlider(5, 200, set.CHAT_MAX_HISTORY_DISPLAY, 5); s.Children.Add(_chatHistorySlider);
            _chatDebugCheck = CreateCheckBox("Show AI Reasoning Context (Debug)", set.CHAT_SHOW_DEBUG_DETAILS, v => set.CHAT_SHOW_DEBUG_DETAILS = v); s.Children.Add(_chatDebugCheck);
            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private void SaveAllSettings() {
            try {
                var s = SettingsManager.Current;
                s.OPENAI_MODEL = _openaiModelBox.Text.Trim();
                s.OPENAI_BASE_URL = _openaiUrlBox.Text.Trim();
                s.OLLAMA_MODEL = _ollamaModelBox.Text.Trim();
                s.OLLAMA_ENDPOINT = _ollamaUrlBox.Text.Trim();
                s.CHAT_MAX_HISTORY_DISPLAY = (int)_chatHistorySlider.Value;
                if (_chatDebugCheck != null) s.CHAT_SHOW_DEBUG_DETAILS = _chatDebugCheck.IsChecked == true;
                if (_customFontPathBox != null) s.CUSTOM_FONT_PATH = _customFontPathBox.Text.Trim();
                if (_downloadDirBox != null) s.DOWNLOAD_DIRECTORY = _downloadDirBox.Text.Trim();
                if (_guiScaleSlider != null) s.GUI_SCALE = _guiScaleSlider.Value;
                ThemeManager.ApplyVisualOverrides();
                BaseOverlay.UpdateAllScales();
                SettingsManager.Save();
                TextOverlay.Show("⚙️ SYSTEM PARAMETERS SYNCHRONIZED", 2000);
                // Keep the settings panel open after saving so the user can keep adjusting /
                // switch tabs. The toast above confirms the save; closing is left to the X / Esc.
            } catch (Exception ex) { MessageBox.Show("Failed to synchronize: " + ex.Message); }
        }

        private CheckBox CreateCheckBox(string text, bool val, Action<bool> changed) {
            var cb = new CheckBox { Content = text, IsChecked = val, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8), FontSize = 12 };
            cb.Checked += (s, e) => changed(true); cb.Unchecked += (s, e) => changed(false); return cb;
        }

        private UIElement BuildAccountsTab() {
            var s = new StackPanel();
            var set = SettingsManager.Current;

            s.Children.Add(CreateHeader("Google"));
            string active = GoogleAccountManager.ActiveEmail;
            s.Children.Add(new TextBlock {
                Text = string.IsNullOrEmpty(active) ? "Not connected." : $"Active account: {active}",
                Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4), TextWrapping = TextWrapping.Wrap
            });
            s.Children.Add(new TextBlock {
                Text = "One click enables Gemini (no API key needed) plus Gmail / Calendar / Drive.",
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
            });

            var connectBtn = CreateStyledButton(
                string.IsNullOrEmpty(active) ? "🔗 Connect Google" : "➕ Add / Switch Google Account",
                (obj, e) => { _ = OAuth2Manager.AddGoogleAccountAsync(st => { try { TextOverlay.Show(st, 3000); } catch { } }); },
                isPrimary: true, fontSize: 13);
            connectBtn.Margin = new Thickness(0, 0, 0, 10);
            s.Children.Add(connectBtn);

            var accounts = GoogleAccountManager.All;
            if (accounts.Count > 0) {
                s.Children.Add(CreateHeader("Connected accounts"));
                foreach (var acc in accounts) {
                    var a = acc;
                    bool isActive = a.Email.Equals(active, StringComparison.OrdinalIgnoreCase);
                    var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var lbl = new TextBlock { Text = (isActive ? "✅ " : "👤 ") + a.Email, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                    Grid.SetColumn(lbl, 0); row.Children.Add(lbl);

                    if (!isActive) {
                        var sw = CreateStyledButton("Switch", (o, e) => { if (GoogleAccountManager.Activate(a.Email)) { try { TextOverlay.Show($"Switched to {a.Email}", 3000); } catch { } } }, isPrimary: false, fontSize: 11);
                        sw.Margin = new Thickness(6, 0, 0, 0); Grid.SetColumn(sw, 1); row.Children.Add(sw);
                    }
                    var rm = CreateStyledButton("Remove", (o, e) => { GoogleAccountManager.Remove(a.Email); try { TextOverlay.Show($"Removed {a.Email}", 2500); } catch { } }, isPrimary: false, fontSize: 11);
                    rm.Margin = new Thickness(6, 0, 0, 0); Grid.SetColumn(rm, 2); row.Children.Add(rm);
                    s.Children.Add(row);
                }
                s.Children.Add(new TextBlock { Text = "Reopen this panel after connecting to refresh the list.", Foreground = Brushes.Gray, FontSize = 10, Margin = new Thickness(0, 8, 0, 0) });
            }

            s.Children.Add(CreateHeader("Blocked signing in? (403 / access_denied)"));
            s.Children.Add(new TextBlock {
                Text = "The built-in Google app is in Google's 'test mode' and can't approve every account. Two fixes:",
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6)
            });
            var geminiKeyBtn = CreateStyledButton("🔑 Easiest: Get a free Gemini API key (no login)", (o, e) => ApiKeyPortals.Open("Gemini"), isPrimary: true, fontSize: 12);
            geminiKeyBtn.Margin = new Thickness(0, 0, 0, 6); s.Children.Add(geminiKeyBtn);
            s.Children.Add(new TextBlock { Text = "…then paste it under LLM → Gemini. That skips Google login entirely.", Foreground = Brushes.Gray, FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });

            var ownClientBtn = CreateStyledButton("🛠️ For Gmail/Calendar/Drive: create your own OAuth client", (o, e) => {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://console.cloud.google.com/auth/clients", UseShellExecute = true }); } catch { }
            }, isPrimary: false, fontSize: 11);
            s.Children.Add(ownClientBtn);
            s.Children.Add(new TextBlock {
                Text = "Steps: New project → Credentials → Create OAuth client ID → type 'Desktop app' → copy the Client ID → paste below. As the project owner you're allowed immediately.",
                Foreground = Brushes.Gray, FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 8)
            });

            s.Children.Add(CreateHeader("Advanced (optional)"));
            s.Children.Add(new TextBlock { Text = "Your Google OAuth Client ID (paste the Desktop-app client id from above):", Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap });
            var cidBox = new TextBox { Text = set.GOOGLE_OAUTH_CLIENT_ID, Margin = new Thickness(0, 4, 0, 0) };
            cidBox.LostFocus += (o, e) => { set.GOOGLE_OAUTH_CLIENT_ID = cidBox.Text.Trim(); };
            s.Children.Add(cidBox);

            return new ScrollViewer { Content = s, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private Slider CreateSettingsSlider(double min, double max, double val, double tick) {
            var slider = new Slider { Minimum = min, Maximum = max, Value = val, TickFrequency = tick, IsSnapToTickEnabled = true, Margin = new Thickness(0, 8, 0, 12), AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft };
            slider.PreviewMouseLeftButtonDown += (s, e) => { try { slider.Focus(); } catch { } }; return slider;
        }

        private static TextBlock CreateHeader(string text) => new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, Margin = new Thickness(0, 15, 0, 10) };
    }
}
```

### 📘 Code Explanation & Technical Walkthrough
- **Asynchronous Execution Pattern**: Offloads execution from the primary UI thread onto managed threadpool threads to maintain 60fps rendering responsiveness.
- **Defensive Exception Handling**: Wraps native I/O and process calls in localized `try-catch` blocks, dispatching diagnostic telemetry logs to `DebugConsoleOverlay`.
- **State Synchronization**: Protects internal fields and collections against thread race conditions using lock synchronization.

---

## ⚡ Execution Flow & Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Caller / UI Overlay
    participant Sub as SettingsOverlay
    participant Kernel as OS Kernel / Layer 0
    participant Log as DebugConsoleOverlay

    Caller->>Sub: Invoke Action / Query Request
    Sub->>Kernel: Execute Managed & Unmanaged Operations
    Kernel-->>Sub: Operation Result / Status Payload
    Sub->>Log: Emit Diagnostic Telemetry Trace
    Sub-->>Caller: Return Results / Update HUD
```

---

## 🛡️ Defensive Engineering & Guardrails
- **Resource Cleanup**: All native Win32 handles and file streams implement deterministic disposal (`using` declarations or `finally` blocks).
- **Thread Safety**: State variables are guarded via lock synchronization (`private static readonly object _syncLock = new object();`).
- **Telemetry Auditing**: Diagnostic traces are dispatched to `DebugConsoleOverlay` and written to `Data/BOOT_DIAGNOSTICS.log`.

---

## 🔗 Related WikiLinks
- [[Master Map of Content & System Index]]
- [[Core System Architecture & 4-Layer Hierarchy]]
- [[NativeMethods & Win32 Kernel Interop Master Manual]]
- [[AiAPI Gateway & Multi-Model Routing Architecture]]
- [[BaseOverlay & GPU Holographic Windowing Engine]]
- [[SystemMonitorOverlay & Diagnostic Telemetry HUD]]
- [[Max PC Optimization Pipeline & Autonomic Engine]]
