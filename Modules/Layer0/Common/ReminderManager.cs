// Developer: copilot
// Date: 2026-08-13
// Summary: Handles background reminder scheduling, JSON serialization, and sound/visual notifications when reminders mature.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Threading;
using System.Media;

namespace HeaplitLauncher
{
    public class ReminderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public DateTime TargetTime { get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public static class ReminderManager
    {
        private static List<ReminderItem> _reminders = new List<ReminderItem>();
        private static DispatcherTimer? _checkTimer;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            LoadReminders();

            _checkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _checkTimer.Tick += (s, e) => CheckReminders();
            _checkTimer.Start();
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
            return Path.Combine(dataDir, "Reminders.json");
        }

        public static List<ReminderItem> LoadReminders()
        {
            lock (_lock)
            {
                try
                {
                    string path = GetFilePath();
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        _reminders = JsonSerializer.Deserialize<List<ReminderItem>>(json) ?? new List<ReminderItem>();
                    }
                    else
                    {
                        _reminders = new List<ReminderItem>();
                    }
                }
                catch
                {
                    _reminders = new List<ReminderItem>();
                }
                return _reminders;
            }
        }

        public static void SaveReminders()
        {
            lock (_lock)
            {
                try
                {
                    string path = GetFilePath();
                    string json = JsonSerializer.Serialize(_reminders, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(path, json);
                }
                catch { }
            }
        }

        public static void AddReminder(string message, DateTime targetTime)
        {
            lock (_lock)
            {
                _reminders.Add(new ReminderItem
                {
                    Message = message,
                    TargetTime = targetTime
                });
                SaveReminders();
            }
        }

        public static bool DeleteReminder(int userIndex)
        {
            lock (_lock)
            {
                var active = _reminders.Where(r => !r.IsCompleted).ToList();
                int idx = userIndex - 1;
                if (idx >= 0 && idx < active.Count)
                {
                    var item = active[idx];
                    _reminders.Remove(item);
                    SaveReminders();
                    return true;
                }
                return false;
            }
        }

        private static void CheckReminders()
        {
            List<ReminderItem> dueReminders = new List<ReminderItem>();

            lock (_lock)
            {
                var now = DateTime.Now;
                foreach (var item in _reminders)
                {
                    if (!item.IsCompleted && item.TargetTime <= now)
                    {
                        item.IsCompleted = true;
                        dueReminders.Add(item);
                    }
                }

                if (dueReminders.Count > 0)
                {
                    SaveReminders();
                }
            }

            foreach (var item in dueReminders)
            {
                // Play notification alert sound
                try
                {
                    SystemSounds.Hand.Play();
                }
                catch { }

                // Speak out loud via TTS!
                try
                {
                    TtsManager.Speak($"Reminder alert: {item.Message}");
                }
                catch { }

                // Display visual alert overlay
                TextOverlay.Show($"🔔 REMINDER ALERT!\n{item.Message}", 6000);
                DebugConsoleOverlay.Log("System", $"Reminder fired: {item.Message}");
            }
        }

        public static List<ReminderItem> GetActiveReminders()
        {
            lock (_lock)
            {
                return _reminders.Where(r => !r.IsCompleted).OrderBy(r => r.TargetTime).ToList();
            }
        }
    }
}
