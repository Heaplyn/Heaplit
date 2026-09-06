// Developer: heaplyn
// Date: 2026-08-13
// Summary: Machine Learning Acoustic Sound Classifier matching live mic audio against trained Voice Profile MFCC vectors.

using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher
{
    public class AcousticMatchResult
    {
        public bool IS_MATCHED { get; set; } = false;
        public string MATCHED_PHRASE { get; set; } = string.Empty;
        public double CONFIDENCE { get; set; } = 0.0; // 0.0 to 1.0 (0% to 100%)
        public VoiceSample? BEST_SAMPLE { get; set; }
    }

    public static class AcousticMlClassifier
    {
        private static readonly Dictionary<string, double[]> CachedProfileMfccs = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Re-builds in-memory MFCC acoustic feature vectors from all recorded samples in Voice Profile.
        /// </summary>
        public static void RebuildAcousticIndex()
        {
            CachedProfileMfccs.Clear();

            // 1. Load official samples from VoiceTrainerManager (Golden Set)
            var ProfileSamples = VoiceTrainerManager.Profile.SAMPLES;
            foreach (var Sample in ProfileSamples)
            {
                if (File.Exists(Sample.AUDIO_FILE_PATH))
                {
                    var Features = AudioFeatureExtractor.ExtractFromFile(Sample.AUDIO_FILE_PATH);
                    if (Features != null && Features.MFCC_COEFFICIENTS != null)
                    {
                        string Key = $"TRAINER:{Sample.ID}:{Sample.PHRASE}";
                        CachedProfileMfccs[Key] = Features.MFCC_COEFFICIENTS;
                    }
                }
            }

            // 2. Load historical logs from VoiceDatasetManager (Self-Learning Set)
            var DatasetRecords = VoiceDatasetManager.DatasetRecords;
            foreach (var Rec in DatasetRecords)
            {
                // ONLY index very short snippets (1-2 words) that are likely to be wake words
                if (File.Exists(Rec.FilePath) && !string.IsNullOrWhiteSpace(Rec.Transcript) &&
                    Rec.Transcript != "..." && Rec.Transcript.Split(' ').Length <= 2)
                {
                    // Use filename hash as a pseudo-id for uniqueness in the index
                    var Features = AudioFeatureExtractor.ExtractFromFile(Rec.FilePath);
                    if (Features != null && Features.MFCC_COEFFICIENTS != null)
                    {
                        string PseudoId = Rec.FileName.GetHashCode().ToString("X");
                        string Key = $"DATASET:{PseudoId}:{Rec.Transcript}";
                        CachedProfileMfccs[Key] = Features.MFCC_COEFFICIENTS;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"🧠 Rebuilt Acoustic ML Index with {CachedProfileMfccs.Count} feature vectors (Golden + Historical).");
        }

        /// <summary>
        /// Classifies an incoming WAV audio file against the trained acoustic voice profile using MFCC Cosine Distance.
        /// </summary>
        public static AcousticMatchResult MatchWavFile(string WavFilePath, double Threshold = 0.70)
        {
            var Result = new AcousticMatchResult();
            if (!File.Exists(WavFilePath)) return Result;

            if (CachedProfileMfccs.Count == 0)
            {
                RebuildAcousticIndex();
            }

            if (CachedProfileMfccs.Count == 0) return Result;

            var InputFeatures = AudioFeatureExtractor.ExtractFromFile(WavFilePath);
            if (InputFeatures == null || InputFeatures.MFCC_COEFFICIENTS == null) return Result;

            double MaxSimilarity = 0.0;
            string BestPhrase = string.Empty;
            VoiceSample? BestSample = null;

            foreach (var Kvp in CachedProfileMfccs)
            {
                double Similarity = AudioFeatureExtractor.CosineSimilarity(InputFeatures.MFCC_COEFFICIENTS, Kvp.Value);
                if (Similarity > MaxSimilarity)
                {
                    MaxSimilarity = Similarity;
                    string[] Parts = Kvp.Key.Split(':');
                    // Key format: SOURCE:ID:PHRASE
                    BestPhrase = Parts.Length > 2 ? Parts[2] : (Parts.Length > 1 ? Parts[1] : string.Empty);

                    if (Parts[0] == "TRAINER")
                    {
                        string SampleId = Parts[1];
                        BestSample = VoiceTrainerManager.Profile.SAMPLES.Find(s => s.ID == SampleId);
                    }
                }
            }

            Result.CONFIDENCE = Math.Round(MaxSimilarity, 3);
            Result.MATCHED_PHRASE = BestPhrase;
            Result.BEST_SAMPLE = BestSample;

            // STRICT GATE: Only consider it a match if it's actually Heaplit
            bool isWakeWordMatch = BestPhrase.ToLowerInvariant().Contains("heaplit");
            Result.IS_MATCHED = MaxSimilarity >= Threshold && isWakeWordMatch;

            if (Result.IS_MATCHED)
                DebugConsoleOverlay.Log("Acoustic ML Match", $"Verified Wake Word: \"{BestPhrase}\" ({Result.CONFIDENCE * 100:F1}%)");

            return Result;
        }
    }
}
