// Developer: heaplyn
// Date: 2026-08-17
// Summary: User System Activity & History Context Service implementation.
//          Uses explicit interface implementation to allow static naming bridges.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeaplitLauncher
{
    public class MemoryManager : IMemoryService
    {
        private CancellationTokenSource? _cts;
        private string _lastWindowTitle = string.Empty;

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        void IMemoryService.Start()
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            MemorySyncer.Start();
            Task.Run(() => MainLoop(_cts.Token));
        }

        void IMemoryService.Stop() { _cts?.Cancel(); _cts = null; MemorySyncer.Stop(); }

        string IMemoryService.GetCurrentWindowTitle() => _lastWindowTitle;

        private async Task MainLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try {
                    IntPtr h = NativeMethods.GetForegroundWindow();
                    StringBuilder sb = new StringBuilder(256);
                    if (GetWindowText(h, sb, 256) > 0) {
                        string current = sb.ToString();
                        if (current != _lastWindowTitle) {
                            _lastWindowTitle = current;
                            ChronoLogManager.LogEvent("Window", $"Switched to: {current}");
                        }
                    }
                } catch { }
                await Task.Delay(2000, token);
            }
        }

        // --- STATIC LEGACY BRIDGES (CRITICAL FOR BUILD) ---
        public static void Start() => CoreRegistry.Data.Memory.Start();
        public static void Stop() => CoreRegistry.Data.Memory.Stop();
        public static string GetCurrentWindowTitle() => CoreRegistry.Data.Memory.GetCurrentWindowTitle();
    }
}
