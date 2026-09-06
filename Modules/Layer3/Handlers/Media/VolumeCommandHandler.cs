// Developer: heaplyn
// Date: 2026-08-08
// Summary: Handles audio commands to change system master volume levels or toggle mute using NAudio API calls.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace HeaplitLauncher
{
    public class VolumeCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string cmd = parts[0];
            return SearchUtil.IsClose(cmd, "volume") ||
                   SearchUtil.IsClose(cmd, "vol") ||
                   SearchUtil.IsClose(cmd, "mute");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0];

            double similarity = Math.Max(
                SearchUtil.GetSimilarity(cmd, "volume"),
                Math.Max(SearchUtil.GetSimilarity(cmd, "vol"), SearchUtil.GetSimilarity(cmd, "mute"))
            );

            if (SearchUtil.IsClose(cmd, "volume") || SearchUtil.IsClose(cmd, "vol"))
            {
                if (parts.Length > 1 && int.TryParse(parts[1], out int targetVolume))
                {
                    targetVolume = Math.Clamp(targetVolume, 0, 100);
                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"Set Volume to {targetVolume}%",
                        DESCRIPTION = $"Adjust system volume level",
                        EXECUTE = () => SetSystemVolume(targetVolume),
                        SIMILARITY = similarity
                    });
                }
            }
            else if (SearchUtil.IsClose(cmd, "mute"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "Toggle Mute Status",
                    DESCRIPTION = "Mute/unmute global system volume",
                    EXECUTE = () => ToggleSystemMute(),
                    SIMILARITY = similarity
                });
            }

            return suggestions;
        }

        private static void SetSystemVolume(int level)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = level / 100f;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set system volume: {ex.Message}");
            }
        }

        private static void ToggleSystemMute()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to toggle system mute: {ex.Message}");
            }
        }
    }
}
