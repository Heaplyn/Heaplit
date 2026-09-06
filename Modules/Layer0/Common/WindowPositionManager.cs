// Developer: heaplyn
// Date: 2026-08-13
// Summary: Window position & open state persistence manager.
// Automatically records and restores screen coordinates, sizes, and open state of all Heaplit overlays across application restarts.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace HeaplitLauncher
{
    public class WindowPositionState
    {
        public string WindowName { get; set; } = string.Empty;
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsOpen { get; set; }
    }

    public static class WindowPositionManager
    {
        private static string PositionsFilePath => Path.Combine(PathHandler.GetDataDirectory(), "window_positions.json");
        private static Dictionary<string, WindowPositionState> _cache = new();
        private static readonly object _lock = new();

        static WindowPositionManager()
        {
            Load();
        }

        public static void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(PositionsFilePath))
                    {
                        string json = File.ReadAllText(PositionsFilePath);
                        var data = JsonSerializer.Deserialize<Dictionary<string, WindowPositionState>>(json);
                        if (data != null) _cache = data;
                    }
                }
                catch { _cache = new(); }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(PositionsFilePath)!;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(PositionsFilePath, json);
                }
                catch { }
            }
        }

        public static void RegisterWindow(Window window, string windowName)
        {
            if (string.IsNullOrEmpty(windowName)) windowName = window.GetType().Name;

            // Apply saved bounds if present
            lock (_lock)
            {
                if (_cache.TryGetValue(windowName, out var state))
                {
                    if (state.Left > 0 && state.Top > 0 && state.Width > 100 && state.Height > 100)
                    {
                        window.WindowStartupLocation = WindowStartupLocation.Manual;
                        window.Left = state.Left;
                        window.Top = state.Top;
                        window.Width = state.Width;
                        window.Height = state.Height;
                    }
                }
            }

            // Track position updates on move / resize / close
            window.LocationChanged += (s, e) => SaveWindowState(window, windowName, isOpen: window.IsVisible);
            window.SizeChanged += (s, e) => SaveWindowState(window, windowName, isOpen: window.IsVisible);
            window.IsVisibleChanged += (s, e) => SaveWindowState(window, windowName, isOpen: window.IsVisible);
            window.Closed += (s, e) => SaveWindowState(window, windowName, isOpen: false);
        }

        public static void SaveWindowState(Window window, string windowName, bool isOpen)
        {
            if (window == null) return;
            if (string.IsNullOrEmpty(windowName)) windowName = window.GetType().Name;

            lock (_lock)
            {
                _cache[windowName] = new WindowPositionState
                {
                    WindowName = windowName,
                    Left = window.Left,
                    Top = window.Top,
                    Width = window.Width,
                    Height = window.Height,
                    IsOpen = isOpen
                };
            }
            Save();
        }

        public static WindowPositionState? GetSavedState(string windowName)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(windowName, out var state)) return state;
                return null;
            }
        }

        public static void RestoreOpenOverlays()
        {
            try
            {
                string path = @"C:\Users\Kyle\Downloads\Projects\Heaplit\Data\BOOT_DIAGNOSTICS.log";
                System.IO.File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] [WPM] RestoreOpenOverlays called\n");
            } catch { }

            List<string> openWindows;
            lock (_lock)
            {
                openWindows = _cache.Values
                    .Where(v => v.IsOpen)
                    .Select(v => v.WindowName)
                    .ToList();
            }

            foreach (var name in openWindows)
            {
                try
                {
                    string path = @"C:\Users\Kyle\Downloads\Projects\Heaplit\Data\BOOT_DIAGNOSTICS.log";
                    System.IO.File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] [WPM] Attempting to restore: {name}\n");
                } catch { }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        switch (name)
                        {
                            case nameof(VoiceStudioOverlay): VoiceStudioOverlay.ShowOverlay(); break;
                            case nameof(LlmSettingsOverlay): LlmSettingsOverlay.ShowOverlay(); break;
                            case nameof(HuggingFaceOverlay): HuggingFaceOverlay.ShowOverlay(); break;
                            case nameof(TtsVoiceLibraryOverlay): TtsVoiceLibraryOverlay.ShowOverlay(); break;
                            case nameof(OfflineStudioOverlay): OfflineStudioOverlay.ShowOverlay(); break;
                            case nameof(SystemMonitorOverlay): SystemMonitorOverlay.ShowOverlay(); break;
                            case nameof(StickyNotesOverlay): StickyNotesOverlay.ShowOverlay(); break;
                            case nameof(MusicPlaylistOverlay): MusicPlaylistOverlay.ShowOverlay(); break;
                            case nameof(ChatOverlay): ChatOverlay.ShowOverlay(); break;
                            case nameof(SettingsOverlay): SettingsOverlay.ShowSettings(); break;
                            case nameof(CalculusStudioOverlay): CalculusStudioOverlay.ShowStudio(); break;
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            string path = @"C:\Users\Kyle\Downloads\Projects\Heaplit\BOOT_DIAGNOSTICS.log";
                            System.IO.File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] [WPM] Restore Error ({name}): {ex.Message}\n");
                        } catch { }
                    }
                });
            }
        }
    }
}
