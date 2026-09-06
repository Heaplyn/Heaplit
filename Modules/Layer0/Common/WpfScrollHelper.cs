// Developer: heaplyn
// Date: 2026-09-03
// Summary: Universal WPF Mouse Wheel Scroll Fixer and Event Propagator.
//          Resolves WPF's default swallowing of mouse wheel events by RichTextBox, TextBox, ListBox,
//          ComboBox, and other nested controls, ensuring smooth scrolling in parent ScrollViewers.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public static class WpfScrollHelper
    {
        private static bool _initialized = false;

        public static void InitializeGlobalScrollFix()
        {
            if (_initialized) return;
            _initialized = true;

            // Route mouse wheel events for controls that normally swallow them even when not scrolling
            EventManager.RegisterClassHandler(
                typeof(RichTextBox),
                UIElement.PreviewMouseWheelEvent,
                new MouseWheelEventHandler(OnNestedPreviewMouseWheel),
                true
            );

            EventManager.RegisterClassHandler(
                typeof(TextBox),
                UIElement.PreviewMouseWheelEvent,
                new MouseWheelEventHandler(OnNestedPreviewMouseWheel),
                true
            );

            EventManager.RegisterClassHandler(
                typeof(ListBox),
                UIElement.PreviewMouseWheelEvent,
                new MouseWheelEventHandler(OnNestedPreviewMouseWheel),
                true
            );

            EventManager.RegisterClassHandler(
                typeof(ComboBox),
                UIElement.PreviewMouseWheelEvent,
                new MouseWheelEventHandler(OnNestedPreviewMouseWheel),
                true
            );
        }

        private static void OnNestedPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            if (sender is not DependencyObject dep) return;

            // Find nearest parent ScrollViewer
            var parentScroll = FindAncestor<ScrollViewer>(dep);
            if (parentScroll == null) return;

            // If the control itself is actively scrollable and can still scroll in the wheel direction:
            if (sender is TextBox tb)
            {
                if (tb.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled &&
                    tb.VerticalScrollBarVisibility != ScrollBarVisibility.Hidden &&
                    tb.ExtentHeight > tb.ViewportHeight)
                {
                    if ((e.Delta < 0 && tb.VerticalOffset < tb.ExtentHeight - tb.ViewportHeight) ||
                        (e.Delta > 0 && tb.VerticalOffset > 0))
                    {
                        return; // Let the TextBox scroll itself
                    }
                }
            }
            else if (sender is RichTextBox rtb)
            {
                if (rtb.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled &&
                    rtb.VerticalScrollBarVisibility != ScrollBarVisibility.Hidden &&
                    rtb.ExtentHeight > rtb.ViewportHeight)
                {
                    if ((e.Delta < 0 && rtb.VerticalOffset < rtb.ExtentHeight - rtb.ViewportHeight) ||
                        (e.Delta > 0 && rtb.VerticalOffset > 0))
                    {
                        return; // Let the RichTextBox scroll itself
                    }
                }
            }
            else if (sender is ListBox lb)
            {
                var innerScroll = FindDescendant<ScrollViewer>(lb);
                if (innerScroll != null && innerScroll.ScrollableHeight > 0)
                {
                    if ((e.Delta < 0 && innerScroll.VerticalOffset < innerScroll.ScrollableHeight) ||
                        (e.Delta > 0 && innerScroll.VerticalOffset > 0))
                    {
                        return; // Let the ListBox scroll itself
                    }
                }
            }
            else if (sender is ComboBox cb && cb.IsDropDownOpen)
            {
                // When ComboBox popup is open, allow popup to scroll its own items
                return;
            }

            // Propagate mouse wheel delta up to the parent ScrollViewer
            e.Handled = true;
            double scrollAmount = (e.Delta / 3.0 > 0 ? Math.Max(28, e.Delta / 3.0) : Math.Min(-28, e.Delta / 3.0));
            parentScroll.ScrollToVerticalOffset(parentScroll.VerticalOffset - scrollAmount);
        }

        public static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            try
            {
                current = VisualTreeHelper.GetParent(current);
                while (current != null)
                {
                    if (current is T match) return match;
                    current = VisualTreeHelper.GetParent(current);
                }
            }
            catch { }
            return null;
        }

        public static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
        {
            try
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T match) return match;
                    var sub = FindDescendant<T>(child);
                    if (sub != null) return sub;
                }
            }
            catch { }
            return null;
        }
    }
}
