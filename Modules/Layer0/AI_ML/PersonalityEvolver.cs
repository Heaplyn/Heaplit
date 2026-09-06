// Developer: heaplyn
// Date: 2026-08-15
// Summary: Autonomous Personality Evolution Engine.
//          Analyzes recent chat logs to detect the user's preferred "vibe" and Heaplit's evolving persona.
//          Updates a persistent 'PersonalityProfile.md' in the instructions folder.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class PersonalityEvolver
    {
        private static bool IsRunning = false;
        private static readonly string ProfilePath = Path.Combine(PathHandler.GetDataDirectory(), "Instructions", "PersonalityProfile.md");

        public static void Start()
        {
            if (IsRunning) return;
            IsRunning = true;

            Task.Run(async () =>
            {
                // Wait for initial boot logic
                await Task.Delay(TimeSpan.FromMinutes(2));

                while (IsRunning)
                {
                    try
                    {
                        await EvolvePersonalityAsync();
                    }
                    catch (Exception ex)
                    {
                        DebugConsoleOverlay.Log("Personality-Error", ex.Message);
                    }

                    // Evolve every hour
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromHours(1));
                }
            });

            DebugConsoleOverlay.Log("Personality-System", "Personality Evolution Engine active.");
        }

        private static async Task EvolvePersonalityAsync()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Conversations");
            if (!Directory.Exists(logDir)) return;

            // Get the last 2 logs to analyze recent vibe
            var files = Directory.GetFiles(logDir, "*.txt")
                                 .Select(f => new FileInfo(f))
                                 .OrderByDescending(f => f.LastWriteTime)
                                 .Take(2)
                                 .ToList();

            if (files.Count == 0) return;

            var sb = new StringBuilder();
            foreach (var file in files)
            {
                sb.AppendLine(File.ReadAllText(file.FullName));
            }

            string recentHistory = sb.ToString();
            if (recentHistory.Length > 8000) recentHistory = recentHistory.Substring(recentHistory.Length - 8000);

            string currentProfile = File.Exists(ProfilePath) ? File.ReadAllText(ProfilePath) : "No personality profile established yet.";

            string prompt = "You are the Heaplit Personality Architect. Analyze the recent conversation history and the current personality profile.\n" +
                            "1. Detect the user's current 'vibe' (sarcastic, serious, friendly, chaotic).\n" +
                            "2. Note any inside jokes, nicknames, or recurring themes.\n" +
                            "3. Update the 'Personality Profile' to reflect how Heaplit should behave to best match this dynamic.\n" +
                            "Maintain the core 'Sassy Heaplit' persona but evolve the specific details.\n\n" +
                            "CURRENT PROFILE:\n" + currentProfile + "\n\n" +
                            "RECENT HISTORY:\n" + recentHistory + "\n\n" +
                            "Return ONLY the updated Markdown content for the 'PersonalityProfile.md' file.";

            try
            {
                string evolvedProfile = await LlmRouter.AskAsync(prompt, null);

                if (!string.IsNullOrWhiteSpace(evolvedProfile) && !evolvedProfile.Contains("Error"))
                {
                    string dir = Path.GetDirectoryName(ProfilePath)!;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    await File.WriteAllTextAsync(ProfilePath, evolvedProfile);
                    DebugConsoleOverlay.Log("Personality-Update", "Heaplit's persona has evolved based on recent interactions.");
                }
            }
            catch { }
        }
    }
}
