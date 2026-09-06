// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to toggle mute state of the system audio devices via Win32 key injection.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HeaplitLauncher
{
    public class MuteCommandHandler : ICommandHandler
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "mute" || query == "unmute" || query == "togglemute" || query == "sound mute";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = SearchUtil.GetSimilarity(query, "mute");

            suggestions.Add(new CommandResult
            {
                TITLE       = "Toggle Audio Mute",
                DESCRIPTION = "Fast toggle mute/unmute state of system sound device",
                SIMILARITY  = similarity + 0.5,
                EXECUTE     = () => ToggleMute()
            });

            return suggestions;
        }

        private static void ToggleMute()
        {
            try
            {
                const byte VK_VOLUME_MUTE = 0xAD;
                keybd_event(VK_VOLUME_MUTE, 0, 0, 0); // Key Down
                keybd_event(VK_VOLUME_MUTE, 0, 2, 0); // Key Up
                TextOverlay.Show("🔇 System Audio Mute Toggled", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Mute toggle failed: {ex.Message}", 3000);
            }
        }
    }
}
