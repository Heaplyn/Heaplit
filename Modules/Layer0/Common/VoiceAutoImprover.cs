// Developer: heaplyn
// Date: 2026-08-14
// Summary: Background Voice Recognition Auto-Improver Engine.
//          Analyzes captured voice clips, auto-learns alternative pronunciations/phrases,
//          and rebuilds local acoustic classifiers to dynamically improve offline accuracy.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class VoiceAutoImprover
    {
        private static bool _isRunning = false;
        private static int _processedClipCount = 0;

        public static void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            Task.Run(async () =>
            {
                // Wait 15 seconds after app launch before first check
                await Task.Delay(15000);

                while (_isRunning)
                {
                    try
                    {
                        if (SettingsManager.Current.IS_VOICE_MODE_ACTIVE && SettingsManager.Current.IS_HEAPLIT_ENABLED)
                        {
                            await RunAutoImproverAuditAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsoleOverlay.Log("AutoImprover Error", ex.Message);
                    }

                    // Run the audit every 15 minutes
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromMinutes(15));
                }
            });

            DebugConsoleOverlay.Log("VoiceAutoImprover", "Background voice recognition auto-improver loop active.");
        }

        public static void Stop()
        {
            _isRunning = false;
        }

        private static async Task RunAutoImproverAuditAsync()
        {
            // Load latest records from Dataset manager
            VoiceDatasetManager.LoadMetadata();
            var records = VoiceDatasetManager.DatasetRecords;
            
            if (records.Count <= _processedClipCount)
            {
                return; // No new clips to analyze
            }

            DebugConsoleOverlay.Log("VoiceAutoImprover", $"New voice logs detected ({records.Count - _processedClipCount} new). Analyzing audio features...");

            int learnedCount = 0;

            // Iterate new records
            for (int i = _processedClipCount; i < records.Count; i++)
            {
                var record = records[i];
                if (record == null || string.IsNullOrEmpty(record.Transcript)) continue;

                string t = record.Transcript.Trim();
                
                // 1. If it was a successful command transcript, learn it!
                // This updates SAPI's vocabulary dictionary automatically in the background
                if (record.Classification == "Command" && t.Length > 2 && t.Length < 35 && !t.Contains("..."))
                {
                    // Call VoiceActivationManager to add it to SAPI commands
                    VoiceActivationManager.LearnPhraseGlobal(t);
                    learnedCount++;
                }

                // 2. If it's a wake phrase that was processed through Gemini fallback,
                // teach SAPI the variant
                if (record.Classification == "Wake Word" && !t.Equals("Heaplit", StringComparison.OrdinalIgnoreCase))
                {
                    VoiceActivationManager.LearnPhraseGlobal(t);
                    learnedCount++;
                }
            }

            // Update process counter
            _processedClipCount = records.Count;

            // 3. Trigger classifier rebuild to incorporate the new audio vectors into the ML index
            string trainingMsg = VoiceDatasetManager.TrainClassifierModel();
            DebugConsoleOverlay.Log("VoiceAutoImprover", $"Classifier training completed in background. {learnedCount} new phrases learned.");

            if (learnedCount > 0)
            {
                // Notify user on debug panel
                DebugConsoleOverlay.Log("VoiceAutoImprover", $"Successfully optimized voice recognition. Added {learnedCount} phonetic variant phrases.");
            }

            await Task.CompletedTask;
        }
    }
}
