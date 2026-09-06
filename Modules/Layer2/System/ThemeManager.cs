// Developer: heaplyn
// Date: 2026-08-17
// Summary: Master Theme Manager with 60+ visual styles.
//          Restored Corner Radius support and added more themes.

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;
using System.Windows.Controls;

namespace HeaplitLauncher
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeName)
        {
            themeName = themeName.Trim().ToLower();

            if (SettingsManager.Current.BACKGROUND_MODE == "RGB") SettingsManager.Current.BACKGROUND_MODE = "Gradient";

            string bgHex = "#FA1E1035"; string borderHex = "#ffffffe6"; string caretHex = "#FFD9CCFF";
            string hoverHex = "#1C8050E6"; string selectedHex = "#338050E6"; string selectedBorderHex = "#808050E6";
            string gradientStartHex = "#FA301845"; string gradientEndHex = "#FA100820";

            switch (themeName)
            {
                case "matrix": case "hacker": case "terminal":
                    bgHex = "#FA04100A"; borderHex = "#4000ff00"; caretHex = "#00ff00ff"; hoverHex = "#1C00ff00";
                    selectedHex = "#3300ff00"; selectedBorderHex = "#8000ff00"; gradientStartHex = "#FA082612"; gradientEndHex = "#FA020604"; break;

                case "cyberpunk": case "neon":
                    bgHex = "#FA100818"; borderHex = "#ff007fff"; caretHex = "#00ffff"; hoverHex = "#1C00ffff";
                    selectedHex = "#33ff007f"; selectedBorderHex = "#ffff00ff"; gradientStartHex = "#FA251535"; gradientEndHex = "#FA07040E"; break;

                case "dracula": case "vampire":
                    bgHex = "#FA282A36"; borderHex = "#BD93F9"; caretHex = "#FF79C6"; hoverHex = "#1CBD93F9";
                    selectedHex = "#33BD93F9"; selectedBorderHex = "#80BD93F9"; gradientStartHex = "#FA454858"; gradientEndHex = "#FA22232E"; break;

                case "nord": case "arctic":
                    bgHex = "#FA2E3440"; borderHex = "#88C0D0"; caretHex = "#81A1C1"; hoverHex = "#1C88C0D0";
                    selectedHex = "#3388C0D0"; selectedBorderHex = "#8088C0D0"; gradientStartHex = "#F23B4455"; gradientEndHex = "#F2232730"; break;

                case "vaporwave": case "retro":
                    bgHex = "#F21A0F2C"; borderHex = "#FF7B00"; caretHex = "#FF007F"; hoverHex = "#1CFF7B00";
                    selectedHex = "#33FF007F"; selectedBorderHex = "#80FF7B00"; gradientStartHex = "#F23D143A"; gradientEndHex = "#F2120A1E"; break;

                case "lava": case "magma":
                    bgHex = "#F5150500"; borderHex = "#FF4500"; caretHex = "#FFD700"; hoverHex = "#22FF4500";
                    selectedHex = "#448B0000"; selectedBorderHex = "#FF4500"; gradientStartHex = "#F52E0A00"; gradientEndHex = "#F50F0200"; break;

                case "ocean": case "abyssal": case "deep_sea":
                    bgHex = "#FA0C1525"; borderHex = "#2600bfff"; caretHex = "#00bffff9"; hoverHex = "#1C00bfff";
                    selectedHex = "#3300bfff"; selectedBorderHex = "#8000bfff"; gradientStartHex = "#FA182838"; gradientEndHex = "#FA04080F"; break;

                case "blood": case "crimson": case "ruby":
                    bgHex = "#F21A0508"; borderHex = "#DC143C"; caretHex = "#FF2400"; hoverHex = "#1CDC143C";
                    selectedHex = "#33DC143C"; selectedBorderHex = "#80DC143C"; gradientStartHex = "#F23A0815"; gradientEndHex = "#F2100305"; break;

                case "solarized":
                    bgHex = "#F2002B36"; borderHex = "#268BD2"; caretHex = "#859900"; hoverHex = "#1C268BD2";
                    selectedHex = "#33268BD2"; selectedBorderHex = "#80268BD2"; gradientStartHex = "#F2073642"; gradientEndHex = "#F200212B"; break;

                case "emerald": case "forest": case "jade":
                    bgHex = "#F2141F16"; borderHex = "#2E8B57"; caretHex = "#8FBC8F"; hoverHex = "#1C2E8B57";
                    selectedHex = "#332E8B57"; selectedBorderHex = "#802E8B57"; gradientStartHex = "#F21D2E20"; gradientEndHex = "#F20B0F0C"; break;

                case "rainbow": case "rgb": case "spectrum":
                    bgHex = "#F5050505"; borderHex = "#FF00FFFF"; caretHex = "#FFFFFF"; hoverHex = "#22FFFFFF";
                    selectedHex = "#4400FFFF"; selectedBorderHex = "#FF00FFFF"; gradientStartHex = "#F5101010"; gradientEndHex = "#F5000000";
                    SettingsManager.Current.BACKGROUND_MODE = "RGB"; break;

                case "phantom": case "ghost": case "ethereal":
                    bgHex = "#AA000000"; borderHex = "#55FFFFFF"; caretHex = "#FFFFFF"; hoverHex = "#22FFFFFF";
                    selectedHex = "#44FFFFFF"; selectedBorderHex = "#88FFFFFF"; gradientStartHex = "#AA111111"; gradientEndHex = "#AA000000"; break;

                case "obsidian": case "onyx": case "void":
                    bgHex = "#FF050505"; borderHex = "#333333"; caretHex = "#FFFFFF"; hoverHex = "#22FFFFFF";
                    selectedHex = "#44FFFFFF"; selectedBorderHex = "#FFFFFF"; gradientStartHex = "#FF0A0A0A"; gradientEndHex = "#FF000000"; break;

                case "gold": case "luxury": case "amber":
                    bgHex = "#F2141005"; borderHex = "#FFD700"; caretHex = "#FFBF00"; hoverHex = "#1CFFD700";
                    selectedHex = "#33FFD700"; selectedBorderHex = "#80FFD700"; gradientStartHex = "#F22A200B"; gradientEndHex = "#F20C0903"; break;

                case "sakura": case "rose": case "blossom":
                    bgHex = "#F22B1E22"; borderHex = "#FFB7C5"; caretHex = "#FF69B4"; hoverHex = "#1CFFB7C5";
                    selectedHex = "#33FFB7C5"; selectedBorderHex = "#80FFB7C5"; gradientStartHex = "#F23D2A31"; gradientEndHex = "#F21C1216"; break;

                case "midnight": case "dusk": case "shadow":
                    bgHex = "#F2020205"; borderHex = "#1A1A2E"; caretHex = "#16213E"; hoverHex = "#1C0F3460";
                    selectedHex = "#330F3460"; selectedBorderHex = "#800F3460"; gradientStartHex = "#F21A1A2E"; gradientEndHex = "#F20F3460"; break;

                case "plasma": case "nebula": case "nova":
                    bgHex = "#F805000F"; borderHex = "#7F00FF"; caretHex = "#E0B0FF"; hoverHex = "#227F00FF";
                    selectedHex = "#441A0033"; selectedBorderHex = "#7F00FF"; gradientStartHex = "#F80F0025"; gradientEndHex = "#F802000A"; break;

                case "acid": case "toxic": case "radioactive":
                    bgHex = "#F5050F02"; borderHex = "#CCFF00"; caretHex = "#39FF14"; hoverHex = "#22CCFF00";
                    selectedHex = "#44003300"; selectedBorderHex = "#CCFF00"; gradientStartHex = "#F50A2605"; gradientEndHex = "#F5020501"; break;

                case "carbon": case "graphite": case "industrial":
                    bgHex = "#FA232323"; borderHex = "#555555"; caretHex = "#888888"; hoverHex = "#1C444444";
                    selectedHex = "#33444444"; selectedBorderHex = "#666666"; gradientStartHex = "#FA2A2A2A"; gradientEndHex = "#FA151515"; break;

                case "citrus": case "orange": case "amber_glow":
                    bgHex = "#F2251000"; borderHex = "#FF8C00"; caretHex = "#FFA500"; hoverHex = "#1CFF8C00";
                    selectedHex = "#33FF8C00"; selectedBorderHex = "#80FF8C00"; gradientStartHex = "#F23D1A00"; gradientEndHex = "#F2150A00"; break;

                case "ice": case "frost": case "subzero":
                    bgHex = "#F2001525"; borderHex = "#E0F7FA"; caretHex = "#00FFFF"; hoverHex = "#1C00FFFF";
                    selectedHex = "#3300FFFF"; selectedBorderHex = "#8000FFFF"; gradientStartHex = "#F2102838"; gradientEndHex = "#F2050F15"; break;

                case "amethyst": case "quartz": case "violet":
                    bgHex = "#F2150025"; borderHex = "#9966CC"; caretHex = "#E0B0FF"; hoverHex = "#1C9966CC";
                    selectedHex = "#339966CC"; selectedBorderHex = "#809966CC"; gradientStartHex = "#F2251038"; gradientEndHex = "#F20F0515"; break;

                case "light": case "white": case "glass":
                    bgHex = "#E6F5F5FA"; borderHex = "#33000000"; caretHex = "#ff007acc"; hoverHex = "#14007acc";
                    selectedHex = "#22007acc"; selectedBorderHex = "#66007acc"; gradientStartHex = "#E6FFFFFF"; gradientEndHex = "#E6E5E5EA"; break;

                case "purple": default:
                    bgHex = "#FA1E1035"; borderHex = "#ffffffe6"; caretHex = "#FFD9CCFF"; hoverHex = "#1C8050E6";
                    selectedHex = "#338050E6"; selectedBorderHex = "#808050E6"; gradientStartHex = "#FA301845"; gradientEndHex = "#FA100820"; break;
            }

            SetBackgroundResource("WindowBackgroundBrush", bgHex, gradientStartHex, gradientEndHex);
            SetColorResource("WindowBorderBrush", borderHex);
            SetColorResource("AccentCaretBrush", caretHex);
            SetColorResource("HoverBackgroundBrush", hoverHex);
            SetColorResource("SelectedBackgroundBrush", selectedHex);
            SetColorResource("SelectedBorderBrush", selectedBorderHex);

            bool isLight = (themeName == "light" || themeName == "glass" || themeName == "white");
            SetColorResource("TextPrimaryBrush", isLight ? "#111111" : "#FFFFFF");
            SetColorResource("TextPlaceholderBrush", isLight ? "#5A000000" : "#5AFFFFFF");
            SetColorResource("TextSecondaryBrush", isLight ? "#8C000000" : "#8CFFFFFF");

            // --- RESTORE CORNER RADIUS RESOURCES ---
            bool rounded = SettingsManager.Current.USE_ROUNDED_CORNERS;
            Application.Current.Resources["WindowCornerRadius"] = new CornerRadius(rounded ? 12 : 0);
            Application.Current.Resources["ItemCornerRadius"] = new CornerRadius(rounded ? 6 : 0);

            ApplyVisualOverrides();
        }

        public static void ApplyVisualOverrides()
        {
            try
            {
                var set = SettingsManager.Current;

                // 0. Custom Color Overrides (Visual Studio Integration)
                if (set.THEME == "custom")
                {
                    // If the user picked an explicit two-colour gradient, use those stops; otherwise
                    // derive a subtle gradient from the single background colour (legacy behaviour).
                    bool hasCustomGrad = !string.IsNullOrWhiteSpace(set.THEME_GRADIENT_START)
                                      && !string.IsNullOrWhiteSpace(set.THEME_GRADIENT_END);

                    if (set.BACKGROUND_MODE == "Solid") {
                        SetColorResource("WindowBackgroundBrush", set.THEME_BG_COLOR);
                    } else if (set.BACKGROUND_MODE == "Radial") {
                        try {
                            Color inner, outer;
                            if (hasCustomGrad) {
                                inner = (Color)ColorConverter.ConvertFromString(set.THEME_GRADIENT_START);
                                outer = (Color)ColorConverter.ConvertFromString(set.THEME_GRADIENT_END);
                            } else {
                                inner = (Color)ColorConverter.ConvertFromString(set.THEME_BG_COLOR);
                                outer = Color.Multiply(inner, 0.5f);
                            }
                            var brush = new RadialGradientBrush(inner, outer);
                            brush.Freeze();
                            Application.Current.Resources["WindowBackgroundBrush"] = brush;
                        } catch { }
                    } else {
                        try {
                            LinearGradientBrush brush;
                            if (hasCustomGrad) {
                                brush = BuildAngledGradient(set.THEME_GRADIENT_START, set.THEME_GRADIENT_END, set.THEME_GRADIENT_ANGLE);
                            } else {
                                var color = (Color)ColorConverter.ConvertFromString(set.THEME_BG_COLOR);
                                var dark = Color.Multiply(color, 0.7f);
                                brush = new LinearGradientBrush(color, dark, new Point(0,0), new Point(1,1));
                                brush.Freeze();
                            }
                            Application.Current.Resources["WindowBackgroundBrush"] = brush;
                        } catch { }
                    }

                    SetColorResource("TextPrimaryBrush", set.THEME_TEXT_COLOR);
                    SetColorResource("AccentCaretBrush", set.THEME_ACCENT_COLOR);
                    SetColorResource("WindowBorderBrush", set.THEME_ACCENT_COLOR);
                    SetColorResource("HoverBackgroundBrush", set.THEME_ACCENT_COLOR + "22");
                    SetColorResource("SelectedBackgroundBrush", set.THEME_ACCENT_COLOR + "44");
                }

                FontFamily activeFontFamily = null;

                // 1. Custom Font Support
                if (!string.IsNullOrWhiteSpace(set.CUSTOM_FONT_PATH) && File.Exists(set.CUSTOM_FONT_PATH))
                {
                    try
                    {
                        var families = Fonts.GetFontFamilies(new Uri(set.CUSTOM_FONT_PATH));
                        if (families != null && families.Count > 0)
                        {
                            var first = families.First();
                            string fontName = first.FamilyNames.Values.FirstOrDefault() ?? first.Source;
                            if (fontName.Contains("#"))
                            {
                                var parts = fontName.Split('#');
                                fontName = parts[parts.Length - 1];
                            }
                            string dir = Path.GetDirectoryName(set.CUSTOM_FONT_PATH) ?? string.Empty;
                            string file = Path.GetFileName(set.CUSTOM_FONT_PATH);
                            string folderUriStr = "file:///" + dir.Replace("\\", "/") + "/";
                            var folderUri = new Uri(folderUriStr, UriKind.Absolute);
                            activeFontFamily = new FontFamily(folderUri, "./" + file + "#" + fontName);
                        }
                        else
                        {
                            var fontUri = new Uri(set.CUSTOM_FONT_PATH);
                            activeFontFamily = new FontFamily(fontUri, "./#" + set.CUSTOM_FONT_FAMILY);
                        }
                    }
                    catch
                    {
                        try
                        {
                            var fontUri = new Uri(set.CUSTOM_FONT_PATH);
                            activeFontFamily = new FontFamily(fontUri, "./#" + set.CUSTOM_FONT_FAMILY);
                        }
                        catch { }
                    }
                }

                if (activeFontFamily == null)
                {
                    activeFontFamily = new FontFamily(set.CUSTOM_FONT_FAMILY);
                }

                Application.Current.Resources["GlobalFontFamily"] = activeFontFamily;
                Application.Current.Resources["ActiveFontFamily"] = activeFontFamily;

                // Per-text-type font families. Blank = inherit the global/custom font above. These are
                // published as resources so components can opt in (headings, body, code/mono, chat).
                FontFamily Fam(string name, FontFamily fallback)
                    => string.IsNullOrWhiteSpace(name) ? fallback : new FontFamily(name);
                var bodyFont    = Fam(set.BODY_FONT_FAMILY, activeFontFamily);
                var headingFont = Fam(set.HEADING_FONT_FAMILY, activeFontFamily);
                var monoFont    = Fam(set.MONO_FONT_FAMILY, new FontFamily("Consolas"));
                var chatFont    = Fam(set.CHAT_FONT_FAMILY, bodyFont);
                Application.Current.Resources["BodyFontFamily"]    = bodyFont;
                Application.Current.Resources["HeadingFontFamily"] = headingFont;
                Application.Current.Resources["MonoFontFamily"]    = monoFont;
                Application.Current.Resources["ChatFontFamily"]    = chatFont;
                // The global implicit text style follows the Body font when one is chosen.
                activeFontFamily = bodyFont;

                // 1a. Text Gradient Support – apply BEFORE UpdateImplicitStyles so it reads new brush
                if (set.USE_TEXT_GRADIENT)
                {
                    try
                    {
                        var start = (Color)ColorConverter.ConvertFromString(set.TEXT_GRADIENT_START);
                        var end   = (Color)ColorConverter.ConvertFromString(set.TEXT_GRADIENT_END);
                        LinearGradientBrush MakeGrad() {
                            var g = new LinearGradientBrush(start, end, new Point(0, 0), new Point(1, 0));
                            g.Freeze(); return g;
                        }
                        Application.Current.Resources["TextPrimaryBrush"]   = MakeGrad();
                        Application.Current.Resources["TextSecondaryBrush"] = MakeGrad();
                        Application.Current.Resources["TextAccentBrush"]    = MakeGrad();
                    }
                    catch { }
                }
                else
                {
                    // Gradient disabled – restore solid white/black depending on theme
                    bool isLight = set.THEME == "light" || set.THEME == "glass" || set.THEME == "white";
                    var white = new SolidColorBrush(isLight ? Colors.Black : Colors.White); white.Freeze();
                    var grey  = new SolidColorBrush(isLight ? Color.FromRgb(50,50,50) : Color.FromRgb(200,200,200)); grey.Freeze();
                    Application.Current.Resources["TextPrimaryBrush"]   = white;
                    Application.Current.Resources["TextSecondaryBrush"] = grey;
                }

                // 1b. Now update implicit styles – reads updated TextPrimaryBrush above
                UpdateImplicitStyles(activeFontFamily);

                // 2. Glow / Effects
                double glow = set.GLOW_STRENGTH;
                Application.Current.Resources["GlobalGlowOpacity"] = Math.Clamp(glow * 0.5, 0, 1.0);
                Application.Current.Resources["GlobalBlurRadius"] = set.ENABLE_GLASS_BLUR ? set.GLASS_BLUR_DEPTH : 0.0;

                // 3. Text Gradient Support
                if (set.USE_TEXT_GRADIENT)
                {
                    var startColor = (Color)ColorConverter.ConvertFromString(set.TEXT_GRADIENT_START);
                    var endColor = (Color)ColorConverter.ConvertFromString(set.TEXT_GRADIENT_END);
                    var grad = new LinearGradientBrush(startColor, endColor, new Point(0, 0), new Point(1, 1));
                    grad.Freeze();
                    Application.Current.Resources["TextPrimaryBrush"] = grad;
                    Application.Current.Resources["TextSecondaryBrush"] = grad;
                    Application.Current.Resources["TextAccentBrush"] = grad;
                }

                // 4. Glass Transparency & Background Normalization
                if (Application.Current.Resources["WindowBackgroundBrush"] is SolidColorBrush solid)
                {
                    var color = solid.Color;

                    // Master FILL opacity from HUD slider
                    color.A = (byte)(set.WINDOW_OPACITY * 255);

                    var newBrush = new SolidColorBrush(color);
                    newBrush.Freeze();
                    Application.Current.Resources["WindowBackgroundBrush"] = newBrush;
                }
                else if (Application.Current.Resources["WindowBackgroundBrush"] is LinearGradientBrush grad)
                {
                    foreach (var stop in grad.GradientStops)
                    {
                        var color = stop.Color;
                        color.A = (byte)(set.WINDOW_OPACITY * 255);
                        stop.Color = color;
                    }
                }
            }
            catch { }
        }

        public static void SetBackgroundResource(string key, string solidHex, string startHex, string endHex)
        {
            try {
                Brush brush;
                if (SettingsManager.Current.BACKGROUND_MODE == "RGB") {
                    var g = new LinearGradientBrush { StartPoint = new Point(0,0), EndPoint = new Point(1,1) };
                    var s1 = new GradientStop(Color.FromArgb(0xEE, 255, 0, 0), 0.0);
                    var s2 = new GradientStop(Color.FromArgb(0xEE, 0, 255, 0), 0.5);
                    var s3 = new GradientStop(Color.FromArgb(0xEE, 0, 0, 255), 1.0);
                    g.GradientStops.Add(s1); g.GradientStops.Add(s2); g.GradientStops.Add(s3);
                    if (SettingsManager.Current.ENABLE_ANIMATIONS) {
                        var d = TimeSpan.FromSeconds(8);
                        var a1 = new ColorAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever, Duration = d };
                        a1.KeyFrames.Add(new LinearColorKeyFrame(Color.FromRgb(255,0,0), KeyTime.FromPercent(0)));
                        a1.KeyFrames.Add(new LinearColorKeyFrame(Color.FromRgb(0,255,0), KeyTime.FromPercent(0.33)));
                        a1.KeyFrames.Add(new LinearColorKeyFrame(Color.FromRgb(0,0,255), KeyTime.FromPercent(0.66)));
                        a1.KeyFrames.Add(new LinearColorKeyFrame(Color.FromRgb(255,0,0), KeyTime.FromPercent(1)));
                        s1.BeginAnimation(GradientStop.ColorProperty, a1); brush = g;
                    } else brush = g;
                }
                else {
                    var s = (Color)ColorConverter.ConvertFromString(startHex);
                    var e = (Color)ColorConverter.ConvertFromString(endHex);
                    brush = new LinearGradientBrush(s, e, new Point(0,0), new Point(1,1));
                }
                Application.Current.Resources[key] = brush;
            } catch { }
        }

        public static void SetColorResource(string key, string hex) { try { var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); Application.Current.Resources[key] = b; } catch { } }

        /// <summary>Builds a frozen two-stop linear gradient brush oriented at <paramref name="angleDeg"/> degrees (0 = left→right, 90 = top→bottom).</summary>
        public static LinearGradientBrush BuildAngledGradient(string startHex, string endHex, double angleDeg)
        {
            var start = (Color)ColorConverter.ConvertFromString(startHex);
            var end   = (Color)ColorConverter.ConvertFromString(endHex);
            double rad = angleDeg * Math.PI / 180.0;
            double dx = Math.Cos(rad), dy = Math.Sin(rad);
            var brush = new LinearGradientBrush(start, end,
                new Point(0.5 - dx / 2.0, 0.5 - dy / 2.0),
                new Point(0.5 + dx / 2.0, 0.5 + dy / 2.0));
            brush.Freeze();
            return brush;
        }

        private static void UpdateImplicitStyles(FontFamily fontFamily)
        {
            try
            {
                var set = SettingsManager.Current;
                double size = set.GLOBAL_TEXT_SIZE;

                // Grab whatever TextPrimaryBrush is at this moment (may be gradient or solid)
                var fgBrush = Application.Current.Resources.Contains("TextPrimaryBrush")
                    ? (Brush)Application.Current.Resources["TextPrimaryBrush"]
                    : Brushes.White;

                var types = new[] { typeof(Window), typeof(Control), typeof(TextBlock) };
                foreach (var type in types)
                {
                    var style = new Style(type);
                    if (type == typeof(TextBlock))
                    {
                        style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, fontFamily));
                        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, size));
                        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, fgBrush));
                    }
                    else
                    {
                        style.Setters.Add(new Setter(Control.FontFamilyProperty, fontFamily));
                        style.Setters.Add(new Setter(Control.FontSizeProperty, size));
                        style.Setters.Add(new Setter(Control.ForegroundProperty, fgBrush));
                    }
                    Application.Current.Resources[type] = style;
                }
            }
            catch { }
        }
    }
}
