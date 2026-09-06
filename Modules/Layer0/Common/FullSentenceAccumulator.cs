// Developer: heaplyn
// Date: 2026-08-13
// Summary: Full-Sentence Speech Accumulator & Silence Detector Engine.
// Buffers streaming voice tokens until the user completely finishes speaking, then executes the statement.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public static class FullSentenceAccumulator
    {
        public static event Action<string>? OnFullSentenceCompleted;

        private static readonly StringBuilder _sentenceBuffer = new();
        private static System.Threading.Timer? _silenceTimer;
        private static readonly object _lock = new();
        private static DateTime _lastSpeechTime = DateTime.MinValue;

        // 3.0 seconds (3000ms) of audio silence required for regular speech to ensure user is fully finished.
        private static int SilencePauseMs => Math.Max(1000, SettingsManager.Current.VOICE_CHUNKING_SILENCE_MS > 0 ? SettingsManager.Current.VOICE_CHUNKING_SILENCE_MS : 3000);

        static FullSentenceAccumulator()
        {
            _silenceTimer = new System.Threading.Timer(OnSilenceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Appends a new voice token into the full-sentence buffer and resets silence timer.
        /// </summary>
        public static void AppendSpeechToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            // Acoustic Echo Suppression
            if (((TtsManager)CoreRegistry.Interaction.Tts).IsSpeakingOrEchoingInternal) return;

            lock (_lock)
            {
                string cleanToken = token.Trim();

                // Avoid duplicate consecutive tokens
                string currentStr = _sentenceBuffer.ToString().Trim();
                if (!currentStr.EndsWith(cleanToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (_sentenceBuffer.Length > 0) _sentenceBuffer.Append(" ");
                    _sentenceBuffer.Append(cleanToken);
                }

                _lastSpeechTime = DateTime.Now;

                string fullTextSoFar = _sentenceBuffer.ToString().Trim();
                DebugConsoleOverlay.Log("Sentence Accumulator", $"Listening... \"{fullTextSoFar}\"");

                // Dynamic Silence Gate: If sentence contains a recognized action command keyword,
                // trigger query processing after 1.5 seconds of silence instead of waiting full 6 seconds.
                int activeSilenceMs = SilencePauseMs;
                string lowerText = fullTextSoFar.ToLower();
                if (lowerText.Contains("open") || lowerText.Contains("weather") || lowerText.Contains("shutdown") ||
                    lowerText.Contains("lock") || lowerText.Contains("mcp") || lowerText.Contains("google") ||
                    lowerText.Contains("search") || lowerText.Contains("convert") || lowerText.Contains("code assist") ||
                    lowerText.Contains("turn on") || lowerText.Contains("turn off") || lowerText.Contains("exit") ||
                    lowerText.Contains("voicemode"))
                {
                    activeSilenceMs = 1500;
                }

                // Reset silence countdown timer (waits until user finishes speaking completely)
                _silenceTimer?.Change(activeSilenceMs, Timeout.Infinite);
            }
        }

        private static void OnSilenceTimerElapsed(object? state)
        {
            string completedSentence = string.Empty;

            lock (_lock)
            {
                if (_sentenceBuffer.Length == 0) return;

                completedSentence = _sentenceBuffer.ToString().Trim();
                _sentenceBuffer.Clear();
            }

            if (!string.IsNullOrWhiteSpace(completedSentence))
            {
                System.Diagnostics.Debug.WriteLine($"✅ User Finished Speaking ({completedSentence.Length} chars): \"{completedSentence}\"");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnFullSentenceCompleted?.Invoke(completedSentence);
                });
            }
        }

        /// <summary>
        /// Forces immediate submission of the accumulated sentence without waiting for silence timer.
        /// </summary>
        public static void FlushNow()
        {
            OnSilenceTimerElapsed(null);
        }

        /// <summary>
        /// Clears current buffered speech.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _sentenceBuffer.Clear();
                _silenceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }
}
