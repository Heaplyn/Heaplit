// Developer: heaplyn
// Date: 2026-08-09
// Summary: Static tracker for the currently spawned Chrome/Edge web stream process.
//          Any part of the app can call ChromeStreamTracker.Set(process) to register,
//          ChromeStreamTracker.KillIfRunning() to terminate, or ChromeStreamTracker.IsRunning to check status.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HeaplitLauncher
{
    public static class ChromeStreamTracker
    {
        private static readonly List<IntPtr> _spawnedWindows = new List<IntPtr>();
        private static List<IntPtr> _preLaunchWindows = new List<IntPtr>();
        private static Process? _process = null;
        private static int _pid = -1;

        static ChromeStreamTracker()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => KillIfRunning();
        }

        private static List<IntPtr> GetChromeWindows()
        {
            var list = new List<IntPtr>();
            try
            {
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    var sb = new System.Text.StringBuilder(256);
                    if (NativeMethods.GetClassName(hWnd, sb, sb.Capacity) > 0)
                    {
                        if (sb.ToString() == "Chrome_WidgetWin_1")
                        {
                            list.Add(hWnd);
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return list;
        }

    
        public static bool Focus()
        {
            // Strategy 1: Focus using tracked HWND handles (Most reliable for Chrome/Edge)
            lock (_spawnedWindows)
            {
                foreach (var hWnd in _spawnedWindows)
                {
                    if (NativeMethods.IsWindow(hWnd) && NativeMethods.IsWindowVisible(hWnd))
                    {
                        return BringHWndToForeground(hWnd);
                    }
                }
            }

            // Strategy 2: Search windows by target PID if process is alive
            if (_process != null && !_process.HasExited)
            {
                IntPtr targetHWnd = IntPtr.Zero;
                uint targetPid = (uint)_process.Id;

                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
                    if (windowPid == targetPid && NativeMethods.IsWindowVisible(hWnd))
                    {
                        targetHWnd = hWnd;
                        return false; // Found match, stop enumeration
                    }
                    return true;
                }, IntPtr.Zero);

                if (targetHWnd != IntPtr.Zero)
                {
                    return BringHWndToForeground(targetHWnd);
                }

                // Strategy 3: Fallback to standard process focus implementation
                return NativeMethods.FocusProcessInstance(_process);
            }

            return false;
        }

        private static bool BringHWndToForeground(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            // Restore if window is minimized
            if (NativeMethods.IsIconic(hWnd))
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            }

            return NativeMethods.SetForegroundWindow(hWnd);
        }
    

        /// <summary>True if the tracked Chrome stream process or any window is alive.</summary>
        public static bool IsRunning
        {
            get
            {
                lock (_spawnedWindows)
                {
                    _spawnedWindows.RemoveAll(hWnd => !NativeMethods.IsWindow(hWnd));
                    return _spawnedWindows.Count > 0 || (_process != null && !_process.HasExited);
                }
            }
        }

        /// <summary>The PID of the tracked Chrome stream process, or -1 if none.</summary>
        public static int Pid => _pid;

        /// <summary>
        /// Register a newly spawned Chrome/Edge stream process.
        /// </summary>
        public static void Set(Process? process)
        {
            _process = process;
            _pid = process != null ? process.Id : -1;
        }

        /// <summary>Call this just before Process.Start so we can find newly spawned Chrome windows.</summary>
        public static void MarkLaunchTime()
        {
            _preLaunchWindows = GetChromeWindows();

            // Run off-thread to capture newly spawned windows near this launch event
            Task.Run(async () =>
            {
                await Task.Delay(1500); // Allow browser to initialize process tree and open windows

                var postLaunchWindows = GetChromeWindows();
                var newWindows = postLaunchWindows.Except(_preLaunchWindows).ToList();

                lock (_spawnedWindows)
                {
                    foreach (var hWnd in newWindows)
                    {
                        if (!_spawnedWindows.Contains(hWnd))
                        {
                            _spawnedWindows.Add(hWnd);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[ChromeStreamTracker] Tracking window handles: {string.Join(", ", _spawnedWindows)}");
                }
            });
        }

        /// <summary>
        /// Kill the tracked stream process and all chrome windows spawned from it.
        /// </summary>
        public static void KillIfRunning()
        {
            lock (_spawnedWindows)
            {
                foreach (var hWnd in _spawnedWindows)
                {
                    if (NativeMethods.IsWindow(hWnd))
                    {
                        NativeMethods.PostMessage(hWnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                _spawnedWindows.Clear();
            }

            // Kill main process handler if set
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }
                catch { }
                _process = null;
            }
            _pid = -1;
        }
    }
}
