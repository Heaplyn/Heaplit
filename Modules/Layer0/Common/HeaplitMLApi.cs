// Developer: heaplyn
// Date: 2026-08-18
// Summary: Heaplit Native C# ML API.
//          Exposes Godellian Brain and Layered Tensor operations to external components.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class HeaplitMLApi
    {
        // ── TEXT & LLM ──────────────────────────────────────────────────────────

        public static async Task<string> AskAiAsync(string prompt, List<ChatTurn>? history = null, CancellationToken ct = default)
        {
            return await CoreRegistry.Intelligence.Llm.AskAsync(prompt, history, ct);
        }

        // ── VISION & IMAGE PROCESSING ───────────────────────────────────────────

        public static async Task<string> AnalyzeCurrentScreenAsync(string question = "What is currently visible?")
        {
            string? base64 = ScreenCaptureUtil.CapturePrimaryScreenToBase64(saveToDisk: false);
            if (string.IsNullOrEmpty(base64)) return "Error: Failed to capture screen.";
            return await AiAPI.AnalyzeImageBase64Async(question, base64);
        }

        // ── LOCAL NEURAL INTELLIGENCE (LayeredIntelligence Port) ────────────────

        /// <summary>
        /// Creates a deep Godellian Brain with the specified layering.
        /// </summary>
        public static GodellianBrain CreateGodellianBrain(int inputSize, int[] hiddenLayers)
        {
            return new GodellianBrain(inputSize, hiddenLayers);
        }

        /// <summary>
        /// Trains a Godellian brain on local data vectors.
        /// </summary>
        public static void TrainBrain(GodellianBrain brain, double[][] inputs, double[][] targets, int epochs = 50)
        {
            brain.Evolve(inputs, targets, epochs);
        }

        /// <summary>
        /// Evaluates an N-Dimensional Tensor pattern.
        /// </summary>
        public static string RunNeuralEvaluation()
        {
            return LayeredIntelligenceEvaluator.EvaluateXorPattern();
        }
    }
}
