// Developer: heaplyn
// Date: 2026-08-13
// Summary: Command handler for Voice AI Studio, Offline Pre-Caching Studio, GitHub Custom TTS Voice Library, & Vosk model downloader.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class VoiceStudioCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.ToLower().Trim();
            return query == "voice" || query == "voicestudio" || query == "voicetrainer" ||
                   query == "record" || query == "audiorecorder" || query == "voicememo" ||
                   query == "speechcalibrate" || query == "voicetrain" || query == "speechtraining" ||
                   query == "downloadvosk" || query == "downloadmodel" || query == "voskmodel" ||
                   query == "ttsvoices" || query == "customvoice" || query == "ttssamples" || query == "ttsvoice" ||
                   query == "offline" || query == "offlinemode" || query == "precache" || query == "cacheoffline" ||
                   query == "disable voice" || query == "enable voice" || query == "voicemode off" || query == "voicemode on" || query == "toggle voice" ||
                   query == "voice dataset" || query == "voice classification" || query == "classify voice" || query == "teleprompter" ||
                   query.StartsWith("voice ") || query.StartsWith("record ");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var results = new List<CommandResult>();
            string lower = query.ToLower().Trim();

            // Voice Mode Toggle Commands
            if (lower == "disable voice" || lower == "voicemode off" || lower == "stop voice" || lower == "turn off voice mode")
            {
                results.Add(new CommandResult
                {
                    TITLE = "🔇 Disable Voice Interaction Mode",
                    DESCRIPTION = "Stops Heaplit from responding to conversations, but keeps wake-word listening active for reactivation.",
                    SIMILARITY = 6.0,
                    EXECUTE = () =>
                    {
                        SettingsManager.Current.IS_VOICE_MODE_ACTIVE = false;
                        SettingsManager.Save();
                        TtsManager.Speak("Voice mode disabled.");
                        TextOverlay.Show("🔇 Voice Mode: OFF", 3000);
                    }
                });
                return results;
            }

            if (lower == "enable voice" || lower == "voicemode on" || lower == "start voice" || lower == "turn on voice mode")
            {
                results.Add(new CommandResult
                {
                    TITLE = "🎙️ Enable Voice Interaction Mode",
                    DESCRIPTION = "Resumes full voice conversation and system command execution.",
                    SIMILARITY = 6.0,
                    EXECUTE = () =>
                    {
                        SettingsManager.Current.IS_VOICE_MODE_ACTIVE = true;
                        SettingsManager.Save();
                        LocalWakeWordDetector.Initialize(); // Ensure initialized
                        TtsManager.Speak("Voice mode enabled.");
                        TextOverlay.Show("🎙️ Voice Mode: ON", 3000);
                    }
                });
                return results;
            }

            if (lower.Contains("dataset") || lower.Contains("classification") || lower.Contains("classify"))
            {
                results.Add(new CommandResult
                {
                    TITLE = "🏷️ Open Voice Dataset & Classification Studio",
                    DESCRIPTION = "View, play, tag (Command, AI Chat, Wake Word, Noise), & train acoustic voice dataset",
                    SIMILARITY = 6.0,
                    EXECUTE = () => VoiceStudioOverlay.ShowOverlay()
                });
                return results;
            }

            if (lower == "offline" || lower == "offlinemode" || lower == "precache" || lower == "cacheoffline")
            {
                results.Add(new CommandResult
                {
                    TITLE = "📶 Open Offline Mode & Wi-Fi Pre-Caching Studio",
                    DESCRIPTION = "Pre-download speech models, TTS voices, & local LLM models for 100% offline functionality",
                    SIMILARITY = 6.0,
                    EXECUTE = () => OfflineStudioOverlay.ShowOverlay()
                });
                return results;
            }

            if (lower == "ttsvoices" || lower == "customvoice" || lower == "ttssamples" || lower == "ttsvoice")
            {
                results.Add(new CommandResult
                {
                    TITLE = "🌐 Open GitHub Custom TTS Voice Library (yaph/tts-samples)",
                    DESCRIPTION = "Browse, preview, & set custom TTS voice MP3 samples directly from GitHub",
                    SIMILARITY = 6.0,
                    EXECUTE = () => TtsVoiceLibraryOverlay.ShowOverlay()
                });
                return results;
            }

            if (lower == "downloadvosk" || lower == "downloadmodel" || lower == "voskmodel")
            {
                results.Add(new CommandResult
                {
                    TITLE = "📥 Download Official Vosk Neural Speech Model (~40MB)",
                    DESCRIPTION = "Auto-downloads and installs full offline neural speech recognition model for 99%+ accuracy",
                    SIMILARITY = 6.0,
                    EXECUTE = () => Task.Run(async () => await VoskEngine.EnsureModelDownloadedAsync(showToast: true))
                });
                return results;
            }

            results.Add(new CommandResult
            {
                TITLE = "📶 Open Offline Mode & Pre-Caching Studio",
                DESCRIPTION = "Pre-cache speech, TTS, & local LLM features for 100% offline usage",
                SIMILARITY = 5.6,
                EXECUTE = () => OfflineStudioOverlay.ShowOverlay()
            });

            results.Add(new CommandResult
            {
                TITLE = "🎙️ Open Voice AI Studio & Audio Recorder",
                DESCRIPTION = "Train AI voice profiles, record audio memos, calibrate speech sensitivity, & map voice shortcuts",
                SIMILARITY = 5.5,
                EXECUTE = () => VoiceStudioOverlay.ShowOverlay()
            });

            return results;
        }
    }
}
