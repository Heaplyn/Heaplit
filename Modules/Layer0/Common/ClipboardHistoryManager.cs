// Developer: heaplyn
// Date: 2026-08-09
// Summary: Background listener and persistent manager for Clipboard History.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HeaplitLauncher
{
    public class ClipboardItem
    {
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public static class ClipboardHistoryManager
    {
        private static readonly List<ClipboardItem> _history = new List<ClipboardItem>();
        private static readonly DispatcherTimer _timer = new DispatcherTimer();
        private static string _lastText = string.Empty;

        public static void Initialize()
        {
            LoadHistory();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += MonitorClipboard;
            _timer.Start();
        }

        private static void MonitorClipboard(object? sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text) && text != _lastText)
                    {
                        _lastText = text;
                        AddHistoryItem(text);
                    }
                }
            }
            catch { }
        }

        public static void AddHistoryItem(string text)
        {
            _history.RemoveAll(x => x.Content == text);
            _history.Insert(0, new ClipboardItem { Content = text, Timestamp = DateTime.Now });

            if (_history.Count > 50)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            SaveHistory();
        }

        public static List<ClipboardItem> GetHistory()
        {
            return new List<ClipboardItem>(_history);
        }

        public static void ClearHistory()
        {
            _history.Clear();
            _lastText = string.Empty;
            SaveHistory();
        }

        private static string GetFilePath()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDir))
            {
                string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Data"));
                if (Directory.Exists(devPath))
                {
                    dataDir = devPath;
                }
                else
                {
                    Directory.CreateDirectory(dataDir);
                }
            }
            return Path.Combine(dataDir, "ClipboardHistory.json");
        }

        private static void LoadHistory()
        {
            try
            {
                string file = GetFilePath();
                if (File.Exists(file))
                {
                    string json = File.ReadAllText(file);
                    var items = JsonSerializer.Deserialize<List<ClipboardItem>>(json);
                    if (items != null)
                    {
                        _history.Clear();
                        _history.AddRange(items);
                        if (_history.Count > 0)
                        {
                            _lastText = _history[0].Content;
                        }
                    }
                }
            }
            catch { }
        }

        private static void SaveHistory()
        {
            try
            {
                string file = GetFilePath();
                string json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);
            }
            catch { }
        }
    }
}
