// Developer: heaplyn
// Date: 2026-08-18
// Summary: Simplified Evolution Orchestrator.
//          Handles automatic dataset harvesting and kernel refinement cycles.

using System;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class EvolutionManager
    {
        private static bool _isActive = false;

        public static void StartContinuousEvolution()
        {
            if (_isActive) return;
            _isActive = true;

            // 1. DATASET HARVESTING CYCLE (Every 30 mins)
            Task.Run(async () => {
                while (_isActive) {
                    try {
                        if (SettingsManager.Current.DATA_ENABLE_AUTO_SCRAPE) {
                            await DatasetHarvester.RunAutomaticHarvestAsync();
                        }
                    } catch { }
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromMinutes(30));
                }
            });

            // 2. KERNEL REFINEMENT (Every 60 mins)
            Task.Run(async () => {
                while (_isActive) {
                    try {
                        await PerformDeepKernelRefinementAsync();
                    } catch { }
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromMinutes(60));
                }
            });
        }

        private static async Task PerformDeepKernelRefinementAsync()
        {
            try {
                string advice = await LlmRouter.AskAsync("### OPTIMIZE KERNEL\nReview 'NeuralVectorizationKernels.cs'. Provide @mod_code to improve manifold projection density.");
                if (advice.Contains("@mod_code")) await AiAPI.ExecuteAgentLoopAsync(advice);
            } catch { }
        }
    }
}
