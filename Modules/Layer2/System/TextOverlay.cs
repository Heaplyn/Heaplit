// Developer: heaplyn
// Date: 2026-08-09
// Summary: Draggable text notification overlay inheriting BaseOverlay that auto-closes after a set duration.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace HeaplitLauncher
{
    public class TextOverlay : BaseOverlay
    {
        private static TextOverlay? LastOverlay;
        private static string LastText = string.Empty;

        public static void Show(
            string Text,
            int DurationMs = 1500,
            double Width = 350,
            double Height = 120,
            double FontSize = 20,
            string BackgroundColor = "#F2140D24",
            string TextColor = "#FFFFFF",
            string BorderColor = "#808050E6")
        {
            if (string.IsNullOrEmpty(Text)) return;

            // Execute on UI Dispatcher Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Simple Debounce: Don't show the exact same message if one is already visible
                if (LastOverlay != null && LastOverlay.IsVisible && LastText == Text)
                {
                    return;
                }

                // Close previous toast to prevent stacking if it's the same type of notification
                if (LastOverlay != null && LastOverlay.IsVisible)
                {
                    LastOverlay.FadeOutAndClose();
                }

                var Overlay = new TextOverlay(Text, Width, Height, FontSize, BackgroundColor, TextColor, BorderColor);
                LastOverlay = Overlay;
                LastText = Text;
                Overlay.Show();

                if (DurationMs > 0)
                {
                    var Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DurationMs) };
                    Timer.Tick += (S, E) =>
                    {
                        Timer.Stop();
                        if (LastOverlay == Overlay) LastOverlay = null;
                        Overlay.FadeOutAndClose();
                    };
                    Timer.Start();
                }
            });
        }

        private TextOverlay(
            string Text,
            double Width,
            double Height,
            double FontSize,
            string BgColor,
            string TxtColor,
            string BdrColor)
            : base("NOTIFICATION", Width, Height, BgColor, TxtColor, BdrColor)
        {
            var BrushConverter = new BrushConverter();
            var TxtBrush = (Brush)(BrushConverter.ConvertFromString(TxtColor) ?? Brushes.White);

            var TextBlock = new TextBlock
            {
                Text = Text,
                Foreground = TxtBrush,
                FontSize = FontSize,
                FontFamily = new FontFamily("Segoe UI Semibold, Arial"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            this.UserContent = TextBlock;
        }
    }
}
