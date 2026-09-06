// Developer: heaplyn
// Date: 2026-08-15
// Summary: User Sentiment & Emotional Intelligence Engine.
//          Tracks the user's emotional state over the current session.
//          Allows Heaplit to "Understand" when to dial down the sass and be more supportive.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public enum UserMood { Neutral, Focused, Stressed, Frustrated, Happy, Bored }

    public static class EmotionalContextManager
    {
        public static UserMood CurrentMood { get; private set; } = UserMood.Neutral;
        private static double _sentimentScore = 0; // -1 to 1

        public static void Start()
        {
            // Sync with sound detection for frustration cues
            EnvironmentalAudioAnalyzer.OnSoundDetected += (cat, conf) =>
            {
                if (cat == "Sigh" || cat == "Frustrated_Noise") CurrentMood = UserMood.Stressed;
                else if (cat == "Success_Cheer") CurrentMood = UserMood.Happy;
            };
        }

        public static async Task AnalyzeSentimentAsync(string userText)
        {
            string prompt = $"Analyze the emotional 'vibe' of this user input. Return ONLY one word: NEUTRAL, FOCUSED, STRESSED, FRUSTRATED, HAPPY, BORED.\n\nINPUT: \"{userText}\"";

            try
            {
                string moodStr = await LlmRouter.AskAsync(prompt, null);
                if (Enum.TryParse<UserMood>(moodStr.Trim(), true, out var mood))
                {
                    CurrentMood = mood;
                }
            }
            catch { }
        }

        public static string GetEmotionalDirective()
        {
            return CurrentMood switch
            {
                UserMood.Stressed => "DIRECTIVE: User is stressed. Minimize sass. Be concise and highly helpful. Offer technical support.",
                UserMood.Frustrated => "DIRECTIVE: User is frustrated. Stop jokes. Focus purely on resolving the issue immediately.",
                UserMood.Focused => "DIRECTIVE: User is in flow. Do not interrupt unless necessary. Stay in the background.",
                UserMood.Happy => "DIRECTIVE: User is in a good mood. Sass is encouraged. Celebrate successes with them.",
                UserMood.Bored => "DIRECTIVE: User is idle/bored. Engage them with a witty thought or system insight.",
                _ => "DIRECTIVE: Maintain standard witty Heaplit persona."
            };
        }
    }
}
