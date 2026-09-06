using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher
{
    public class TtsCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "tts", "speak", "say", "read", "stop tts", "ttsvoices", "voices");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            if (query == "ttsvoices" || query == "voices")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🗣️ TTS Voice Studio",
                    DESCRIPTION = "Manage system voices and custom audio files",
                    EXECUTE = () => TtsVoiceLibraryOverlay.ShowOverlay(),
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "tts", "speak", "say", "read", "stop tts", "ttsvoices", "voices") + 5.0 * 0.01)
                });
                return suggestions;
            }

            var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();
            string arg = parts.Length > 1 ? parts[1].Trim() : "";

            if (cmd == "stop")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🔇 Stop Heaplit Speech",
                    DESCRIPTION = "Instantly cancel all active TTS output",
                    EXECUTE = () => TtsManager.Stop(),
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "tts", "speak", "say", "read", "stop tts", "ttsvoices", "voices") + 5.0 * 0.01)
                });
                return suggestions;
            }

            if (cmd == "read" && File.Exists(arg))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🔊 Read File: {Path.GetFileName(arg)}",
                    DESCRIPTION = $"Convert text in {arg} to speech",
                    EXECUTE = () => TtsManager.Speak(File.ReadAllText(arg)),
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "tts", "speak", "say", "read", "stop tts", "ttsvoices", "voices") + 4.5 * 0.01)
                });
            }

            if (!string.IsNullOrEmpty(arg))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🗣️ Say: \"{arg}\"",
                    DESCRIPTION = "Play text through the active TTS engine",
                    EXECUTE = () => TtsManager.Speak(arg),
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "tts", "speak", "say", "read", "stop tts", "ttsvoices", "voices") + 4.0 * 0.01)
                });
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("tts <text>", "Speak any text out loud", "tts System online"),
                new CommandDesc("read <path>", "Read a text file out loud", "read notes.txt"),
                new CommandDesc("stop tts", "Cancel current speech output", "stop tts")
            };
        }
    }
}
