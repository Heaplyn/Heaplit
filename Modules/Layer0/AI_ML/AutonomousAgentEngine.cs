// Developer: heaplyn
// Date: 2026-08-18
// Summary: Autonomous Background Agent implementation.
//          Handles app indexing, focus monitoring, and proactive AI assistance.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Text;
using System.Threading;

namespace HeaplitLauncher
{
    public static class AutonomousAgentEngine
    {
        private static DateTime LastAppAudit = DateTime.MinValue;
        private static int DistractionCounter = 0;
        private static readonly string[] DISTRACTIONS = { "youtube", "netflix", "reddit", "facebook", "gaming", "steam" };

        public static void Start()
        {
            // Start the standard autonomous loops
            Task.Run(async () => {
                while (true) {
                    try {
                        AuditAppIndex();
                        AuditFocus();
                        // Teacher-mode screen tutoring is owned by LiveCodingTutorEngine (single path).
                        // Start() is idempotent and the engine self-gates on Teacher Mode.
                        LiveCodingTutorEngine.Start();
                        if (new Random().Next(100) < 5) await RunSubconsciousReflect();
                    } catch { }
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromMinutes(2));
                }
            });

            // Start the Continuous Neural Evolution loop
            EvolutionManager.StartContinuousEvolution();
        }

        private static void AuditAppIndex() {
            if ((DateTime.Now - LastAppAudit).TotalMinutes >= 30) {
                LastAppAudit = DateTime.Now;
                WindowsAppScanner.IndexApplicationsGlobal(true);
            }
        }

        private static void AuditFocus() {
            string win = CoreRegistry.Data.Memory.GetCurrentWindowTitle().ToLower();
            if (DISTRACTIONS.Any(k => win.Contains(k))) {
                DistractionCounter += 2;
                if (DistractionCounter >= 15) {
                    DistractionCounter = 0;
                    TtsManager.Speak("You've been focused on distractions for a while. Need to switch back to productivity?");
                }
            } else DistractionCounter = 0;
        }

        private static async Task RunSubconsciousReflect() {
            string res = await CoreRegistry.Intelligence.Llm.AskAsync("Perform background maintenance check. Decide on tasks like [CLEAN_LOGS]. Respond 'QUIET' if nothing is needed.");
            if (!res.Contains("QUIET")) await AiAPI.ExecuteAgentLoopAsync(res);
        }
    }
}
