// Developer: heaplyn
// Date: 2026-08-15
// Summary: Live Environmental Audio Analyzer.
//          Analyzes background sounds in real-time using vector categorization.
//          Fires events when significant non-voice sounds are detected.
//          Buffer processing runs on a dedicated background thread via ConcurrentQueue
//          so the audio capture callback thread is never stalled by FFT/MFCC work.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class EnvironmentalAudioAnalyzer
    {
        public static event Action<string, double>? OnSoundDetected;

        private static DateTime _lastDetectionTime = DateTime.MinValue;
        private const int DetectionCooldownMs = 1500;

        // Thread-safe queue: capture callback enqueues, background worker drains
        private static readonly ConcurrentQueue<(byte[] buffer, int length)> _queue = new();
        private static readonly SemaphoreSlim _signal = new(0);

        static EnvironmentalAudioAnalyzer()
        {
            // Single long-running background worker — never competes with UI or capture thread
            Task.Factory.StartNew(DrainLoop, TaskCreationOptions.LongRunning);
        }

        /// <summary>Called from the audio capture callback. Enqueues the buffer and signals the worker.</summary>
        public static void ProcessBuffer(byte[] buffer, int length)
        {
            // Copy the buffer because NAudio reuses it after the callback returns
            var copy = new byte[length];
            Buffer.BlockCopy(buffer, 0, copy, 0, length);
            _queue.Enqueue((copy, length));
            _signal.Release();
        }

        private static void DrainLoop()
        {
            while (true)
            {
                _signal.Wait(); // Block until work is available
                while (_queue.TryDequeue(out var item))
                {
                    try { ProcessBufferInternal(item.buffer, item.length); }
                    catch { /* Never crash the drain loop */ }
                }
            }
        }

        private static void ProcessBufferInternal(byte[] buffer, int length)
        {
            if (((TtsManager)CoreRegistry.Tts).IsSpeakingOrEchoingInternal) return;

            // Extract samples
            int sampleCount = length / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short sample16 = BitConverter.ToInt16(buffer, i * 2);
                samples[i] = sample16 / 32768.0f;
            }

            var features = AudioFeatureExtractor.ExtractFromPcmSamples(samples, 16000);

            // Gate: Only analyze sounds with significant energy
            if (features.RMS_ENERGY > 0.08)
            {
                if ((DateTime.Now - _lastDetectionTime).TotalMilliseconds < DetectionCooldownMs) return;

                var (category, confidence) = SoundVectorManager.ClassifyVector(features.MFCC_COEFFICIENTS);

                if (category != "Ambient" && category != "Unknown")
                {
                    _lastDetectionTime = DateTime.Now;
                    DebugConsoleOverlay.Log("Sound-Analyzer", $"Detected: {category} ({confidence:P0})");
                    OnSoundDetected?.Invoke(category, confidence);

                    // Log to chronology for history understanding
                    ChronoLogManager.LogEvent("Sound", $"Detected {category} (Conf: {confidence:P0})");

                    // Ingest into predictive stream
                    PredictiveStreamManager.IngestEvent("SOUND", $"{category} ({confidence:P0})");
                }
            }
        }

        public static void LearnCurrentSound(string categoryName, byte[] buffer, int length)
        {
            int sampleCount = length / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short sample16 = BitConverter.ToInt16(buffer, i * 2);
                samples[i] = sample16 / 32768.0f;
            }

            var features = AudioFeatureExtractor.ExtractFromPcmSamples(samples, 16000);
            SoundVectorManager.AddFingerprint(categoryName, features.MFCC_COEFFICIENTS);
            DebugConsoleOverlay.Log("Sound-Trainer", $"Learned new fingerprint for: {categoryName}");
        }
    }
}
