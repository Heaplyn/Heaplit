// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to query the Gemini AI API, returning scrollable terminal outputs.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class AiCommandHandler : ICommandHandler
    {
        public bool CanHandle(string Query)
        {
            Query = Query.Trim();
            var Parts = Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (Parts.Length == 0) return false;

            string Cmd = Parts[0].ToLower();
            return SearchUtil.IsClose(Cmd, "ai") ||
                   SearchUtil.IsClose(Cmd, "ask") ||
                   SearchUtil.IsClose(Cmd, "gemini") ||
                   SearchUtil.IsClose(Cmd, "chat") ||
                   SearchUtil.IsClose(Cmd, "companion");
        }

        public List<CommandResult> GetSuggestions(string Query)
        {
            var Suggestions = new List<CommandResult>();
            Query = Query.Trim();

            var Parts = Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (Parts.Length == 0) return Suggestions;

            string Cmd = Parts[0].ToLower();
            double Similarity = SearchUtil.GetSimilarity(Cmd, "ai");

            if (Parts.Length > 1)
            {
                string Prompt = Query.Substring(Cmd.Length).Trim();
                bool CanUseGemini = OfflineCacheManager.CanUseGemini();

                if (CanUseGemini)
                {
                    Suggestions.Add(new CommandResult
                    {
                        TITLE = $"🧠 Ask Gemini AI (Online): \"{Prompt}\"",
                        DESCRIPTION = "Sends query to Gemini API and displays output in console panel",
                        EXECUTE = () => RunAiQuery(Prompt),
                        SIMILARITY = Similarity + 1.0
                    });
                }
                else
                {
                    Suggestions.Add(new CommandResult
                    {
                        TITLE = $"🦙 Ask Local LLM / Search (Offline Mode): \"{Prompt}\"",
                        DESCRIPTION = "Offline Mode Active: Routes query to local Ollama model or search engine",
                        EXECUTE = () => RunAiQuery(Prompt),
                        SIMILARITY = Similarity + 0.5
                    });
                }
            }
            else
            {
                Suggestions.Add(new CommandResult
                {
                    TITLE = "Open AI Chat Companion",
                    DESCRIPTION = "Open the floating interactive chat window",
                    EXECUTE = () => ChatOverlay.ShowChat(),
                    SIMILARITY = Similarity
                });
            }

            return Suggestions;
        }

        private static void RunAiQuery(string Prompt)
        {
            // Route everything through the modern AI Chat Companion overlay
            Task.Run(async () =>
            {
                try
                {
                    // Map HUD manual entries as TEXT source
                    await ChatOverlay.SubmitTextMessage(Prompt);
                }
                catch (Exception Ex)
                {
                    DebugConsoleOverlay.Log("AI Error", $"Query failed: {Ex.Message}");
                }
            });
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("ai <prompt> / ask", "Ask Heaplit AI assistant questions or tasks", "ai explain quantum computing")
            };
        }
    }
}
