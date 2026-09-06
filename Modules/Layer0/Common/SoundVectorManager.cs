// Developer: heaplyn
// Date: 2026-08-15
// Summary: Vector Store for Environmental Sounds.
//          Maintains a library of acoustic "Fingerprints" (MFCC vectors) for non-voice sounds.
//          Allows Heaplit to recognize sounds like clapping, snapping, sirens, or door knocks.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HeaplitLauncher
{
    public class SoundCategory
    {
        public string Name { get; set; } = string.Empty;
        public List<double[]> Fingerprints { get; set; } = new List<double[]>();
    }

    public static class SoundVectorManager
    {
        private static readonly string LibraryPath = Path.Combine(PathHandler.GetDataDirectory(), "SoundLibrary.json");
        private static List<SoundCategory> _categories = new List<SoundCategory>();
        private static readonly object _lock = new object();

        static SoundVectorManager()
        {
            LoadLibrary();
        }

        public static void LoadLibrary()
        {
            try
            {
                if (File.Exists(LibraryPath))
                {
                    string json = File.ReadAllText(LibraryPath);
                    _categories = JsonSerializer.Deserialize<List<SoundCategory>>(json) ?? new List<SoundCategory>();
                }
                else
                {
                    // Seed with defaults
                    _categories = new List<SoundCategory>
                    {
                        new SoundCategory { Name = "Clap" },
                        new SoundCategory { Name = "Snap" },
                        new SoundCategory { Name = "Whistle" },
                        new SoundCategory { Name = "Sigh" },
                        new SoundCategory { Name = "Frustrated_Noise" },
                        new SoundCategory { Name = "Door_Knock" }
                    };
                    SaveLibrary();
                }
            }
            catch { }
        }

        public static void SaveLibrary()
        {
            try
            {
                string json = JsonSerializer.Serialize(_categories, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LibraryPath, json);
            }
            catch { }
        }

        public static void AddFingerprint(string categoryName, double[] vector)
        {
            lock (_lock)
            {
                var cat = _categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                if (cat == null)
                {
                    cat = new SoundCategory { Name = categoryName };
                    _categories.Add(cat);
                }
                cat.Fingerprints.Add(vector);
                // Keep only last 10 fingerprints per category to avoid search bloat
                if (cat.Fingerprints.Count > 10) cat.Fingerprints.RemoveAt(0);
                SaveLibrary();
            }
        }

        public static (string Category, double Confidence) ClassifyVector(double[] inputVector, double threshold = 0.75)
        {
            lock (_lock)
            {
                string bestCat = "Unknown";
                double maxSim = 0;

                foreach (var cat in _categories)
                {
                    foreach (var fingerprint in cat.Fingerprints)
                    {
                        double sim = AudioFeatureExtractor.CosineSimilarity(inputVector, fingerprint);
                        if (sim > maxSim)
                        {
                            maxSim = sim;
                            bestCat = cat.Name;
                        }
                    }
                }

                return (maxSim >= threshold) ? (bestCat, maxSim) : ("Ambient", maxSim);
            }
        }
    }
}
