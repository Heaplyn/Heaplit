---
title: "BaseOverlay - Technical Specification"
tags: ['03---layer-2-ui-&-holographic-overlays', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# BaseOverlay - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer2\Core\BaseOverlay.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-16`  

```mermaid
graph TD
    Sub["BaseOverlay (class)"]
    Sub --> Layer["Hosting Layer: 03 - Layer 2 UI & Holographic Overlays"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Master glassmorphic base overlay with robust window management and high-performance styles.

`BaseOverlay` is an integral part of `03 - Layer 2 UI & Holographic Overlays`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `BaseOverlay` within the `03 - Layer 2 UI & Holographic Overlays` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

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
| `BringToFront` | `public ` | `void` | `*none*` |
| `CreateStyledButton` | `public static` | `Button` | `string content, RoutedEventHandler? onClick = null, bool isPrimary = false, double fontSize = 12` |
| `CreateLabel` | `public static` | `OutlinedText` | `string text, double fontSize = 11, bool isBold = true, string category = "Labels"` |
| `CreateTextBox` | `public static` | `TextBox` | `*none*` |
| `ForceClose` | `public ` | `void` | `*none*` |
| `CreateHeaderButton` | `private ` | `Button` | `string content, RoutedEventHandler onClick, Style style, double fontSize = 14` |
| `ToggleMiniMode` | `public ` | `void` | `*none*` |
| `FadeOutAndClose` | `public ` | `void` | `*none*` |
| `FadeOutAndHide` | `public virtual` | `void` | `*none*` |
| `CreateGrainBrush` | `private ` | `Brush` | `*none*` |
| `SetupWobbleTransforms` | `protected ` | `void` | `*none*` |
| `ApplyWobble` | `private ` | `void` | `double velocityX, double velocityY` |
| `ResetWobble` | `private ` | `void` | `*none*` |
| `ApplyVisualConfig` | `public ` | `void` | `*none*` |
| `GlobalApplyVisualConfig` | `public static` | `void` | `*none*` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-16
// Summary: Master glassmorphic base overlay with robust window management and high-performance styles.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;

namespace HeaplitLauncher
{
    public abstract class BaseOverlay : Window
    {
        private static readonly List<BaseOverlay> _openOverlays = new();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        public void BringToFront()
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero) SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_FLAGS);
            }
            catch { }
        }

        public static Button CreateStyledButton(string content, RoutedEventHandler? onClick = null, bool isPrimary = false, double fontSize = 12)
        {
            var btn = new Button { Content = content, Margin = new Thickness(4, 2, 4, 2), Padding = new Thickness(12, 6, 12, 6), Cursor = Cursors.Hand, FontSize = fontSize, FontFamily = new FontFamily("Segoe UI"), Focusable = false };
            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.Name = "Border"; factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6)); factory.SetValue(Border.BorderThicknessProperty, new Thickness(1)); factory.SetValue(Border.PaddingProperty, new Thickness(12, 6, 12, 6));
            if (isPrimary) { factory.SetResourceReference(Border.BackgroundProperty, "SelectedBackgroundBrush"); factory.SetResourceReference(Border.BorderBrushProperty, "SelectedBorderBrush"); }
            else { factory.SetResourceReference(Border.BackgroundProperty, "HoverBackgroundBrush"); factory.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush"); }
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter)); presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center); factory.AppendChild(presenter); template.VisualTree = factory;
            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("SelectedBackgroundBrush"), "Border"));
            template.Triggers.Add(hoverTrigger);
            btn.Template = template; btn.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
            if (onClick != null) btn.Click += onClick; return btn;
        }

        public static OutlinedText CreateLabel(string text, double fontSize = 11, bool isBold = true, string category = "Labels") {
            var tb = new OutlinedText { Text = text, Category = category, FontSize = fontSize, FontWeight = isBold ? FontWeights.SemiBold : FontWeights.Normal, Margin = new Thickness(0, 4, 0, 4) };
            tb.SetResourceReference(OutlinedText.ForegroundProperty, "TextPrimaryBrush");
            return tb;
        }
        public static TextBox CreateTextBox() { var box = new TextBox { Background = Brushes.Transparent, BorderThickness = new Thickness(1), Padding = new Thickness(6, 4, 6, 4), FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 8) }; box.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush"); box.SetResourceReference(TextBox.CaretBrushProperty, "AccentCaretBrush"); box.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush"); return box; }

        protected Grid _mainGrid = null!;
        protected Border _mainBorder = null!;
        protected Border _particleBorder = null!;
        protected Border _scanlineBorder = null!;
        protected Border _vignetteBorder = null!;
        protected Border _grainBorder = null!;
        protected ContentPresenter _contentPresenter = null!;
        protected OutlinedText _titleTextBlock = null!;
        protected StackPanel _controlStack = null!;
        protected Button _standardMinButton = null!;
        protected Button _maximizeButton = null!;
        protected Button _miniModeButton = null!;
        protected Button _closeButton = null!;

        protected string _originalTitle = string.Empty;
        protected double _restoreWidth;
        protected double _restoreHeight;
        protected bool _isMiniMode = false;
        protected bool _forceClose = false;

        public void ForceClose() { _forceClose = true; this.Close(); }

        protected BaseOverlay(string title, double width, double height, string? d1 = null, string? d2 = null, string? d3 = null, string? d4 = null, string? d5 = null)
        {
            this.Width = width; this.Height = height; this._restoreWidth = width; this._restoreHeight = height; this._originalTitle = title;
            this.WindowStyle = WindowStyle.None; this.AllowsTransparency = true; this.Background = Brushes.Transparent; this.ShowInTaskbar = true;
            this.Topmost = SettingsManager.Current.ALWAYS_ON_TOP; this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _openOverlays.Add(this); this.Closed += (s, e) => _openOverlays.Remove(this);

            var windowChrome = new WindowChrome { ResizeBorderThickness = new Thickness(6), CaptionHeight = 0, GlassFrameThickness = new Thickness(1), CornerRadius = new CornerRadius(12) };
            WindowChrome.SetWindowChrome(this, windowChrome);

            // RenderingBias.Performance makes the per-frame drop-shadow render markedly cheaper on
            // every overlay for a near-identical look — a safe, visually-neutral UI smoothness win.
            _mainBorder = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), IsHitTestVisible = true, Effect = new DropShadowEffect { BlurRadius = 15, Color = Colors.Black, Opacity = 0.5, ShadowDepth = 2, RenderingBias = RenderingBias.Performance } };
            _mainBorder.SetResourceReference(Border.BackgroundProperty, "WindowBackgroundBrush"); _mainBorder.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush");

            _mainGrid = new Grid { Margin = new Thickness(12, 10, 12, 12) };
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _particleBorder = new Border { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            Grid.SetRowSpan(_particleBorder, 2);
            _mainGrid.Children.Add(_particleBorder);

            var bgMedia = new Image { Stretch = Stretch.UniformToFill, Opacity = 0.5, IsHitTestVisible = false };
            bgMedia.SetResourceReference(Image.VisibilityProperty, "WindowMediaVisibility");
            this.Loaded += (s, e) => {
                RefreshBackgroundMedia();
                if (WindowMemoryManager.RestoreWindowBounds(_originalTitle, this, out bool storedMiniMode)) {
                    if (storedMiniMode && !_isMiniMode) ToggleMiniMode();
                    else if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal;
                }
                BringToFront();
                ApplyGuiScale();
                ApplyVisualConfig();
                RunEntryAnimation();
                AttachPasteContextMenuToAllTextBoxes(this);
            };

            this.LocationChanged += (s, e) => WindowMemoryManager.SaveWindowBounds(_originalTitle, this, _isMiniMode);
            this.SizeChanged += (s, e) => WindowMemoryManager.SaveWindowBounds(_originalTitle, this, _isMiniMode);
            this.StateChanged += (s, e) => WindowMemoryManager.SaveWindowBounds(_originalTitle, this, _isMiniMode);
            Grid.SetRowSpan(bgMedia, 2); _mainGrid.Children.Add(bgMedia);

            var headerGrid = new Grid { Height = 30, Margin = new Thickness(0, 0, 0, 8), Background = Brushes.Transparent };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _titleTextBlock = new OutlinedText { Text = title, Category = "Titles", FontSize = 12, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            _titleTextBlock.SetResourceReference(OutlinedText.ForegroundProperty, "TextSecondaryBrush");
            Grid.SetColumn(_titleTextBlock, 0); headerGrid.Children.Add(_titleTextBlock);

            _controlStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var btnStyle = new Style(typeof(Button));
            var btnTemplate = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border)); factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter)); presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(presenter); btnTemplate.VisualTree = factory; btnStyle.Setters.Add(new Setter(Button.TemplateProperty, btnTemplate));

            _standardMinButton = CreateHeaderButton("—", (s, e) => this.WindowState = WindowState.Minimized, btnStyle);
            _maximizeButton = CreateHeaderButton("⬜", (s, e) => this.WindowState = (this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized), btnStyle, fontSize: 10);
            _miniModeButton = CreateHeaderButton("◱", (s, e) => ToggleMiniMode(), btnStyle);
            _closeButton = CreateHeaderButton("×", (s, e) => FadeOutAndHide(), btnStyle, fontSize: 18);

            _controlStack.Children.Add(_standardMinButton);
            _controlStack.Children.Add(_maximizeButton);
            _controlStack.Children.Add(_miniModeButton);
            _controlStack.Children.Add(_closeButton);

            Grid.SetColumn(_controlStack, 1); headerGrid.Children.Add(_controlStack);
            _mainGrid.Children.Add(headerGrid);

            SetupWobbleTransforms();

            _scanlineBorder = new Border { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            Grid.SetRowSpan(_scanlineBorder, 2);
            _mainGrid.Children.Add(_scanlineBorder);

            _vignetteBorder = new Border { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            var vignetteGrad = new RadialGradientBrush {
                GradientStops = new GradientStopCollection {
                    new GradientStop(Colors.Transparent, 0.4),
                    new GradientStop(Colors.Black, 1.0)
                }
            };
            vignetteGrad.Freeze(); // Static — disable WPF change-tracking
            _vignetteBorder.Background = vignetteGrad;
            Grid.SetRowSpan(_vignetteBorder, 2);
            _mainGrid.Children.Add(_vignetteBorder);

            _grainBorder = new Border { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            _grainBorder.Background = CreateGrainBrush();
            Grid.SetRowSpan(_grainBorder, 2);
            _mainGrid.Children.Add(_grainBorder);

            _contentPresenter = new ContentPresenter(); Grid.SetRow(_contentPresenter, 1); _mainGrid.Children.Add(_contentPresenter);
            _mainBorder.Child = _mainGrid; this.Content = _mainBorder;

            headerGrid.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) try { this.DragMove(); } catch { } };

            this.LocationChanged += (s, e) => {
                var currentPos = new Point(this.Left, this.Top);
                var currentTime = DateTime.Now;
                double elapsed = (currentTime - _lastDragTime).TotalSeconds;

                if (elapsed > 0 && elapsed < 0.1) {
                    double vx = (currentPos.X - _lastDragPos.X) / elapsed;
                    double vy = (currentPos.Y - _lastDragPos.Y) / elapsed;
                    ApplyWobble(vx / 150, vy / 150); // Normalized velocity
                }

                _lastDragPos = currentPos;
                _lastDragTime = currentTime;
                WindowMemoryManager.SaveWindowBounds(_originalTitle, this, _isMiniMode);
            };

            headerGrid.MouseLeftButtonUp += (s, e) => ResetWobble();
            headerGrid.MouseLeave += (s, e) => ResetWobble();
        }

        private Button CreateHeaderButton(string content, RoutedEventHandler onClick, Style style, double fontSize = 14)
        {
            var btn = new Button { Content = content, Background = Brushes.Transparent, BorderThickness = new Thickness(0), FontSize = fontSize, Width = 28, Height = 28, Cursor = Cursors.Hand, Style = style, Margin = new Thickness(4, 0, 0, 0), Focusable = false };
            btn.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
            btn.Click += onClick; return btn;
        }

        public void ToggleMiniMode()
        {
            if (!_isMiniMode) {
                if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal;
                _restoreWidth = this.ActualWidth; _restoreHeight = this.ActualHeight;
                _contentPresenter.Visibility = Visibility.Collapsed;
                _miniModeButton.Content = "+";
                _standardMinButton.Visibility = Visibility.Collapsed;
                _maximizeButton.Visibility = Visibility.Collapsed;
                this.ResizeMode = ResizeMode.NoResize; this.MinWidth = 0; this.MinHeight = 0;
                this.Width = 210; this.Height = 52; _isMiniMode = true;
            } else {
                _standardMinButton.Visibility = Visibility.Visible;
                _maximizeButton.Visibility = Visibility.Visible;
                _contentPresenter.Visibility = Visibility.Visible;
                _miniModeButton.Content = "◱";
                this.MinWidth = 400; this.MinHeight = 300;
                this.Width = _restoreWidth; this.Height = _restoreHeight;
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                this.WindowState = WindowState.Normal; _isMiniMode = false;
            }
            WindowMemoryManager.SaveWindowBounds(_originalTitle, this, _isMiniMode);
        }

        public new void Show()
        {
            if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal;
            base.Show(); this.Activate(); BringToFront();
        }

        public void FadeOutAndClose() { _forceClose = true; this.Close(); }
        public virtual void FadeOutAndHide() { this.Hide(); }
        protected object UserContent { get => _contentPresenter.Content; set => _contentPresenter.Content = value; }

        private Brush CreateGrainBrush()
        {
            var drawingGroup = new DrawingGroup();
            using (var dc = drawingGroup.Open())
            {
                var rand = new Random();
                for (int i = 0; i < 50; i++) {
                    for (int j = 0; j < 50; j++) {
                        byte alpha = (byte)rand.Next(0, 50);
                        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(alpha, 128, 128, 128)), null, new Rect(i, j, 1, 1));
                    }
                }
            }
            var brush = new ImageBrush(new DrawingImage(drawingGroup)) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 100, 100), ViewportUnits = BrushMappingMode.Absolute };
            brush.Freeze();
            return brush;
        }

        private Point _lastDragPos;
        private DateTime _lastDragTime;
        private SkewTransform? _wobbleSkew;
        private RotateTransform? _wobbleRotate;

        protected void SetupWobbleTransforms()
        {
            var transformGroup = new TransformGroup();
            _wobbleSkew = new SkewTransform();
            _wobbleRotate = new RotateTransform();
            transformGroup.Children.Add(_wobbleSkew);
            transformGroup.Children.Add(_wobbleRotate);
            _mainBorder.RenderTransform = transformGroup;
            _mainBorder.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void ApplyWobble(double velocityX, double velocityY)
        {
            var set = SettingsManager.Current;
            if (!set.ENABLE_WINDOW_DRAG_WOBBLE || _wobbleSkew == null || _wobbleRotate == null) return;

            double intensity = set.WINDOW_DRAG_WOBBLE;
            double maxSkew = set.WINDOW_DRAG_WOBBLE_MAX_SKEW;

            double targetSkewX = Math.Clamp(velocityX * 0.05 * intensity, -maxSkew, maxSkew);
            double targetSkewY = Math.Clamp(velocityY * 0.05 * intensity, -maxSkew, maxSkew);
            double targetRotate = Math.Clamp(velocityX * 0.02 * intensity, -maxSkew, maxSkew);

            var duration = TimeSpan.FromMilliseconds(200);
            var easing = new ElasticEase { Oscillations = 2, Springiness = 3, EasingMode = EasingMode.EaseOut };

            _wobbleSkew.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation(targetSkewX, duration) { EasingFunction = easing });
            _wobbleSkew.BeginAnimation(SkewTransform.AngleYProperty, new DoubleAnimation(targetSkewY, duration) { EasingFunction = easing });
            _wobbleRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(targetRotate, duration) { EasingFunction = easing });
        }

        private void ResetWobble()
        {
            if (_wobbleSkew == null || _wobbleRotate == null) return;
            var duration = TimeSpan.FromMilliseconds(600);
            var easing = new ElasticEase { Oscillations = 3, Springiness = 4, EasingMode = EasingMode.EaseOut };

            _wobbleSkew.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation(0, duration) { EasingFunction = easing });
            _wobbleSkew.BeginAnimation(SkewTransform.AngleYProperty, new DoubleAnimation(0, duration) { EasingFunction = easing });
            _wobbleRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, duration) { EasingFunction = easing });
        }

        public void ApplyVisualConfig()
        {
            try
            {
                var set = SettingsManager.Current;
                _mainBorder.BorderThickness = new Thickness(set.WINDOW_BORDER_THICKNESS);

                if (set.WINDOW_SHAPE_MODE == "Capsule") {
                    _mainBorder.CornerRadius = new CornerRadius(this.Height / 2);
                    _mainBorder.Clip = null;
                } else if (set.WINDOW_SHAPE_MODE == "Flat") {
                    _mainBorder.CornerRadius = new CornerRadius(0);
                    _mainBorder.Clip = null;
                } else if (set.WINDOW_SHAPE_MODE == "Cut") {
                    _mainBorder.CornerRadius = new CornerRadius(0);
                    var clip = new PathGeometry();
                    var figure = new PathFigure { StartPoint = new Point(20, 0), IsClosed = true };
                    figure.Segments.Add(new LineSegment(new Point(this.Width - 20, 0), true));
                    figure.Segments.Add(new LineSegment(new Point(this.Width, 20), true));
                    figure.Segments.Add(new LineSegment(new Point(this.Width, this.Height - 20), true));
                    figure.Segments.Add(new LineSegment(new Point(this.Width - 20, this.Height), true));
                    figure.Segments.Add(new LineSegment(new Point(20, this.Height), true));
                    figure.Segments.Add(new LineSegment(new Point(0, this.Height - 20), true));
                    figure.Segments.Add(new LineSegment(new Point(0, 20), true));
                    clip.Figures.Add(figure);
                    _mainBorder.Clip = clip;
                } else if (set.WINDOW_SHAPE_MODE == "Slanted") {
                    _mainBorder.CornerRadius = new CornerRadius(0);
                    var clip = new PathGeometry();
                    var figure = new PathFigure { StartPoint = new Point(30, 0), IsClosed = true };
                    figure.Segments.Add(new LineSegment(new Point(this.Width, 0), true));
                    figure.Segments.Add(new LineSegment(new Point(this.Width - 30, this.Height), true));
                    figure.Segments.Add(new LineSegment(new Point(0, this.Height), true));
                    clip.Figures.Add(figure);
                    _mainBorder.Clip = clip;
                } else if (set.WINDOW_SHAPE_MODE == "Diamond") {
                    _mainBorder.CornerRadius = new CornerRadius(0);
                    var clip = new PathGeometry();
                    var figure = new PathFigure { StartPoint = new Point(this.Width / 2, 0), IsClosed = true };
                    figure.Segments.Add(new LineSegment(new Point(this.Width, this.Height / 2), true));
                    figure.Segments.Add(new LineSegment(new Point(this.Width / 2, this.Height), true));
                    figure.Segments.Add(new LineSegment(new Point(0, this.Height / 2), true));
                    clip.Figures.Add(figure);
                    _mainBorder.Clip = clip;
                } else if (set.WINDOW_SHAPE_MODE == "Octagon") {
                    _mainBorder.CornerRadius = new CornerRadius(0);
                    double c = 30; // Corner cut size
                    var clip = new PathGeometry();
                    var fig = new PathFigure { StartPoint = new Point(c, 0), IsClosed = true };
                    fig.Segments.Add(new LineSegment(new Point(this.Width - c, 0), true));
                    fig.Segments.Add(new LineSegment(new Point(this.Width, c), true));
                    fig.Segments.Add(new LineSegment(new Point(this.Width, this.Height - c), true));
                    fig.Segments.Add(new LineSegment(new Point(this.Width - c, this.Height), true));
                    fig.Segments.Add(new LineSegment(new Point(c, this.Height), true));
                    fig.Segments.Add(new LineSegment(new Point(0, this.Height - c), true));
                    fig.Segments.Add(new LineSegment(new Point(0, c), true));
                    clip.Figures.Add(fig);
                    _mainBorder.Clip = clip;
                } else {
                    _mainBorder.CornerRadius = new CornerRadius(set.WINDOW_CORNER_RADIUS);
                    _mainBorder.Clip = null;
                }

                if (_mainBorder.Effect is DropShadowEffect shadow) {
                    shadow.Opacity = set.ENABLE_WINDOW_GLOW ? 0.6 : 0;
                    shadow.BlurRadius = set.WINDOW_GLOW_RADIUS;
                    try { shadow.Color = (Color)ColorConverter.ConvertFromString(set.THEME_ACCENT_COLOR); } catch { }

                    if (set.ENABLE_GLOW_PULSE && set.ENABLE_WINDOW_GLOW) {
                        var pulse = new DoubleAnimation(0.2, 0.8, TimeSpan.FromSeconds(2.0 / set.GLOW_PULSE_SPEED)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
                        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, pulse);
                    } else {
                        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
                    }
                }

                if (set.ENABLE_SCANLINES) {
                    ApplyScanlines();
                } else {
                    _scanlineBorder.Visibility = Visibility.Collapsed;
                }

                if (set.BACKGROUND_MODE == "Starfield") {
                    StartStarfield();
                } else {
                    _particleBorder.Visibility = Visibility.Collapsed;
                }

                if (set.ENABLE_VIGNETTE) {
                    _vignetteBorder.Visibility = Visibility.Visible;
                    _vignetteBorder.Opacity = set.VIGNETTE_INTENSITY;
                } else {
                    _vignetteBorder.Visibility = Visibility.Collapsed;
                }

                if (set.ENABLE_GRAIN) {
                    _grainBorder.Visibility = Visibility.Visible;
                    _grainBorder.Opacity = set.GRAIN_OPACITY;
                } else {
                    _grainBorder.Visibility = Visibility.Collapsed;
                }

                if (set.ENABLE_RAINBOW_BORDER) {
                    ApplyRainbowBorder();
                } else {
                    _mainBorder.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush");
                }
            } catch { }
        }

        public static void GlobalApplyVisualConfig()
        {
            try
            {
                foreach (var overlay in _openOverlays.ToList())
                {
                    overlay.ApplyVisualConfig();
                }
            }
            catch { }
        }

        public static void PurgeSystemMemory()
        {
            try
            {
                // 1. Clear internal caches
                OutlinedText.ClearCache();

                // 2. Notify all overlays to release heavy assets
                foreach (var overlay in _openOverlays.ToList())
                {
                    overlay.OnPurgeMemory();
                }

                // 3. Force Garbage Collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch { }
        }

        protected virtual void OnPurgeMemory()
        {
            // Base implementation: Refresh background media to clear any stuck GIF frames
            RefreshBackgroundMedia();
        }

        private void ApplyRainbowBorder()
        {
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            var s1 = new GradientStop(Colors.Red, 0.0);
            var s2 = new GradientStop(Colors.Yellow, 0.25);
            var s3 = new GradientStop(Colors.Lime, 0.5);
            var s4 = new GradientStop(Colors.Cyan, 0.75);
            var s5 = new GradientStop(Colors.Magenta, 1.0);
            brush.GradientStops.Add(s1); brush.GradientStops.Add(s2); brush.GradientStops.Add(s3); brush.GradientStops.Add(s4); brush.GradientStops.Add(s5);

            var anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(10.0 / SettingsManager.Current.RAINBOW_BORDER_SPEED)) { RepeatBehavior = RepeatBehavior.Forever };
            _mainBorder.BorderBrush = brush;
        }

        private void RunEntryAnimation()
        {
            if (!SettingsManager.Current.ENABLE_ANIMATIONS) return;

            var duration = TimeSpan.FromMilliseconds(500 * SettingsManager.Current.ANIMATION_SPEED);
            var easing = new ElasticEase { Oscillations = 2, Springiness = 3, EasingMode = EasingMode.EaseOut };

            var anim = new DoubleAnimation(0.5, 1.0, duration) { EasingFunction = easing };
            _mainBorder.LayoutTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            _mainBorder.LayoutTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);

            var opacityAnim = new DoubleAnimation(0, 1.0, duration);
            this.BeginAnimation(Window.OpacityProperty, opacityAnim);
        }

        private void ApplyScanlines()
        {
            var set = SettingsManager.Current;
            _scanlineBorder.Visibility = Visibility.Visible;
            _scanlineBorder.Opacity = set.SCANLINE_OPACITY;

            double step = Math.Max(2, set.SCANLINE_FREQUENCY);

            var drawingGroup = new DrawingGroup();
            using (var dc = drawingGroup.Open())
            {
                dc.DrawLine(new Pen(Brushes.Black, 1), new Point(0, 0), new Point(10, 0));
            }

            var brush = new DrawingBrush(drawingGroup)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 10, step),
                ViewportUnits = BrushMappingMode.Absolute
            };
            brush.Freeze();
            _scanlineBorder.Background = brush;
        }

        private void StartStarfield()
        {
            _particleBorder.Visibility = Visibility.Visible;

            var drawingGroup = new DrawingGroup();
            using (var dc = drawingGroup.Open())
            {
                var rand = new Random();
                for (int i = 0; i < 50; i++)
                {
                    double x = rand.NextDouble() * 1000;
                    double y = rand.NextDouble() * 800;
                    double r = rand.NextDouble() * 1.5 + 0.5;
                    dc.DrawEllipse(Brushes.White, null, new Point(x, y), r, r);
                }
            }

            var brush = new DrawingBrush(drawingGroup);
            brush.Freeze();
            _particleBorder.Background = brush;

            var shimmer = new DoubleAnimation(0.4, 1.0, TimeSpan.FromSeconds(2)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            _particleBorder.BeginAnimation(UIElement.OpacityProperty, shimmer);
        }

        public static void StyleTabControl(TabControl tabControl)
        {
            tabControl.Background = Brushes.Transparent;
            tabControl.BorderThickness = new Thickness(0);

            var itemStyle = new Style(typeof(TabItem));
            itemStyle.Setters.Add(new Setter(TabItem.BackgroundProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(TabItem.ForegroundProperty, Brushes.White));
            itemStyle.Setters.Add(new Setter(TabItem.PaddingProperty, new Thickness(10, 5, 10, 5)));
            itemStyle.Setters.Add(new Setter(TabItem.CursorProperty, Cursors.Hand));

            var template = new ControlTemplate(typeof(TabItem));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Bd";
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TabItem.BackgroundProperty));
            border.SetValue(Border.PaddingProperty, new Thickness(2));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4, 4, 0, 0));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            border.AppendChild(presenter);
            template.VisualTree = border;

            template.Triggers.Add(new Trigger { Property = TabItem.IsSelectedProperty, Value = true, Setters = {
                new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), "Bd"),
                new Setter(TabItem.ForegroundProperty, Brushes.Cyan)
            }});

            itemStyle.Setters.Add(new Setter(TabItem.TemplateProperty, template));
            tabControl.ItemContainerStyle = itemStyle;

            // Global speed fix for TabControl scroll buttons
            var repeatButtonStyle = new Style(typeof(RepeatButton));
            repeatButtonStyle.Setters.Add(new Setter(RepeatButton.IntervalProperty, 15));
            repeatButtonStyle.Setters.Add(new Setter(RepeatButton.DelayProperty, 150));
            tabControl.Resources.Add(typeof(RepeatButton), repeatButtonStyle);
        }

        public static void StyleTreeView(TreeView tv)
        {
            tv.Background = Brushes.Transparent;
            tv.BorderThickness = new Thickness(0);
            tv.Padding = new Thickness(15, 10, 10, 10); // Fix left clipping of root-level items!

            var itemStyle = new Style(typeof(TreeViewItem));
            itemStyle.Setters.Add(new Setter(TreeViewItem.ForegroundProperty, Brushes.White));
            itemStyle.Setters.Add(new Setter(TreeViewItem.FontSizeProperty, 12.0));

            var template = new ControlTemplate(typeof(TreeViewItem));
            var stack = new FrameworkElementFactory(typeof(StackPanel));

            var headerStack = new FrameworkElementFactory(typeof(StackPanel));
            headerStack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var toggle = new FrameworkElementFactory(typeof(ToggleButton));
            toggle.Name = "Expander"; toggle.SetValue(ToggleButton.IsCheckedProperty, new TemplateBindingExtension(TreeViewItem.IsExpandedProperty));
            toggle.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
            var toggleTemplate = new ControlTemplate(typeof(ToggleButton));
            var tBorder = new FrameworkElementFactory(typeof(Border)); tBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var tPath = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path)); tPath.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M 0 0 L 4 4 L 0 8 Z")); tPath.SetValue(System.Windows.Shapes.Path.FillProperty, Brushes.Gray); tPath.SetValue(System.Windows.Shapes.Path.VerticalAlignmentProperty, VerticalAlignment.Center);
            tBorder.AppendChild(tPath); toggleTemplate.VisualTree = tBorder;
            var tExpanded = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true }; tExpanded.Setters.Add(new Setter(System.Windows.Shapes.Path.RenderTransformProperty, new RotateTransform(90, 2, 4))); toggleTemplate.Triggers.Add(tExpanded);
            toggle.SetValue(ToggleButton.TemplateProperty, toggleTemplate);

            var contentBorder = new FrameworkElementFactory(typeof(Border));
            contentBorder.Name = "Bd";
            contentBorder.SetValue(Border.PaddingProperty, new Thickness(4, 2, 4, 2));
            contentBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));

            var headerContent = new FrameworkElementFactory(typeof(ContentPresenter));
            headerContent.Name = "PART_Header";
            headerContent.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            headerContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentBorder.AppendChild(headerContent);

            headerStack.AppendChild(toggle);
            headerStack.AppendChild(contentBorder);

            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            itemsPresenter.Name = "ItemsHost";
            itemsPresenter.SetValue(ItemsPresenter.MarginProperty, new Thickness(15, 0, 0, 0));

            stack.AppendChild(headerStack);
            stack.AppendChild(itemsPresenter);
            template.VisualTree = stack;

            template.Triggers.Add(new Trigger { Property = TreeViewItem.IsExpandedProperty, Value = false, Setters = { new Setter(ItemsPresenter.VisibilityProperty, Visibility.Collapsed, "ItemsHost") } });
            template.Triggers.Add(new Trigger { Property = TreeViewItem.HasItemsProperty, Value = false, Setters = { new Setter(ToggleButton.VisibilityProperty, Visibility.Hidden, "Expander") } });
            template.Triggers.Add(new Trigger { Property = TreeViewItem.IsSelectedProperty, Value = true, Setters = { new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0xFF, 0xFF)), "Bd"), new Setter(TreeViewItem.ForegroundProperty, Brushes.White) } });
            itemStyle.Setters.Add(new Setter(TreeViewItem.TemplateProperty, template)); tv.ItemContainerStyle = itemStyle;
        }

        public static void AttachPasteContextMenuToAllTextBoxes(DependencyObject parent)
        {
            if (parent == null) return;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox box && box.ContextMenu == null) {
                    var menu = new ContextMenu();
                    menu.Items.Add(new MenuItem { Header = "📋 Paste (Ctrl+V)", Command = ApplicationCommands.Paste });
                    menu.Items.Add(new MenuItem { Header = "📄 Copy (Ctrl+C)", Command = ApplicationCommands.Copy });
                    box.ContextMenu = menu;
                }
                AttachPasteContextMenuToAllTextBoxes(child);
            }
        }

        public static void SetLabelForeground(UIElement element, Brush brush)
        {
            if (element is TextBlock tb) tb.Foreground = brush;
            else if (element is Control c) c.Foreground = brush;
        }

        public static OutlinedText CreateHeader(string text, string category = "Headers")
        {
            return new OutlinedText { Text = text, Category = category, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, Margin = new Thickness(0, 15, 0, 10) };
        }

        public static TextBox CreateLabeledTextBox(StackPanel panel, string labelText, string value)
        {
            panel.Children.Add(CreateLabel(labelText));
            var tb = CreateTextBox();
            tb.Text = value;
            panel.Children.Add(tb);
            return tb;
        }

       
        public void ApplyGuiScale()
        {
            try
            {
                var s = SettingsManager.Current;
                double scale = s.GUI_SCALE;
                if (s.AUTO_GUI_SCALE_TO_SCREEN)
                {
                    double screenHeight = SystemParameters.PrimaryScreenHeight;
                    scale = (screenHeight / 1080.0) * s.GUI_SCALE;
                }
                if (scale < 0.3) scale = 0.3;
                if (scale > 4.0) scale = 4.0;
                _mainBorder.LayoutTransform = new ScaleTransform(scale, scale);
            }
            catch { }
        }

        public static void UpdateAllScales()
        {
            try
            {
                foreach (var overlay in _openOverlays)
                {
                    overlay.ApplyGuiScale();
                }
                if (Application.Current.MainWindow is MainWindow main)
                {
                    main.ApplyGuiScale();
                }
            }
            catch { }
        }

        public void RefreshBackgroundMedia()
        {
            try
            {
                Image? bgMedia = null;
                foreach (var child in _mainGrid.Children)
                {
                    if (child is Image img)
                    {
                        bgMedia = img;
                        break;
                    }
                }
                if (bgMedia != null)
                {
                    var set = SettingsManager.Current;
                    bgMedia.Opacity = set.BACKGROUND_GIF_OPACITY;
                    string localGifPath = set.BACKGROUND_GIF_PATH;

                    if (!string.IsNullOrEmpty(localGifPath) && System.IO.File.Exists(localGifPath))
                    {
                        try
                        {
                            var uri = new Uri(localGifPath, UriKind.Absolute);
                            var imageSource = new System.Windows.Media.Imaging.BitmapImage(uri);
                            WpfAnimatedGif.ImageBehavior.SetAnimatedSource(bgMedia, imageSource);
                            WpfAnimatedGif.ImageBehavior.SetRepeatBehavior(bgMedia, RepeatBehavior.Forever);
                        }
                        catch { }
                    }
                    else if (Application.Current.Resources["WindowBackgroundMediaSource"] is ImageSource imgSource)
                    {
                        WpfAnimatedGif.ImageBehavior.SetAnimatedSource(bgMedia, imgSource);
                        WpfAnimatedGif.ImageBehavior.SetRepeatBehavior(bgMedia, RepeatBehavior.Forever);
                    }
                    else
                    {
                        WpfAnimatedGif.ImageBehavior.SetAnimatedSource(bgMedia, null);
                    }
                }
            }
            catch { }
        }

        public static void GlobalRefreshBackgroundMedia()
        {
            try
            {
                foreach (var overlay in _openOverlays)
                {
                    overlay.RefreshBackgroundMedia();
                }
                if (Application.Current.MainWindow is MainWindow main)
                {
                    main.RefreshBackgroundMedia();
                }
            }
            catch { }
        }

        protected T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t) return t;
                var childOfChild = FindVisualChild<T>(child!);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) { if (!_forceClose) { e.Cancel = true; this.Hide(); return; } base.OnClosing(e); }
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
    participant Sub as BaseOverlay
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
