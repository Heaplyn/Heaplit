// Developer: heaplyn
// Ring 1 (Analysis) of the HEAPLIT Disassembler Suite.
// AI-assisted static analysis: builds a GROUNDED feature bundle from Ring0 primitives
// (real import table, classified Win32 capabilities, IOCs, per-section entropy) and either
// scores it instantly (heuristic) or sends it to the LLM for a structured threat report.
// Depends on Ring0 (PeStatics) and shared overlay state; consumed by the Ring2 UI.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public partial class DisassemblerSuiteOverlay : BaseOverlay
    {
        private TextBox _aiStaticText = null!;
        private TextBlock _staticVerdictBadge = null!;
        private Button _aiStaticBtn = null!;
        private Button _heuristicScanBtn = null!;

        // Cached, capped ASCII view of the file so capability/language scans don't
        // re-stringify the whole buffer on every pass. Invalidated per analyze.
        private string? _fileAsciiCache;
        private const int AsciiCap = 32 * 1024 * 1024;

        /// <summary>Shared, cached ASCII projection of the loaded file (capped). Ring0-style helper.</summary>
        internal string FileAscii()
        {
            if (_fileAsciiCache != null) return _fileAsciiCache;
            if (_loadedFileBytes == null) return _fileAsciiCache = string.Empty;
            int len = Math.Min(_loadedFileBytes.Length, AsciiCap);
            _fileAsciiCache = Encoding.ASCII.GetString(_loadedFileBytes, 0, len);
            return _fileAsciiCache;
        }

        internal void InvalidateStaticCaches() => _fileAsciiCache = null;

        // ─── Tab construction ──────────────────────────────────────────────────────
        internal TabItem BuildAiStaticAnalysisTab()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };

            _staticVerdictBadge = new TextBlock
            {
                Text = "  NO FILE  ",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(70, 70, 78)),
                Padding = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };
            toolbar.Children.Add(_staticVerdictBadge);

            _heuristicScanBtn = CreateStyledButton("⚡ HEURISTIC SCAN", (s, e) => RunHeuristicScan(), isPrimary: false, fontSize: 10);
            _heuristicScanBtn.Margin = new Thickness(0, 0, 8, 0);
            _heuristicScanBtn.IsEnabled = false;
            toolbar.Children.Add(_heuristicScanBtn);

            _aiStaticBtn = CreateStyledButton("🤖 AI DEEP ANALYSIS", (s, e) => _ = RunAiStaticAnalysisAsync(), isPrimary: true, fontSize: 10);
            _aiStaticBtn.Margin = new Thickness(0, 0, 8, 0);
            _aiStaticBtn.IsEnabled = false;
            toolbar.Children.Add(_aiStaticBtn);

            var copyBtn = CreateStyledButton("📋 COPY", (s, e) =>
            {
                try { if (!string.IsNullOrEmpty(_aiStaticText.Text)) Clipboard.SetText(_aiStaticText.Text); } catch { }
            }, isPrimary: false, fontSize: 10);
            toolbar.Children.Add(copyBtn);

            Grid.SetRow(toolbar, 0);
            grid.Children.Add(toolbar);

            _aiStaticText = CreateLogConsole();
            _aiStaticText.Text =
                "// === HEAPLIT AI STATIC ANALYSIS ===\n" +
                "// Load a binary (Browse + Analyze), then:\n" +
                "//   ⚡ HEURISTIC SCAN  — instant, offline. Parses the real PE import table,\n" +
                "//                        classifies Win32 capabilities, extracts IOCs, scores risk.\n" +
                "//   🤖 AI DEEP ANALYSIS — feeds that grounded feature bundle to the LLM for a\n" +
                "//                        structured behavioral & threat report with a severity verdict.\n";
            Grid.SetRow(_aiStaticText, 1);
            grid.Children.Add(_aiStaticText);

            return new TabItem { Header = "🤖 AI Static Analysis", Content = grid };
        }

        /// <summary>Enables the tab's actions once a file is loaded and refreshes the instant scan.</summary>
        internal void OnFileLoadedForStaticAnalysis()
        {
            if (_heuristicScanBtn != null) _heuristicScanBtn.IsEnabled = true;
            if (_aiStaticBtn != null) _aiStaticBtn.IsEnabled = true;
            RunHeuristicScan();
        }

        // ─── Grounded feature bundle (shared by heuristic + AI) ────────────────────
        private string BuildStaticFeatureBundle(out int riskScore, out List<string> riskDrivers)
        {
            riskScore = 0;
            riskDrivers = new List<string>();
            var sb = new StringBuilder();
            byte[]? b = _loadedFileBytes;
            if (b == null) return "// No file loaded.";

            sb.AppendLine($"FILE: {Path.GetFileName(_loadedFilePath)}  ({b.Length:N0} bytes)");
            sb.AppendLine($"DETECTED LANGUAGE/COMPILER: {DetectLanguage(_isDotNet)}");
            sb.AppendLine($".NET MANAGED: {_isDotNet}");

            double entropy = CalculateEntropy(b);
            sb.AppendLine($"OVERALL SHANNON ENTROPY: {entropy:F3} / 8.0" +
                          (entropy > 7.2 ? "  ⚠️ packed/encrypted/compressed" : ""));
            if (entropy > 7.2) { riskScore += 2; riskDrivers.Add("High overall entropy (likely packed)"); }

            if (PeStatics.IsPe(b))
            {
                int e = (int)PeStatics.ReadU32(b, 0x3C);
                ushort machine = PeStatics.ReadU16(b, e + 4);
                ushort magic = PeStatics.ReadU16(b, e + 24);
                uint entry = PeStatics.ReadU32(b, e + 40);
                string arch = machine switch
                {
                    0x014c => "x86", 0x8664 => "x64", 0xaa64 => "ARM64", 0x01c0 => "ARM",
                    _ => $"0x{machine:X4}"
                };
                sb.AppendLine($"ARCHITECTURE: {arch} ({(magic == 0x20b ? "PE32+ 64-bit" : "PE32 32-bit")})   EntryPoint RVA: 0x{entry:X8}");

                // Security mitigations (reuses the Ring2 checker).
                sb.AppendLine("MITIGATIONS:");
                sb.Append(CheckMitigations(e));

                // Per-section entropy anomalies.
                ushort numSections = PeStatics.ReadU16(b, e + 6);
                ushort sizeOpt = PeStatics.ReadU16(b, e + 20);
                int secTable = e + 24 + sizeOpt;
                var anomalies = new List<string>();
                for (int i = 0; i < numSections; i++)
                {
                    int s = secTable + i * 40;
                    if (s + 40 > b.Length) break;
                    string name = Encoding.ASCII.GetString(b, s, 8).TrimEnd('\0');
                    uint rawSize = PeStatics.ReadU32(b, s + 16);
                    uint rawAddr = PeStatics.ReadU32(b, s + 20);
                    if (rawAddr > 0 && rawSize > 0 && rawAddr + rawSize <= b.Length)
                    {
                        byte[] sec = new byte[rawSize];
                        Array.Copy(b, rawAddr, sec, 0, rawSize);
                        double se = CalculateEntropy(sec);
                        if (se > 7.2) anomalies.Add($"{name} ({se:F2})");
                    }
                }
                if (anomalies.Count > 0)
                {
                    sb.AppendLine($"HIGH-ENTROPY SECTIONS: {string.Join(", ", anomalies)}");
                    riskScore += 1; riskDrivers.Add("High-entropy section(s)");
                }
            }
            sb.AppendLine();

            // Real import table + capability classification.
            var imports = PeStatics.ParseImports(b);
            var allFuncs = imports.SelectMany(m => m.Functions).ToList();
            sb.AppendLine($"IMPORTS: {imports.Count} module(s), {allFuncs.Count} function(s)");
            foreach (var mod in imports.OrderByDescending(m => m.Functions.Count).Take(20))
                sb.AppendLine($"  {mod.Dll}  ({mod.Functions.Count})");
            if (imports.Count > 20) sb.AppendLine($"  ... and {imports.Count - 20} more module(s)");
            sb.AppendLine();

            var caps = PeStatics.ClassifyApis(allFuncs);
            // High-signal categories that push the risk score.
            string[] highRisk = { "Anti-Debug / Anti-Analysis", "Process Injection / Code Exec",
                                  "Keylogging / Input Capture", "Screen / Clipboard Capture" };
            if (caps.Count > 0)
            {
                sb.AppendLine("CLASSIFIED CAPABILITIES (from real imports):");
                foreach (var kv in caps.OrderByDescending(k => highRisk.Contains(k.Key)))
                {
                    sb.AppendLine($"  [{kv.Key}] {string.Join(", ", kv.Value.Take(12))}");
                    if (highRisk.Contains(kv.Key)) { riskScore += 2; riskDrivers.Add(kv.Key); }
                    else riskScore += 1;
                }
                // Network + Cryptography together is a common ransomware/stealer signature.
                if (caps.ContainsKey("Networking") && caps.ContainsKey("Cryptography"))
                { riskScore += 2; riskDrivers.Add("Networking + Cryptography combo"); }
            }
            else
            {
                sb.AppendLine("CLASSIFIED CAPABILITIES: none of interest (or import table unavailable).");
            }
            sb.AppendLine();

            // IOCs from the already-extracted strings.
            var iocs = PeStatics.ExtractIocs(_allExtractedStrings);
            sb.AppendLine($"INDICATORS OF COMPROMISE (IOCs): {iocs.Total} total");
            void Emit(string label, IEnumerable<string> items, int take)
            {
                var list = items.Take(take).ToList();
                if (list.Count == 0) return;
                sb.AppendLine($"  {label}:");
                foreach (var it in list) sb.AppendLine($"    {it}");
            }
            Emit("URLs", iocs.Urls, 25);
            Emit("IPv4", iocs.Ips, 25);
            Emit("Registry", iocs.RegistryKeys, 20);
            Emit("Paths", iocs.Paths, 20);
            Emit("Suspicious files", iocs.SuspiciousFiles, 20);
            if (iocs.Urls.Count > 0) riskScore += 1;

            return sb.ToString();
        }

        private (string label, Brush bg) ScoreToVerdict(int score)
        {
            if (score <= 1) return ("  LOW RISK  ", new SolidColorBrush(Color.FromRgb(34, 120, 60)));
            if (score <= 3) return ("  GUARDED  ", new SolidColorBrush(Color.FromRgb(150, 130, 30)));
            if (score <= 6) return ("  ELEVATED  ", new SolidColorBrush(Color.FromRgb(190, 100, 25)));
            return ("  HIGH RISK  ", new SolidColorBrush(Color.FromRgb(170, 40, 40)));
        }

        private void SetBadge(string text, Brush bg)
        {
            if (_staticVerdictBadge == null) return;
            _staticVerdictBadge.Text = text;
            _staticVerdictBadge.Background = bg;
        }

        // ─── Instant heuristic scan (offline) ──────────────────────────────────────
        private void RunHeuristicScan()
        {
            if (_loadedFileBytes == null) return;
            try
            {
                string bundle = BuildStaticFeatureBundle(out int score, out var drivers);
                var (label, bg) = ScoreToVerdict(score);
                SetBadge(label, bg);

                var sb = new StringBuilder();
                sb.AppendLine("// === HEURISTIC STATIC ANALYSIS (offline, grounded in real PE data) ===");
                sb.AppendLine($"// Risk score: {score}   Verdict:{label.Trim()}");
                if (drivers.Count > 0)
                    sb.AppendLine($"// Drivers: {string.Join("; ", drivers.Distinct())}");
                sb.AppendLine("// Heuristic only — not a verdict of maliciousness. Use 🤖 AI DEEP ANALYSIS for reasoning.");
                sb.AppendLine();
                sb.Append(bundle);
                _aiStaticText.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                _aiStaticText.Text = $"// Heuristic scan failed: {ex.Message}";
            }
        }

        // ─── AI deep analysis (LLM, grounded) ──────────────────────────────────────
        private async Task RunAiStaticAnalysisAsync()
        {
            if (_loadedFileBytes == null)
            {
                MessageBox.Show("Load a file first (Browse + Analyze).", "No File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _aiStaticBtn.IsEnabled = false;
            SetBadge("  ANALYZING…  ", new SolidColorBrush(Color.FromRgb(40, 90, 150)));
            _aiStaticText.Text = "🤖 Building grounded feature bundle (imports, capabilities, IOCs, entropy)...";

            try
            {
                string bundle = await Task.Run(() =>
                {
                    string s = BuildStaticFeatureBundle(out _, out _);
                    return s;
                });

                Application.Current.Dispatcher.Invoke(() =>
                    _aiStaticText.Text = "🤖 Feature bundle ready. Reasoning over it with the LLM engine...\n\n" + bundle);

                string prompt =
                    "You are the HEAPLIT binary static-analysis engine. Below is a GROUNDED feature bundle " +
                    "extracted directly from a binary (real PE import table, classified Win32 capabilities, " +
                    "IOCs from strings, entropy). Reason ONLY from this evidence — do not invent imports or IOCs.\n\n" +
                    "Respond in EXACTLY this structure:\n" +
                    "SEVERITY: <Benign|Low|Medium|High|Critical>\n" +
                    "SUMMARY: <2-3 sentences on what this binary most likely is and does>\n" +
                    "LIKELY PURPOSE: <bullet points>\n" +
                    "BEHAVIORAL CAPABILITIES: <bullets, each tied to the evidence that implies it>\n" +
                    "THREAT ASSESSMENT: <why this severity; call out anti-analysis, injection, C2, crypto+net combos>\n" +
                    "NOTABLE IOCs: <the handful that matter most, or 'none'>\n" +
                    "RECOMMENDED NEXT STEPS: <what an analyst should look at next in this suite>\n\n" +
                    $"=== FEATURE BUNDLE for {Path.GetFileName(_loadedFilePath)} ===\n{bundle}";

                string response = await Task.Run(async () => await CoreRegistry.Intelligence.Llm.AskAsync(prompt));

                // Pull the SEVERITY line to color the badge.
                string sev = "Medium";
                foreach (var line in response.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.StartsWith("SEVERITY:", StringComparison.OrdinalIgnoreCase))
                    { sev = t.Substring("SEVERITY:".Length).Trim(); break; }
                }
                var badge = sev.ToLowerInvariant() switch
                {
                    var x when x.StartsWith("benign") => ("  BENIGN  ", Color.FromRgb(34, 120, 60)),
                    var x when x.StartsWith("low") => ("  LOW  ", Color.FromRgb(60, 120, 70)),
                    var x when x.StartsWith("medium") => ("  MEDIUM  ", Color.FromRgb(180, 130, 30)),
                    var x when x.StartsWith("high") => ("  HIGH  ", Color.FromRgb(190, 80, 25)),
                    var x when x.StartsWith("critical") => ("  CRITICAL  ", Color.FromRgb(170, 35, 35)),
                    _ => ("  ANALYZED  ", Color.FromRgb(40, 90, 150))
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetBadge(badge.Item1, new SolidColorBrush(badge.Item2));
                    _aiStaticText.Text =
                        $"// === AI STATIC ANALYSIS REPORT ===\n" +
                        $"// Target: {Path.GetFileName(_loadedFilePath)}\n" +
                        $"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                        $"// (Grounded in the feature bundle below the report.)\n" +
                        $"// ===================================\n\n" +
                        response +
                        "\n\n// ─────────── EVIDENCE: FEATURE BUNDLE ───────────\n" + bundle;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetBadge("  ERROR  ", new SolidColorBrush(Color.FromRgb(120, 120, 120)));
                    _aiStaticText.Text = $"AI static analysis failed: {ex.Message}";
                });
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => _aiStaticBtn.IsEnabled = true);
            }
        }
    }
}
