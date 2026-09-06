// Developer: heaplyn
// Date: 2026-08-13
// Summary: Handles PC power operations (sleep, shutdown, restart) with mandatory confirmation prompt and safety check.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace HeaplitLauncher
{
    public class PowerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return SearchUtil.IsClose(query, "sleep") || 
                   SearchUtil.IsClose(query, "shutdown") || 
                   SearchUtil.IsClose(query, "rebootpc") ||
                   SearchUtil.IsClose(query, "restartpc") ||
                   query == "turn off computer" || query == "power off" || query == "shut down pc";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            if (SearchUtil.IsClose(query, "sleep"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "💤 Put PC to Sleep (Requires Confirmation)",
                    DESCRIPTION = "Enter standby/sleep mode (asks for confirmation first)",
                    EXECUTE = () => TriggerPowerState("sleep"),
                    SIMILARITY = SearchUtil.GetSimilarity(query, "sleep")
                });
            }
            else if (SearchUtil.IsClose(query, "shutdown") || query == "turn off computer" || query == "power off" || query == "shut down pc")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔌 Shut Down Computer (Requires Confirmation)",
                    DESCRIPTION = "Close all apps & turn off the PC (asks for confirmation first)",
                    EXECUTE = () => TriggerPowerState("shutdown"),
                    SIMILARITY = 6.0
                });
            }
            else if (SearchUtil.IsClose(query, "rebootpc") || SearchUtil.IsClose(query, "restartpc") || query == "restart")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔄 Restart Computer (Requires Confirmation)",
                    DESCRIPTION = "Reboot operating system (asks for confirmation first)",
                    EXECUTE = () => TriggerPowerState("restart"),
                    SIMILARITY = 6.0
                });
            }

            return suggestions;
        }

        private static void TriggerPowerState(string state)
        {
            try
            {
                string actionName = state == "shutdown" ? "SHUT DOWN" : (state == "restart" ? "RESTART" : "put to SLEEP");
                string message = $"⚠️ Are you sure you want to {actionName} your computer?";

                TtsManager.Speak($"Are you sure you want to {actionName.ToLower()} your computer?");
                TextOverlay.Show($"⚠️ {message} (Click Yes/No)", 4000);

                var result = MessageBox.Show(
                    $"{message}\n\nAll unsaved work will be lost if you proceed.",
                    $"⚠️ Heaplit Power Safety Confirmation - {state.ToUpper()}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No
                );

                if (result != MessageBoxResult.Yes)
                {
                    TextOverlay.Show("❌ Power Action Cancelled", 2500);
                    TtsManager.Speak("Power action cancelled.");
                    return;
                }

                if (state == "sleep")
                {
                    TextOverlay.Show("💤 Putting PC to sleep...", 2000);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "rundll32.exe",
                        Arguments = "powrprof.dll,SetSuspendState 0,1,0",
                        UseShellExecute = true
                    });
                }
                else if (state == "shutdown")
                {
                    TextOverlay.Show("🔌 Shutting down system...", 2000);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/s /t 0",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else if (state == "restart")
                {
                    TextOverlay.Show("🔄 Restarting system...", 2000);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/r /t 0",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to trigger power command: {ex.Message}", 3000);
            }
        }
    }
}
