// Developer: heaplyn
// Summary: Editable "decompile to C" workbench. Loads a binary, runs the native decompiler
//          toolchain (IDA / Ghidra / RetDec via NativeDecompilerEngine), shows the FULL C in an
//          editable pane, optionally layers an AI clean-up pass (only when a backend is valid),
//          and saves the result as a small editable C project. Free engines can be auto-provisioned;
//          IDA + Hex-Rays are detected and, if absent, the vendor site is opened.

using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace HeaplitLauncher
{
    public class DecompiledProjectOverlay : BaseOverlay
    {
        private static DecompiledProjectOverlay? _instance;

        private string _filePath = "";
        private TextBox _code = null!;
        private TextBlock _status = null!;
        private TextBlock _fileLabel = null!;
        private ComboBox _engineCombo = null!;
        private Button _aiBtn = null!;
        private CancellationTokenSource? _cts;

        public static void ShowOverlay(string? preloadPath = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded) _instance = new DecompiledProjectOverlay();
                _instance.Show();
                _instance.BringToFront();
                if (!string.IsNullOrEmpty(preloadPath) && File.Exists(preloadPath))
                    _instance.SetFile(preloadPath!);
            });
        }

        private DecompiledProjectOverlay() : base("🧬 DECOMPILE → C WORKBENCH", 900, 760)
        {
            _instance = this;
            this.Closed += (s, e) => { try { _cts?.Cancel(); } catch { } _instance = null; };

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // toolbar
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // file row
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // code
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // status

            // ---- toolbar ----
            var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            Button Btn(string t, RoutedEventHandler h, bool primary = false) {
                var b = CreateStyledButton(t, h, isPrimary: primary, fontSize: 11);
                b.Margin = new Thickness(0, 0, 6, 6);
                bar.Children.Add(b); return b;
            }

            Btn("📂 Load Binary", (s, e) => BrowseFile());
            _engineCombo = new ComboBox { Margin = new Thickness(0, 0, 6, 6), Padding = new Thickness(6, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center };
            foreach (var eng in new[] { "Auto (best available)", "IDA (Hex-Rays)", "Ghidra", "RetDec" }) _engineCombo.Items.Add(eng);
            _engineCombo.SelectedIndex = 0;
            bar.Children.Add(_engineCombo);

            Btn("⚙ Convert to C", (s, e) => _ = ConvertAsync(), primary: true);
            _aiBtn = Btn("🧠 AI Clean-up", (s, e) => _ = AiEnhanceAsync());
            Btn("💾 Save Project", (s, e) => SaveProject());
            Btn("🔎 Detect Tools", (s, e) => ShowToolStatus());
            Btn("📥 Get Free Engines", (s, e) => _ = ProvisionAsync());
            Btn("🐞 Open in x64dbg", (s, e) => LaunchDbg());
            Btn("🔑 Get IDA", (s, e) => NativeDecompilerEngine.OpenIdaSite(free: false));

            Grid.SetRow(bar, 0); root.Children.Add(bar);

            // ---- file row ----
            _fileLabel = new TextBlock { Text = "No file loaded.", Foreground = Brushes.Gray, Margin = new Thickness(2, 0, 0, 8), TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetRow(_fileLabel, 1); root.Children.Add(_fileLabel);

            // ---- editable code ----
            _code = new TextBox
            {
                AcceptsReturn = true, AcceptsTab = true, IsReadOnly = false,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = (Application.Current.Resources["MonoFontFamily"] as FontFamily) ?? new FontFamily("Consolas"),
                FontSize = 12.5,
                Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                Foreground = Brushes.Lime,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Padding = new Thickness(8),
                Text = "// Load a binary and click \"Convert to C\".\n" +
                       "// Engines: IDA Hex-Rays > Ghidra > RetDec. Free engines install via \"Get Free Engines\"."
            };
            Grid.SetRow(_code, 2); root.Children.Add(_code);

            // ---- status ----
            _status = new TextBlock { Text = "", Foreground = Brushes.Cyan, Margin = new Thickness(2, 6, 0, 0), TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(_status, 3); root.Children.Add(_status);

            this.UserContent = root;
            RefreshAiButton();
        }

        private void RefreshAiButton()
        {
            bool ok = NativeDecompilerEngine.IsAiValid();
            _aiBtn.IsEnabled = ok;
            _aiBtn.Opacity = ok ? 1.0 : 0.5;
            _aiBtn.ToolTip = ok ? "Rename/annotate the C with the active LLM backend"
                                : "Connect an AI backend (Settings ▶ AI) to enable AI clean-up";
        }

        private void SetFile(string path)
        {
            _filePath = path;
            _fileLabel.Text = "📄 " + path;
            _fileLabel.Foreground = Brushes.White;
            Status($"Loaded {Path.GetFileName(path)} ({new FileInfo(path).Length / 1024} KB).");
        }

        private void BrowseFile()
        {
            var dlg = new OpenFileDialog { Filter = "Binaries (*.exe;*.dll;*.so;*.bin;*.o;*.elf)|*.exe;*.dll;*.so;*.bin;*.o;*.elf|All files (*.*)|*.*" };
            if (dlg.ShowDialog() == true) SetFile(dlg.FileName);
        }

        private async System.Threading.Tasks.Task ConvertAsync()
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath)) { Status("Load a binary first."); return; }
            var engine = _engineCombo.SelectedIndex switch { 1 => DecompilerEngine.Ida, 2 => DecompilerEngine.Ghidra, 3 => DecompilerEngine.RetDec, _ => DecompilerEngine.Auto };
            Status("Decompiling… this can take a while for large binaries.");
            _cts?.Cancel(); _cts = new CancellationTokenSource();
            string path = _filePath; var ct = _cts.Token;
            try
            {
                var res = await System.Threading.Tasks.Task.Run(() =>
                    NativeDecompilerEngine.DecompileToCAsync(path, engine, m => Dispatcher.Invoke(() => Status(m)), ct), ct);
                if (res.Success) { _code.Text = res.Code; Status($"✅ Done via {res.EngineUsed}. {_code.LineCount} lines. Edit freely, then Save Project."); }
                else _code.Text = "// Decompilation failed.\n\n" + res.Log;
            }
            catch (Exception ex) { Status("Error: " + ex.Message); }
        }

        private async System.Threading.Tasks.Task AiEnhanceAsync()
        {
            if (!NativeDecompilerEngine.IsAiValid()) { Status("No AI backend configured."); return; }
            if (string.IsNullOrWhiteSpace(_code.Text)) { Status("Nothing to enhance."); return; }
            Status("🧠 AI is cleaning up the decompiled C…");
            _cts?.Cancel(); _cts = new CancellationTokenSource();
            string src = _code.Text; var ct = _cts.Token;
            try
            {
                string improved = await NativeDecompilerEngine.AiEnhanceAsync(src, ct);
                _code.Text = improved;
                Status("✅ AI clean-up applied. Verify against the raw output before trusting names/types.");
            }
            catch (Exception ex) { Status("AI error: " + ex.Message); }
        }

        private void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(_code.Text)) { Status("Nothing to save."); return; }
            string baseName = string.IsNullOrEmpty(_filePath) ? "recovered" : Path.GetFileNameWithoutExtension(_filePath);
            var dlg = new SaveFileDialog { Filter = "C source (*.c)|*.c", FileName = baseName + ".c" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                string dir = Path.GetDirectoryName(dlg.FileName) ?? Path.GetTempPath();
                string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                string cFile = NativeDecompilerEngine.SaveProject(dir, name, _code.Text);
                Status($"💾 Saved project to {dir} ({Path.GetFileName(cFile)} + CMakeLists.txt).");
            }
            catch (Exception ex) { Status("Save error: " + ex.Message); }
        }

        private void ShowToolStatus()
        {
            var st = NativeDecompilerEngine.DetectTools();
            string Y(bool b) => b ? "✅" : "—";
            Status($"IDA {Y(st.IdaPath != null)} (Hex-Rays {Y(st.IdaHexRays)})  |  Ghidra {Y(st.GhidraHeadless != null)} (Java {Y(st.JavaPresent)})  |  RetDec {Y(st.RetDecPath != null)}  |  x64dbg {Y(st.X64DbgPath != null)}");
            RefreshAiButton();
        }

        private async System.Threading.Tasks.Task ProvisionAsync()
        {
            if (!HumanConfirm.Ask("Download the FREE reverse-engineering engines (Ghidra, RetDec, x64dbg) from their official GitHub releases into Heaplit's tools folder?\n\nThis downloads and unpacks several hundred MB.", "Provision Decompilers"))
            { Status("Provisioning cancelled."); return; }
            Status("📥 Downloading free engines…");
            _cts?.Cancel(); _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            try
            {
                await NativeDecompilerEngine.ProvisionAsync(ghidra: true, retdec: true, x64dbg: true,
                    log: m => Dispatcher.Invoke(() => Status(m)), ct: ct);
                ShowToolStatus();
            }
            catch (Exception ex) { Status("Provision error: " + ex.Message); }
        }

        private void LaunchDbg()
        {
            if (string.IsNullOrEmpty(_filePath)) { Status("Load a binary first."); return; }
            if (!NativeDecompilerEngine.LaunchX64Dbg(_filePath))
                Status("x64dbg not installed — click \"Get Free Engines\" first.");
        }

        private void Status(string msg) => Dispatcher.Invoke(() => _status.Text = msg);
    }
}
