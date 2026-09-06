// Developer: heaplyn
// Date: 2026-08-14
// Summary: Autonomous AI Notes Curator. Periodically triggers an AI turn to review, organize, and summarize the hierarchical notes system.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HeaplitLauncher
{
    public static class NotesCuratorManager
    {
        private static DispatcherTimer? _curationTimer;
        private static bool _isCurationInProgress = false;

        public static void Initialize()
        {
            // Trigger curation every 4 hours (14400 seconds)
            _curationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(4)
            };
            _curationTimer.Tick += (s, e) => _ = PerformAutonomousCurationAsync();
            _curationTimer.Start();

            // Perform an initial curation check on startup (delayed slightly to allow boot sequence to finish)
            Task.Delay(10000).ContinueWith(_ => PerformAutonomousCurationAsync());
        }

        public static async Task PerformAutonomousCurationAsync()
        {
            if (_isCurationInProgress) return;
            _isCurationInProgress = true;

            try
            {
                DebugConsoleOverlay.Log("Notes Curator", "Starting autonomous AI notes organization turn...");

                // 1. Build a summary of the current hierarchy
                var hierarchy = NotesManager.GetHierarchy();
                string hierarchyStr = FormatHierarchyForAi(hierarchy);

                // 2. Read specific files that might need summarizing (e.g., Quick Notes)
                string quickNotesContent = NotesManager.LoadNote("Quick Notes.txt");

                string prompt = "## TASK: AUTONOMOUS NOTES CURATION\n" +
                               "Review the current notes hierarchy and content below. Your goal is to organize, clean, and build onto this system.\n\n" +
                               "### CURRENT HIERARCHY:\n" + hierarchyStr + "\n\n" +
                               "### RECENT QUICK NOTES:\n" + (string.IsNullOrWhiteSpace(quickNotesContent) ? "[Empty]" : quickNotesContent) + "\n\n" +
                               "### INSTRUCTIONS:\n" +
                               "1. If 'Quick Notes.txt' is long, move relevant entries into specific categories or new notes.\n" +
                               "2. If you see related notes, suggest creating a new category and moving them into it.\n" +
                               "3. Fix typos in filenames or structure if needed.\n" +
                               "4. Use [WRITE_FILE], [DELETE_PATH], etc. to perform the actions.\n" +
                               "5. If no changes are needed, respond with 'Hierarchy is optimal.'";

                string aiDecision = await AiAPI.AskGemini(prompt);

                // 3. Process the AI's organizational commands
                string results = AgentExecutor.ProcessAIResponse(aiDecision);

                DebugConsoleOverlay.Log("Notes Curator", "Curation turn complete. Result: " + results);
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Notes Curator Error", ex.Message);
            }
            finally
            {
                _isCurationInProgress = false;
            }
        }

        private static string FormatHierarchyForAi(List<NoteItem> items, int indent = 0)
        {
            var sb = new StringBuilder();
            string space = new string(' ', indent * 2);

            foreach (var item in items)
            {
                sb.AppendLine($"{space}- {(item.IS_FOLDER ? "[DIR] " : "[FILE] ")}{item.NAME} (Path: {item.RELATIVE_PATH})");
                if (item.IS_FOLDER && item.CHILDREN.Any())
                {
                    sb.Append(FormatHierarchyForAi(item.CHILDREN, indent + 1));
                }
            }

            return sb.ToString();
        }
    }
}
