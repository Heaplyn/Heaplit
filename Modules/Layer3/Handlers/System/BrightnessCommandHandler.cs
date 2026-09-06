// Developer: heaplyn
// Date: 2026-08-09
// Summary: Adjusts monitor brightness using PowerShell WMI methods, showing visual feedback on completion.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class BrightnessCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string cmd = parts[0];
            return SearchUtil.IsClose(cmd, "brightness") || SearchUtil.IsClose(cmd, "bright");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0];
            double similarity = SearchUtil.GetSimilarity(cmd, "brightness");

            if (parts.Length > 1 && int.TryParse(parts[1], out int brightnessValue))
            {
                brightnessValue = Math.Clamp(brightnessValue, 0, 100);
                suggestions.Add(new CommandResult
                {
                    TITLE = $"Set Screen Brightness to {brightnessValue}%",
                    DESCRIPTION = "Adjust monitor backlight level",
                    EXECUTE = () => SetScreenBrightness(brightnessValue),
                    SIMILARITY = similarity
                });
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "Set Brightness...",
                    DESCRIPTION = "Type a percentage (e.g. 'brightness 75')",
                    EXECUTE = null,
                    SIMILARITY = similarity
                });
            }

            return suggestions;
        }

        private static void SetScreenBrightness(int percentage)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"(Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightnessMethods).WmiSetBrightness(1, {percentage})\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                System.Diagnostics.Process.Start(psi);
                TextOverlay.Show($"☀️ Screen Brightness set to {percentage}%", 2000);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Brightness Error: {ex.Message}", 3000);
            }
        }
    }
}
