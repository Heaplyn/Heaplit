// Developer: copilot
// Date: 2026-08-12
// Summary: Legacy entry point for the tunnel manager UI — now merged into the unified MobileOverlay hub.

using System.Windows;

namespace HeaplitLauncher
{
    public static class TunnelOverlay
    {
        public static void ShowOverlay()
        {
            // Tunnel management now lives inside the unified Mobile & Tunnel Hub
            Application.Current.Dispatcher.Invoke(() => MobileOverlay.ShowOverlay());
        }
    }
}

