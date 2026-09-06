// Developer: heaplyn
// Date: 2026-08-17
// Summary: High-performance Text-to-Speech service implementation.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.IO;

namespace HeaplitLauncher
{
    public class TtsManager : ITtsService
    {
        public static event Action? OnSpeechStopped;

        private readonly SpeechSynthesizer _synthesizer = new SpeechSynthesizer();
        private MediaPlayer? _customAudioPlayer;
        private bool _isSpeaking;
        private DateTime _echoCooldownUntil = DateTime.MinValue;

        bool ITtsService.IsSpeaking => _isSpeaking;
        public bool IsSpeakingOrEchoingInternal => _isSpeaking || DateTime.Now < _echoCooldownUntil;

        public TtsManager()
        {
            ApplyCurrentSettings();
            _synthesizer.SpeakStarted += (s, e) => _isSpeaking = true;
            _synthesizer.SpeakCompleted += (s, e) =>
            {
                _isSpeaking = false;
                _echoCooldownUntil = DateTime.Now.AddMilliseconds(25);
                OnSpeechStopped?.Invoke();
            };
        }

        public void ApplyCurrentSettings()
        {
            var s = CoreRegistry.Data.Settings.Current;
            try { if (!string.IsNullOrWhiteSpace(s.SELECTED_TTS_VOICE)) _synthesizer.SelectVoice(s.SELECTED_TTS_VOICE); } catch { }
            _synthesizer.Rate = Math.Clamp(s.TTS_SPEECH_RATE, -10, 10);
            _synthesizer.Volume = Math.Clamp(s.TTS_SPEECH_VOLUME, 0, 100);
        }

        void ITtsService.Speak(string text) => SpeakInternal(text);

        private void SpeakInternal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var s = CoreRegistry.Data.Settings.Current;
            _isSpeaking = true;
            _echoCooldownUntil = DateTime.Now.AddMilliseconds(25);

            if (s.USE_CUSTOM_TTS_SOUND_FILE && !string.IsNullOrEmpty(s.CUSTOM_TTS_SAMPLE_PATH))
            {
                if (Directory.Exists(s.CUSTOM_TTS_SAMPLE_PATH))
                {
                    string match = FindLocalVoiceMatch(text, s.CUSTOM_TTS_SAMPLE_PATH);
                    if (!string.IsNullOrEmpty(match)) { PlayCustomAudioInternal(match); return; }
                }
                else if (File.Exists(s.CUSTOM_TTS_SAMPLE_PATH))
                {
                    PlayCustomAudioInternal(s.CUSTOM_TTS_SAMPLE_PATH);
                    if (s.CUSTOM_SOUND_ONLY) return;
                }
            }

            Task.Run(() =>
            {
                try {
                    _synthesizer.SpeakAsyncCancelAll();
                    string cleanText = PrepareSpeechText(text);
                    if (!string.IsNullOrWhiteSpace(cleanText)) { ApplyCurrentSettings(); _synthesizer.SpeakAsync(cleanText); }
                } catch { }
            });
        }

        void ITtsService.Stop()
        {
            _synthesizer.SpeakAsyncCancelAll();
            System.Windows.Application.Current.Dispatcher.Invoke(() => _customAudioPlayer?.Stop());
            _isSpeaking = false;
            OnSpeechStopped?.Invoke();
        }

        private void PlayCustomAudioInternal(string path)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _customAudioPlayer?.Stop();
                _customAudioPlayer = new MediaPlayer();
                _customAudioPlayer.Open(new Uri(path, UriKind.Absolute));
                _customAudioPlayer.Volume = _synthesizer.Volume / 100.0;
                _customAudioPlayer.Play();
                _isSpeaking = true;
                _customAudioPlayer.MediaEnded += (s, e) => { _isSpeaking = false; OnSpeechStopped?.Invoke(); };
            });
        }

        private string PrepareSpeechText(string text)
        {
            string cleaned = Regex.Replace(text, @"```[\s\S]*?```", "");
            cleaned = Regex.Replace(cleaned, @"\[.*?\]", "");
            cleaned = Regex.Replace(cleaned, @"[*_`#~]", "");
            return cleaned.Trim();
        }

        private string FindLocalVoiceMatch(string text, string dir)
        {
            string lower = text.ToLowerInvariant();
            if (lower.Contains("yes")) return GetFile(dir, "yes");
            if (lower.Contains("no")) return GetFile(dir, "no");
            return "";
        }

        private string GetFile(string dir, string prefix)
        {
            try {
                var files = Directory.GetFiles(dir, prefix + "*.*");
                if (files.Length > 0) return files[new Random().Next(files.Length)];
            } catch { }
            return "";
        }

        // --- STATIC BRIDGES ---
        public static void Speak(string text) => CoreRegistry.Interaction.Tts.Speak(text);
        public static void Stop() => CoreRegistry.Interaction.Tts.Stop();
        public static bool IsSpeaking => CoreRegistry.Interaction.Tts.IsSpeaking;
        public static bool IsSpeakingOrEchoing => ((TtsManager)CoreRegistry.Interaction.Tts).IsSpeakingOrEchoingInternal;

        public static void SpeakFile(string path) { if (File.Exists(path)) Speak(File.ReadAllText(path)); }
        public static List<string> GetInstalledVoices() => ((TtsManager)CoreRegistry.Interaction.Tts).GetVoicesInternal();
        public static void SetVoice(string v) => ((TtsManager)CoreRegistry.Interaction.Tts).SetVoiceInternal(v);
        public static void SetRate(int r) => ((TtsManager)CoreRegistry.Interaction.Tts).SetRateInternal(r);
        public static void SetVolume(int v) => ((TtsManager)CoreRegistry.Interaction.Tts).SetVolumeInternal(v);

        public List<string> GetVoicesInternal() {
            var v = new List<string>();
            foreach (InstalledVoice iv in _synthesizer.GetInstalledVoices()) if (iv.Enabled) v.Add(iv.VoiceInfo.Name);
            return v;
        }
        public void SetVoiceInternal(string v) { try { _synthesizer.SelectVoice(v); } catch {} }
        public void SetRateInternal(int r) { _synthesizer.Rate = r; }
        public void SetVolumeInternal(int v) { _synthesizer.Volume = v; }
    }
}
