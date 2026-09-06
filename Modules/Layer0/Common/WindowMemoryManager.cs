// Developer: heaplyn
// Date: 2026-08-13
// Summary: Persistence Memory Manager for Overlay Window States & Positioning.
// Remembers Left, Top, Width, Height, IsMinimized (MiniMode), and WindowState for all overlays across app restarts.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace HeaplitLauncher
{
    public class WindowBoundsState
    {
        public double Left { get; set; } = 0;
        public double Top { get; set; } = 0;
        public double Width { get; set; } = 0;
        public double Height { get; set; } = 0;
        public bool IsMinimized { get; set; } = false;
        public bool IsMaximized { get; set; } = false;
        public bool IsMiniMode { get; set; } = false;
    }

    public static class WindowMemoryManager
    {
        private static readonly string MemoryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "WindowMemory.json");
        private static readonly object _lock = new();
        private static Dictionary<string, WindowBoundsState> _states = new(StringComparer.OrdinalIgnoreCase);

        static WindowMemoryManager()
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
            LoadMemory();
        }

        public static void LoadMemory()
        {
            lock (_lock)
            {
                if (File.Exists(MemoryFile))
                {
                    try
                    {
                        string json = File.ReadAllText(MemoryFile);
                        var data = JsonSerializer.Deserialize<Dictionary<string, WindowBoundsState>>(json);
                        if (data != null) _states = data;
                    }
                    catch { }
                }
            }
        }

        public static void SaveMemory()
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(_states, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(MemoryFile, json);
                }
                catch { }
            }
        }

        public static void SaveWindowBounds(string windowKey, Window window, bool isMiniMode = false)
        {
            if (string.IsNullOrWhiteSpace(windowKey) || window == null) return;

            lock (_lock)
            {
                var bounds = new WindowBoundsState
                {
                    Left = window.Left,
                    Top = window.Top,
                    Width = window.Width,
                    Height = window.Height,
                    IsMinimized = window.WindowState == WindowState.Minimized,
                    IsMaximized = window.WindowState == WindowState.Maximized,
                    IsMiniMode = isMiniMode
                };

                _states[windowKey] = bounds;
                SaveMemory();
            }
        }

        public static bool IsWindowMaximized(string windowKey)
        {
            if (string.IsNullOrWhiteSpace(windowKey)) return false;
            lock (_lock)
            {
                if (_states.TryGetValue(windowKey, out var bounds))
                {
                    return bounds.IsMaximized;
                }
            }
            return false;
        }

        public static bool RestoreWindowBounds(string windowKey, Window window, out bool isMiniMode)
        {
            isMiniMode = false;
            if (string.IsNullOrWhiteSpace(windowKey) || window == null) return false;

            lock (_lock)
            {
                if (_states.TryGetValue(windowKey, out var bounds))
                {
                    if (bounds.Width > 100 && bounds.Height > 80)
                    {
                        window.Width = bounds.Width;
                        window.Height = bounds.Height;
                    }

                    var workArea = SystemParameters.WorkArea;
                    if (bounds.Left >= 0 && bounds.Left < workArea.Width - 100 && bounds.Top >= 0 && bounds.Top < workArea.Height - 100)
                    {
                        window.Left = bounds.Left;
                        window.Top = bounds.Top;
                    }

                    if (bounds.IsMinimized)
                    {
                        window.WindowState = WindowState.Minimized;
                    }
                    else if (bounds.IsMaximized)
                    {
                        window.WindowState = WindowState.Maximized;
                    }

                    isMiniMode = bounds.IsMiniMode;
                    return true;
                }
            }
            return false;
        }
    }
}
