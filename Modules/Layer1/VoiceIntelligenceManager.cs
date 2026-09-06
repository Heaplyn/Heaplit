using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class VoiceIntelligenceManager
    {
        private static readonly string INTELLIGENCE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "VoiceIntelligence.json");
        private static Dictionary<string, string> LearnedCorrections = new();

        static VoiceIntelligenceManager()
        {
            LoadIntelligence();
        }

        public static string ApplyIntelligence(string Transcript)
        {
            if (string.IsNullOrWhiteSpace(Transcript)) return Transcript;
            string Result = Transcript;

            foreach (var Correction in LearnedCorrections)
            {
                Result = System.Text.RegularExpressions.Regex.Replace(
                    Result,
                    @"\b" + System.Text.RegularExpressions.Regex.Escape(Correction.Key) + @"\b",
                    Correction.Value,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return Result;
        }

        /// <summary>
        /// Periodically called to analyze the trigger dataset and find common patterns or "corrections" using the LLM.
        /// </summary>
        public static async Task AnalyzeAndLearnAsync()
        {
            try
            {
                string DatasetExamples = VoiceDatasetManager.GetFewShotExamples();
                if (DatasetExamples.Contains("No recent history")) return;

                string Prompt = "Analyze these recent voice command transcripts and system contexts. " +
                               "Find common phonetic misrecognitions (e.g., user says 'run debug' but it's transcribed as 'run big'). " +
                               "Output ONLY a JSON dictionary where the KEY is the misrecognition and the VALUE is the intended command. " +
                               "If no clear corrections found, return {}. " +
                               "Examples:\n" + DatasetExamples;

                string JsonResponse = await LlmRouter.AskAsync(Prompt);

                // Extract JSON if model included markdown or chat filler
                int Start = JsonResponse.IndexOf('{');
                int End = JsonResponse.LastIndexOf('}');
                if (Start >= 0 && End > Start)
                {
                    string Json = JsonResponse.Substring(Start, End - Start + 1);
                    var NewCorrections = JsonSerializer.Deserialize<Dictionary<string, string>>(Json);
                    if (NewCorrections != null)
                    {
                        foreach (var Kvp in NewCorrections)
                        {
                            if (!LearnedCorrections.ContainsKey(Kvp.Key))
                            {
                                LearnedCorrections[Kvp.Key] = Kvp.Value;
                                DebugConsoleOverlay.Log("Voice-Intelligence", $"Learned correction: \"{Kvp.Key}\" -> \"{Kvp.Value}\"");
                            }
                        }
                        SaveIntelligence();
                    }
                }
            }
            catch { }
        }

        private static void LoadIntelligence()
        {
            try
            {
                if (File.Exists(INTELLIGENCE_PATH))
                {
                    string Json = File.ReadAllText(INTELLIGENCE_PATH);
                    LearnedCorrections = JsonSerializer.Deserialize<Dictionary<string, string>>(Json) ?? new();
                }
            }
            catch { }
        }

        private static void SaveIntelligence()
        {
            try
            {
                string Json = JsonSerializer.Serialize(LearnedCorrections, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(INTELLIGENCE_PATH, Json);
            }
            catch { }
        }
    }
}
