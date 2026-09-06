// Developer: heaplyn
// Date: 2026-08-17
// Summary: Voice Activation and Wake Word Detection Service implementation.
//          Uses explicit interface implementation to prevent naming collisions.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Threading;
using System.Text.RegularExpressions;

namespace HeaplitLauncher
{
    public class VoiceActivationManager : IVoiceActivationService
    {
        bool IVoiceActivationService.IsListening => LocalWakeWordDetector.IsListening;

        // Boot calls this via CoreRegistry.Interaction.Voice.Start(). Rather than buffer raw mic
        // audio into a stream nobody reads (the old stub), drive the real "Hey Heaplit" wake-word
        // engine so voice activation actually works. Gated by ENABLE_WAKE_WORD.
        void IVoiceActivationService.Start()
        {
            try {
                if (!SettingsManager.Current.ENABLE_WAKE_WORD)
                {
                    DebugConsoleOverlay.Log("Voice", "Wake word disabled (ENABLE_WAKE_WORD = false). Say-\"Hey Heaplit\" listening not started.");
                    return;
                }
                LocalWakeWordDetector.Initialize();
                DebugConsoleOverlay.Log("Voice", "Wake-word engine online — listening for \"Hey Heaplit\".");
            } catch { }
        }

        void IVoiceActivationService.Stop() => LocalWakeWordDetector.Stop();
        void IVoiceActivationService.SetSensitivity(double level) { }

        Task IVoiceActivationService.EnrollVoiceAsync(string name) => Task.CompletedTask;
        Task IVoiceActivationService.LearnEnvironmentalSoundAsync(string category) => Task.CompletedTask;
        Task IVoiceActivationService.SaveBackgroundAudioTokenAsync(string text) => Task.CompletedTask;
        void IVoiceActivationService.LearnPhrase(string phrase) { }

        // --- STATIC LEGACY BRIDGES (CRITICAL FOR BUILD) ---
        public static void Start() => CoreRegistry.Interaction.Voice.Start();
        public static void Stop() => CoreRegistry.Interaction.Voice.Stop();
        public static void LearnPhrase(string phrase) => CoreRegistry.Interaction.Voice.LearnPhrase(phrase);
        public static void LearnPhraseGlobal(string phrase) => LearnPhrase(phrase);
        public static Task EnrollVoiceAsync(string name) => CoreRegistry.Interaction.Voice.EnrollVoiceAsync(name);
        public static Task EnrollVoiceGlobalAsync(string name) => EnrollVoiceAsync(name);
        public static Task LearnEnvironmentalSoundAsync(string category) => CoreRegistry.Interaction.Voice.LearnEnvironmentalSoundAsync(category);
        public static Task LearnSoundGlobalAsync(string category) => LearnEnvironmentalSoundAsync(category);
        public static Task SaveBackgroundAudioTokenAsync(string text) => CoreRegistry.Interaction.Voice.SaveBackgroundAudioTokenAsync(text);
        public static Task SaveAudioTokenGlobalAsync(string text) => SaveBackgroundAudioTokenAsync(text);
    }
}
