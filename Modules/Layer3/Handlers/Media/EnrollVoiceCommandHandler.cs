// Developer: heaplyn
// Date: 2026-08-14
// Summary: Handles commands to enroll or train speaker verification voiceprints.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class EnrollVoiceCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "enroll voice", "biometric enroll", "voice enrollment", "train voice");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim();
            
            // Extract target speaker name
            string name = "Owner";
            string[] parts = q.Split(' ');
            if (parts.Length > 2)
            {
                name = parts[parts.Length - 1];
            }

            suggestions.Add(new CommandResult
            {
                TITLE = $"🎙️ Enroll Voiceprint for '{name}'",
                DESCRIPTION = "Train speaker verification biometrics to secure voice activation",
                EXECUTE = () =>
                {
                    Task.Run(async () => await VoiceActivationManager.EnrollVoiceGlobalAsync(name));
                },
                SIMILARITY = (SearchUtil.BestSimilarity(query, "enroll voice", "biometric enroll", "voice enrollment", "train voice") + 8.5 * 0.01)
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "🛡️ Enable Speaker Voice Verification",
                DESCRIPTION = "GATES voice activations: only runs commands if speaker biometrics match Kyle",
                EXECUTE = () =>
                {
                    SettingsManager.Current.IS_SPEAKER_VERIFICATION_ENABLED = true;
                    SettingsManager.Save();
                    TextOverlay.Show("✅ Speaker Verification Enabled!", 3000);
                    TtsManager.Speak("Speaker verification is now active. Only enrolled users may trigger commands.");
                },
                SIMILARITY = q.ToLower().Contains("enable") ? 8.0 : 4.0
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "🔓 Disable Speaker Voice Verification",
                DESCRIPTION = "ALLOWS any speaker to trigger voice commands (disabled owner gate)",
                EXECUTE = () =>
                {
                    SettingsManager.Current.IS_SPEAKER_VERIFICATION_ENABLED = false;
                    SettingsManager.Save();
                    TextOverlay.Show("🔓 Speaker Verification Disabled", 3000);
                    TtsManager.Speak("Speaker verification has been disabled. Anyone can now control the HUD.");
                },
                SIMILARITY = q.ToLower().Contains("disable") ? 8.0 : 4.0
            });

            return suggestions;
        }
    }
}
