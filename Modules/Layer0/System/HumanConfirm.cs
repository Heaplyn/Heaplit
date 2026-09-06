// Developer: heaplyn
// Date: 2026-08-31
// Summary: SECURITY - modal yes/no gate for high-risk actions initiated by the model or by
//          autonomous loops (process kill, self-modification, screen capture, etc.).
//          Fails CLOSED: if the prompt cannot be shown, the action is denied.

using System;
using System.Windows;

namespace HeaplitLauncher
{
    public static class HumanConfirm
    {
        public static bool Ask(string message, string title = "Heaplit — Confirm Action")
        {
            try
            {
                var app = Application.Current;
                if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
                {
                    return app.Dispatcher.Invoke(() =>
                        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes);
                }
                return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
            }
            catch
            {
                // Fail closed: no confirmation possible => do not perform the dangerous action.
                return false;
            }
        }
    }
}
