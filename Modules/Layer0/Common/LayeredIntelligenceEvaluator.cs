// Developer: heaplyn
// Date: 2026-08-18
// Summary: High-level evaluator for ported Godellian neural networks.
//          Provides multi-dimensional tensor indexing and evaluation logic.

using System;
using System.Collections.Generic;
using System.Linq;

namespace HeaplitLauncher
{
    public static class LayeredIntelligenceEvaluator
    {
        /// <summary>
        /// Evaluates a simple 2D pattern (XOR) using the local Godellian Brain.
        /// </summary>
        public static string EvaluateXorPattern()
        {
            var brain = new GodellianBrain(2, new[] { 4, 4, 1 });

            double[][] inputs = {
                new double[] { 0, 0 },
                new double[] { 0, 1 },
                new double[] { 1, 0 },
                new double[] { 1, 1 }
            };
            double[][] targets = {
                new double[] { 0 },
                new double[] { 1 },
                new double[] { 1 },
                new double[] { 0 }
            };

            brain.Evolve(inputs, targets, epochs: 100);

            var res = new System.Text.StringBuilder("### XOR Evolution Results:\n");
            foreach (var input in inputs)
            {
                var output = brain.Think(input);
                res.AppendLine($"Input: [{input[0]}, {input[1]}] -> Output: {output.Data[0]:F4}");
            }
            return res.ToString();
        }

        /// <summary>
        /// Example of N-Dimensional tensor indexing logic ported from C++.
        /// </summary>
        public static double GetTensorValue(double[] flatData, int[] dims, int[] indices)
        {
            if (dims.Length != indices.Length) throw new ArgumentException("Dimension mismatch.");
            int offset = 0;
            int stride = 1;
            for (int i = dims.Length - 1; i >= 0; i--)
            {
                offset += indices[i] * stride;
                stride *= dims[i];
            }
            return flatData[offset];
        }
    }
}
