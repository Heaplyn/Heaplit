// Developer: heaplyn
// Date: 2026-08-13
// Summary: Handles PC power operations (sleep, shutdown, restart, boot to BIOS/UEFI, advanced launch options)
//          with mandatory safety confirmations, voice warnings, and elevated privilege dispatch.

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
            query = query.Trim().ToLowerInvariant();
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string first = parts[0];

            return SearchUtil.IsClose(query, "sleep") || 
                   SearchUtil.IsClose(query, "shutdown") || 
                   SearchUtil.IsClose(query, "rebootpc") ||
                   SearchUtil.IsClose(query, "restartpc") ||
                   SearchUtil.IsClose(query, "bios") ||
                   SearchUtil.IsClose(query, "uefi") ||
                   query.Contains("bios") ||
                   query.Contains("uefi") ||
                   query.Contains("launch option") ||
                   query.Contains("boot option") ||
                   query.Contains("advanced startup") ||
                   query.Contains("startup option") ||
                   query.Contains("recovery") ||
                   query == "turn off computer" || query == "power off" || query == "shut down pc" || query == "restart" || query == "reboot" ||
                   SearchUtil.IsClose(first, "power") ||
                   SearchUtil.IsClose(first, "shutdown") ||
                   SearchUtil.IsClose(first, "restart") ||
                   SearchUtil.IsClose(first, "reboot") ||
                   SearchUtil.IsClose(first, "bios") ||
                   SearchUtil.IsClose(first, "uefi");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLowerInvariant();

            // 1. Boot to BIOS / UEFI Firmware Settings
            if (query.Contains("bios") || query.Contains("uefi") || SearchUtil.IsClose(query, "bios") || SearchUtil.IsClose(query, "uefi"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "⚙️ Boot to BIOS / UEFI Firmware (Requires Confirmation)",
                    DESCRIPTION = "Reboot computer directly into motherboard BIOS / UEFI firmware configuration screen",
                    EXECUTE = () => TriggerBootToBios(),
                    SIMILARITY = 6.5
                });
            }

            // 2. Boot to Windows Advanced Launch Options / Startup Settings
            if (query.Contains("launch") || query.Contains("boot option") || query.Contains("advanced startup") || query.Contains("startup option") || query.Contains("recovery") || query.Contains("bios") || query.Contains("uefi"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🚀 Reboot to Windows Advanced Launch / Boot Options",
                    DESCRIPTION = "Restart into Windows Recovery Environment (Safe Mode, Startup Settings, Troubleshoot, UEFI)",
                    EXECUTE = () => TriggerBootToLaunchOptions(),
                    SIMILARITY = query.Contains("launch") || query.Contains("boot option") ? 6.5 : 5.8
                });
            }

            // 3. Put PC to Sleep
            if (SearchUtil.IsClose(query, "sleep") || query.Contains("sleep") || query.Contains("standby"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "💤 Put PC to Sleep (Requires Confirmation)",
                    DESCRIPTION = "Enter standby/sleep mode (asks for confirmation first)",
                    EXECUTE = () => TriggerPowerState("sleep"),
                    SIMILARITY = SearchUtil.GetSimilarity(query, "sleep")
                });
            }

            // 4. Shut Down Computer
            if (SearchUtil.IsClose(query, "shutdown") || query.Contains("shutdown") || query == "turn off computer" || query == "power off" || query == "shut down pc")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔌 Shut Down Computer (Requires Confirmation)",
                    DESCRIPTION = "Close all apps & turn off the PC (asks for confirmation first)",
                    EXECUTE = () => TriggerPowerState("shutdown"),
                    SIMILARITY = 6.0
                });
            }

            // 5. Restart Computer
            if (SearchUtil.IsClose(query, "rebootpc") || SearchUtil.IsClose(query, "restartpc") || query.Contains("restart") || query.Contains("reboot"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔄 Restart Computer (Requires Confirmation)",
                    DESCRIPTION = "Reboot operating system (asks for confirmation first)",
                    EXECUTE = () => TriggerPowerState("restart"),
                    SIMILARITY = 6.0
                });
            }

            // If query is generic "power", add all options
            if (suggestions.Count == 0 && (query.Contains("power") || SearchUtil.IsClose(query, "power")))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "⚙️ Boot to BIOS / UEFI Firmware",
                    DESCRIPTION = "Reboot computer directly into motherboard BIOS/UEFI setup interface",
                    EXECUTE = () => TriggerBootToBios(),
                    SIMILARITY = 5.5
                });
                suggestions.Add(new CommandResult
                {
                    TITLE = "🚀 Reboot to Advanced Launch / Boot Options",
                    DESCRIPTION = "Restart into Windows Recovery Environment (Safe Mode, Startup Settings)",
                    EXECUTE = () => TriggerBootToLaunchOptions(),
                    SIMILARITY = 5.4
                });
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔄 Restart Computer",
                    DESCRIPTION = "Standard Windows operating system reboot",
                    EXECUTE = () => TriggerPowerState("restart"),
                    SIMILARITY = 5.3
                });
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔌 Shut Down Computer",
                    DESCRIPTION = "Power off the computer",
                    EXECUTE = () => TriggerPowerState("shutdown"),
                    SIMILARITY = 5.2
                });
                suggestions.Add(new CommandResult
                {
                    TITLE = "💤 Put PC to Sleep",
                    DESCRIPTION = "Suspend system to RAM",
                    EXECUTE = () => TriggerPowerState("sleep"),
                    SIMILARITY = 5.1
                });
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc
                {
                    COMMAND_NAME = "bios / uefi / boot to bios / reboot to bios",
                    COMMAND_DESCRIPTION = "Reboots PC directly into motherboard BIOS / UEFI firmware configuration screen",
                    COMMAND_EXAMPLE = "boot to bios"
                },
                new CommandDesc
                {
                    COMMAND_NAME = "launch options / boot options / advanced startup",
                    COMMAND_DESCRIPTION = "Reboots PC into Windows Advanced Launch / Startup Options recovery menu",
                    COMMAND_EXAMPLE = "launch options"
                },
                new CommandDesc
                {
                    COMMAND_NAME = "sleep / shutdown / restart / reboot",
                    COMMAND_DESCRIPTION = "Standard power control operations with safety confirmations",
                    COMMAND_EXAMPLE = "restart"
                }
            };
        }

        public static void TriggerBootToBios()
        {
            try
            {
                string message = "⚠️ Are you sure you want to REBOOT directly into BIOS / UEFI Firmware Settings?";
                TtsManager.Speak("Are you sure you want to reboot into BIOS?");
                TextOverlay.Show("⚠️ Rebooting into BIOS / UEFI Firmware (Click Yes/No)", 4000);

                var result = MessageBox.Show(
                    $"{message}\n\nYour computer will restart immediately and enter the motherboard BIOS/UEFI setup interface.\nAll unsaved work will be lost if you proceed.",
                    "⚠️ Heaplit Power Safety Confirmation - BOOT TO BIOS / UEFI",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No
                );

                if (result != MessageBoxResult.Yes)
                {
                    TextOverlay.Show("❌ Boot to BIOS Cancelled", 2500);
                    TtsManager.Speak("Boot to BIOS cancelled.");
                    return;
                }

                TextOverlay.Show("⚙️ Restarting into BIOS / UEFI Firmware...", 3000);

                // Run shutdown /r /fw /t 1 with elevation
                var psi = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /fw /t 1",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                try
                {
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(3000);
                    if (proc != null && proc.ExitCode != 0)
                    {
                        // /fw might not be supported on legacy BIOS or without UEFI fast-boot flag.
                        // Fall back gracefully to /r /o (Advanced Startup Options)
                        TextOverlay.Show("⚠️ Direct /fw boot unsupported on this motherboard. Falling back to Advanced Startup Options...", 4000);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "shutdown.exe",
                            Arguments = "/r /o /t 1",
                            UseShellExecute = true,
                            Verb = "runas"
                        });
                    }
                }
                catch
                {
                    // Fallback to /r /o
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/r /o /t 1",
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                }
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to trigger BIOS boot: {ex.Message}", 3000);
            }
        }

        public static void TriggerBootToLaunchOptions()
        {
            try
            {
                string message = "⚠️ Are you sure you want to REBOOT into Windows Advanced Launch / Boot Options?";
                TtsManager.Speak("Are you sure you want to reboot into advanced launch options?");
                TextOverlay.Show("⚠️ Rebooting to Advanced Launch Options (Click Yes/No)", 4000);

                var result = MessageBox.Show(
                    $"{message}\n\nYour computer will restart immediately into the Windows Recovery / Startup Settings menu (Troubleshoot, Safe Mode, Startup Settings, UEFI).\nAll unsaved work will be lost if you proceed.",
                    "⚠️ Heaplit Power Safety Confirmation - ADVANCED LAUNCH OPTIONS",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No
                );

                if (result != MessageBoxResult.Yes)
                {
                    TextOverlay.Show("❌ Advanced Launch Options Reboot Cancelled", 2500);
                    TtsManager.Speak("Launch options reboot cancelled.");
                    return;
                }

                TextOverlay.Show("🚀 Restarting into Windows Advanced Launch Options...", 3000);

                var psi = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /o /t 1",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to trigger launch options reboot: {ex.Message}", 3000);
            }
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
