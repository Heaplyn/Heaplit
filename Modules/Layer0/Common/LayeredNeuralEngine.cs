// Developer: heaplyn
// Date: 2026-08-18
// Summary: Godellian Hybrid "Meta-Neural" Engine v19 (Ultra-Turbo).
//          High-Capacity Cluster Evolution: Parallel training on all cores.
//          Symbolic Synergy: Constant synthesis of calculus equations.
//          Hardened Guardrails: Clamping and NaN protection at high speed.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public enum CellSpecialization { General, Vision, Audio, Search, Logic, Performance, Benchmark, Symbolic }

    public class GodellianModule
    {
        public Guid Id { get; } = Guid.NewGuid();
        public LayeredTensor Weights;
        public LayeredTensor Bias;
        public LayeredTensor LR_Tensor;
        public LayeredTensor? RecurrentState;

        public CellSpecialization Specialization { get; set; }
        public double Confidence { get; set; } = 0.5;
        public double AvgLatencyMs { get; set; } = 0.0;

        public int InputDim { get; }
        public int OutputDim { get; }

        public GodellianModule(int nin, int nout, CellSpecialization spec = CellSpecialization.General)
        {
            InputDim = nin;
            OutputDim = nout;
            Specialization = spec;
            Weights = LayeredTensor.Random(new[] { nin + nout, nout });
            Bias = new LayeredTensor(new[] { 1, nout });
            LR_Tensor = new LayeredTensor(Weights.Shape);
            Array.Fill(LR_Tensor.Data, 0.05);
        }

        public LayeredTensor Forward(LayeredTensor input, bool training = false)
        {
            var sw = Stopwatch.StartNew();
            LayeredTensor projectedInput = (input.Size != InputDim)
                ? new LayeredTensor(new[] { 1, InputDim }, NeuralVectorizationKernels.ProjectVector(input.Data, InputDim), track: training)
                : input;

            var recursiveIn = projectedInput;
            if (RecurrentState != null && RecurrentState.Size == OutputDim)
            {
                var combined = new LayeredTensor(new[] { 1, InputDim + OutputDim }, track: training);
                Buffer.BlockCopy(projectedInput.Data, 0, combined.Data, 0, InputDim * 8);
                Buffer.BlockCopy(RecurrentState.Data, 0, combined.Data, InputDim * 8, OutputDim * 8);
                recursiveIn = combined;
            }
            else
            {
                var padded = new LayeredTensor(new[] { 1, InputDim + OutputDim }, track: training);
                Buffer.BlockCopy(projectedInput.Data, 0, padded.Data, 0, InputDim * 8);
                recursiveIn = padded;
            }

            var res = (LayeredTensor.MatMul(recursiveIn, Weights) + Bias).Tanh();
            res.Clamp(-1.0, 1.0);
            RecurrentState = new LayeredTensor(res.Shape, res.Data.ToArray());

            AvgLatencyMs = (AvgLatencyMs * 0.9) + (sw.Elapsed.TotalMilliseconds * 0.1);
            return res;
        }

        public void ApplyStabilityGuard()
        {
            bool corrupted = false;
            for (int i = 0; i < Weights.Size; i++) {
                if (double.IsNaN(Weights.Data[i]) || double.IsInfinity(Weights.Data[i])) { corrupted = true; break; }
            }
            if (corrupted) {
                Weights = LayeredTensor.Random(Weights.Shape);
                Confidence *= 0.5;
            }
            Weights.Clamp(-2.5, 2.5);
            Bias.Clamp(-1.2, 1.2);
        }

        public GodellianModule Replicate()
        {
            var child = new GodellianModule(InputDim, OutputDim, Specialization);
            double rate = 0.05; // was GODELLIAN_MUTATION_RATE
            for (int i = 0; i < Weights.Size; i++) {
                child.Weights.Data[i] = Weights.Data[i] + (Random.Shared.NextDouble() * 2 - 1) * rate;
            }
            return child;
        }
    }

    public class GodellianBrain
    {
        private List<GodellianModule> _clusters = new List<GodellianModule>();
        private int _currentInputDim;
        private int _currentOutputDim;
        private readonly object _lock = new object();

        public double LastAccuracy { get; private set; } = 0.0;
        public string LastTrainingSource { get; private set; } = "Standby (Idle)";
        public List<double> AccuracyHistory { get; private set; } = new List<double>();
        private List<string> _vocabList = new List<string>();
        private readonly string _vocabDir;

        public GodellianBrain(int nin, int[] nouts)
        {
            _currentInputDim = nin;
            _currentOutputDim = nouts.Last();
            _vocabDir = Path.Combine(PathHandler.GetDataDirectory(), "Intelligence", "Vocab");
            if (!Directory.Exists(_vocabDir)) Directory.CreateDirectory(_vocabDir);

            ReloadVocabulary();

            int initCount = 8; // was GODELLIAN_INITIAL_CLUSTERS
            foreach (CellSpecialization spec in Enum.GetValues(typeof(CellSpecialization)))
                for (int i = 0; i < Math.Max(3, initCount / 8); i++)
                    _clusters.Add(new GodellianModule(_currentInputDim, _currentOutputDim, spec));
        }

        public void ReloadVocabulary()
        {
            try {
                var files = Directory.GetFiles(_vocabDir, "*.txt");
                lock (_lock) {
                    var cache = new HashSet<string>();
                    foreach (var f in files) {
                        foreach (var l in File.ReadAllLines(f)) {
                            foreach (var p in l.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                                string word = p.Trim().ToLower();
                                if (word.Length > 2) cache.Add(word);
                            }
                        }
                    }
                    // CAP VOCABULARY to prevent exponential memory growth
                    _vocabList = cache.Take(5000).ToList();
                    if (_vocabList.Count > _currentOutputDim) // was GODELLIAN_AUTO_EXPAND_FIELD guard — always expand
                        GrowBrainField(0, _vocabList.Count - _currentOutputDim);
                }
            } catch { }
        }

        public void IngestVocabulary(List<string> words, string category = "General")
        {
            lock (_lock) {
                if (_vocabList.Count >= 5000) return; // Hard limit for stability

                var unique = words.Select(w => w.Trim().ToLower()).Where(w => w.Length > 2 && !_vocabList.Contains(w)).ToList();
                if (unique.Count > 0) {
                    File.AppendAllLines(Path.Combine(_vocabDir, $"{category}.txt"), unique);
                    _vocabList.AddRange(unique);
                    if (_vocabList.Count > 5000) _vocabList = _vocabList.Take(5000).ToList();

                    if (_vocabList.Count > _currentOutputDim) // was GODELLIAN_AUTO_EXPAND_FIELD guard — always expand
                        GrowBrainField(0, _vocabList.Count - _currentOutputDim);
                }
            }
        }

        public void GrowBrainField(int inExp, int outExp)
        {
            lock (_lock) {
                _currentInputDim += inExp; _currentOutputDim += outExp;
                NeuralVectorizationKernels.CurrentDimension = _currentInputDim;
                foreach (CellSpecialization spec in Enum.GetValues(typeof(CellSpecialization)))
                    _clusters.Add(new GodellianModule(_currentInputDim, _currentOutputDim, spec));
            }
        }

        public LayeredTensor Think(double[] flatInput)
        {
            LayeredTensor.ComputeGrad = false;
            try {
                double[] inputData = (flatInput.Length != _currentInputDim) ? NeuralVectorizationKernels.ProjectVector(flatInput, _currentInputDim) : flatInput;
                var x = new LayeredTensor(new[] { 1, _currentInputDim }, inputData);
                LayeredTensor refinement = new LayeredTensor(new[] { 1, _currentOutputDim });
                int passes = NeuralResourceManager.RecursionDepth;
                for (int p = 0; p < passes; p++) refinement = RunConsensusPass(x, refinement, false);
                return refinement;
            } finally { LayeredTensor.ComputeGrad = true; }
        }

        private LayeredTensor RunConsensusPass(LayeredTensor input, LayeredTensor bias, bool training)
        {
            List<GodellianModule> active;
            lock (_lock) active = _clusters.Where(c => c.OutputDim == _currentOutputDim).ToList();
            if (active.Count == 0) return bias;

            var topActive = active.OrderByDescending(c => c.Confidence).Take(training ? 32 : 16).ToList();
            var results = topActive.Select(c => c.Forward(input, training)).ToList();
            var confidences = new LayeredTensor(new[] { results.Count }, topActive.Select(c => c.Confidence).ToArray(), track: training);
            var attention = confidences.Softmax();

            var aggregate = new LayeredTensor(new[] { 1, _currentOutputDim }, track: training);
            for (int i = 0; i < results.Count; i++) {
                double w = attention.Data[i];
                for (int j = 0; j < _currentOutputDim; j++) aggregate.Data[j] += results[i].Data[j] * w;
            }
            return (aggregate + bias).Tanh();
        }

        public void BatchTrain(List<double[]> inputs, List<double[]> targets, int epochs = -1, string source = "Forge")
        {
            if (inputs.Count == 0) return;
            if (epochs <= 0) epochs = 30; // was GODELLIAN_TRAINING_EPOCHS
            LastTrainingSource = source;
            NeuralResourceManager.MonitorResources();
            LogTraining($"Batch training session: {source} ({inputs.Count} samples, {epochs} epochs)");

            List<GodellianModule> current;
            lock (_lock) current = _clusters.Where(c => c.OutputDim == _currentOutputDim).ToList();

            Parallel.ForEach(current, new ParallelOptions { MaxDegreeOfParallelism = 1 }, m => {
                m.ApplyStabilityGuard();
                TrainModuleSurgical(m, inputs, targets, epochs);
                double mLoss = CalculateModuleLoss(m, inputs, targets);
                if (mLoss < 0.1 && _clusters.Count < 48) {
                    lock(_lock) _clusters.Add(m.Replicate());
                } else if (mLoss > 0.98 && _clusters.Count > 10) {
                    lock(_lock) _clusters.Remove(m);
                }
            });

            double totalLoss = CalculateLoss(inputs, targets);
            LastAccuracy = Math.Max(0.0, 100.0 * Math.Exp(-totalLoss * 0.4));
            lock (_lock) {
                AccuracyHistory.Add(LastAccuracy);
                if (AccuracyHistory.Count > 150) AccuracyHistory.RemoveAt(0);
            }
        }

        private void TrainModuleSurgical(GodellianModule m, List<double[]> inputs, List<double[]> targets, int epochs)
        {
            LayeredTensor.ComputeGrad = true;
            for (int e = 0; e < epochs; e++) {
                foreach (var (inp, tgt) in inputs.Zip(targets)) {
                    var x = new LayeredTensor(new[] { 1, m.InputDim }, NeuralVectorizationKernels.ProjectVector(inp, m.InputDim), track: true);
                    var pred = m.Forward(x, training: true);
                    var loss = (pred + (new LayeredTensor(pred.Shape, NeuralVectorizationKernels.ProjectVector(tgt, m.OutputDim)) * new LayeredTensor(pred.Shape, new[] { -1.0 }))).Tanh();
                    m.Weights.ZeroGrad(); m.Bias.ZeroGrad();
                    loss.BackwardPass();
                    for (int i = 0; i < m.Weights.Size; i++) {
                        m.Weights.Data[i] -= m.LR_Tensor.Data[i] * Math.Clamp(m.Weights.Grad[i], -0.5, 0.5);
                        m.LR_Tensor.Data[i] = Math.Clamp(m.LR_Tensor.Data[i] * (1.0 + 0.005 * Math.Sign(-m.Weights.Grad[i])), 0.0001, 0.3);
                    }
                }
            }
        }

        private double CalculateLoss(List<double[]> inputs, List<double[]> targets)
        {
            double total = 0;
            foreach (var (inp, tgt) in inputs.Zip(targets)) {
                var pred = Think(inp);
                for (int i = 0; i < Math.Min(pred.Size, tgt.Length); i++) total += Math.Abs(pred.Data[i] - tgt[i]);
            }
            return total / (inputs.Count + 0.0001);
        }

        private double CalculateModuleLoss(GodellianModule m, List<double[]> inputs, List<double[]> targets)
        {
            double total = 0;
            foreach (var (inp, tgt) in inputs.Zip(targets)) {
                var pred = m.Forward(new LayeredTensor(new[] { 1, m.InputDim }, NeuralVectorizationKernels.ProjectVector(inp, m.InputDim)));
                for (int i = 0; i < Math.Min(pred.Size, tgt.Length); i++) total += Math.Abs(pred.Data[i] - tgt[i]);
            }
            return total / (inputs.Count + 0.0001);
        }

        public string ThinkInWords(double[] flatInput)
        {
            var output = Think(flatInput);
            string equation = SymbolicMathKernel.SynthesizeEquation(output.Data);
            var acts = output.Data.Select((v, i) => new { v, i }).OrderByDescending(x => Math.Abs(x.v)).Take(8);
            var sb = new StringBuilder($"[SYMBOLIC]: {equation}\n[LOGIC]: ");
            lock (_lock) {
                foreach (var a in acts) if (a.i < _vocabList.Count) sb.Append((a.v > 0.1 ? "" : "non-") + _vocabList[a.i] + " ");
            }
            return sb.ToString().Trim() + ".";
        }

        public string GetDiagnosticReport() => $"[GI-V19] Acc: {LastAccuracy:F1}% | Pop: {_clusters.Count} | Field: {_currentInputDim}x{_currentOutputDim}";

        public List<string> TrainingLog { get; private set; } = new List<string>();

        private void LogTraining(string msg)
        {
            lock (_lock)
            {
                TrainingLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
                if (TrainingLog.Count > 50) TrainingLog.RemoveAt(50);
            }
        }

        public async Task<string> ExchangeLogicWithLlmAsync()
        {
            string thought = ThinkInWords(new double[_currentInputDim]);
            string report = GetDiagnosticReport();

            string prompt = $"### GODELLIAN SYNAPTIC EXCHANGE\n" +
                            $"CURRENT STATE: {report}\n" +
                            $"CURRENT THOUGHT: {thought}\n\n" +
                            $"### TASK\n" +
                            $"1. Analyze the symbolic manifold for logical inconsistencies.\n" +
                            $"2. Provide a massive batch of 25-40 new high-level technical, scientific, or philosophical terms to expand our vocabulary.\n" +
                            $"3. Generate a 16-dim 'Knowledge Vector' representing a high-level breakthrough in AI ethics, multi-dimensional calculus, or recursive logic.\n" +
                            $"Format: [CRITIQUE]: ... [NEW_VOCAB]: term1, term2, term3... [VECTOR]: v1,v2...";

            try {
                string response = await LlmRouter.AskAsync(prompt);

                // Parse and Ingest
                if (response.Contains("[NEW_VOCAB]:")) {
                    var terms = response.Split("[NEW_VOCAB]:")[1].Split('\n')[0].Split(',').Select(t => t.Trim()).ToList();
                    IngestVocabulary(terms, "LLM_Exchange");
                    LogTraining($"Exchanged logic with LLM. Absorbed {terms.Count} new terms.");
                }

                if (response.Contains("[VECTOR]:")) {
                    var vStr = response.Split("[VECTOR]:")[1].Split('\n')[0];
                    var vec = vStr.Split(',').Select(s => double.TryParse(s.Trim(), out double d) ? d : 0.0).Take(_currentInputDim).ToArray();
                    // Self-training on the LLM's suggested breakthrough vector
                    BatchTrain(new List<double[]> { new double[_currentInputDim] }, new List<double[]> { vec }, epochs: 10, source: "LLM_Exchange");
                }

                return response;
            } catch (Exception ex) { return $"Exchange failed: {ex.Message}"; }
        }

        public async Task<string> PerformDeepEvolutionaryAnalysisAsync()
        {
            string prompt = "### GODELLIAN DEEP ANALYSIS\n" +
                            "Sir, perform a deep audit of your current neural state. Review the symbolic logic manifold " +
                            "and suggest 5 specific hyper-parameter adjustments or topology mutations to reach v20 intelligence.";
            try {
                return await LlmRouter.AskAsync(prompt);
            } catch { return "Analysis buffer overflow, Sir."; }
        }

        public void MutateTopology() {
            lock (_lock) {
                if (_clusters.Count == 0) return;
                var target = _clusters[Random.Shared.Next(_clusters.Count)];
                int idx = Random.Shared.Next(target.Weights.Size);
                target.Weights.Data[idx] += (Random.Shared.NextDouble() * 2 - 1) * 0.1;
                target.ApplyStabilityGuard();
            }
        }
        public void Evolve(double[][] inputs, double[][] targets, int epochs = 10) => BatchTrain(inputs.ToList(), targets.ToList(), epochs);
    }
}
