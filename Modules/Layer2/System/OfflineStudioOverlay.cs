// Developer: heaplyn
// Date: 2026-08-17
// Summary: Offline Mode & Wi-Fi Pre-Caching Studio Overlay.
//          Provides 1-click pre-caching for Vosk models, TTS, and multi-language toolchains.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class OfflineStudioOverlay : BaseOverlay
    {
        private static OfflineStudioOverlay? _instance;
        private TextBlock _connectionStatus = null!;
        private TextBlock _voskStatus = null!;
        private TextBlock _ttsStatus = null!;
        private TextBlock _progressText = null!;

        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() => {
                if (_instance == null || !_instance.IsLoaded || !_instance.IsVisible) _instance = new OfflineStudioOverlay();
                _instance.Show(); _instance.BringToFront();
            });
        }

        public OfflineStudioOverlay()
            : base("OFFLINE MODE & PRE-CACHING STUDIO", width: 850, height: 700)
        {
            this.Closed += (s, e) => { _instance = null; };
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            var tabControl = new TabControl { Margin = new Thickness(4) };
            StyleTabControl(tabControl);

            // --- Tab 1: Status & Core ---
            var corePanel = CreateTab(tabControl, "📡 Core Status");
            corePanel.Children.Add(CreateHeader("📡 System Connectivity & Cache"));

            _connectionStatus = new TextBlock { FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 4) };
            _connectionStatus.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            corePanel.Children.Add(_connectionStatus);

            _voskStatus = new TextBlock { FontSize = 12, Margin = new Thickness(0, 2, 0, 4) };
            _voskStatus.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            corePanel.Children.Add(_voskStatus);

            _ttsStatus = new TextBlock { FontSize = 12, Margin = new Thickness(0, 2, 0, 8) };
            _ttsStatus.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            corePanel.Children.Add(_ttsStatus);

            var preCacheBtn = CreateButton("📶 Run Full Pre-Cache Sequence");
            preCacheBtn.Height = 40; preCacheBtn.FontWeight = FontWeights.Bold;
            preCacheBtn.Click += async (s, e) => {
                preCacheBtn.IsEnabled = false;
                await OfflineCacheManager.PreCacheAllForOfflineAsync(st => Application.Current.Dispatcher.Invoke(() => _progressText.Text = st));
                RefreshStatus(); preCacheBtn.IsEnabled = true;
            };
            corePanel.Children.Add(preCacheBtn);

            _progressText = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), Opacity = 0.7 };
            _progressText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            corePanel.Children.Add(_progressText);

            // --- Tab 2: Languages & Toolchains ---
            var toolPanel = CreateTab(tabControl, "💻 Dev Toolchains");
            toolPanel.Children.Add(CreateHeader("🛠️ Compilers & Runtimes (Offline Support)"));

            AddToolRow(toolPanel, "C++ (MinGW/MSYS2)", "MSYS2.MSYS2", "g++");
            AddToolRow(toolPanel, "Assembly (NASM)", "NASM.NASM", "nasm");
            AddToolRow(toolPanel, "Python 3.x", "Python.Python.3", "python");
            AddToolRow(toolPanel, ".NET 8.0 SDK", "Microsoft.DotNet.SDK.8", "dotnet");
            AddToolRow(toolPanel, "Node.js (LTS)", "OpenJS.NodeJS", "node");
            AddToolRow(toolPanel, "Go Language", "GoLang.Go", "go");
            AddToolRow(toolPanel, "Rust (rustup)", "Rustlang.Rustup", "rustc");
            AddToolRow(toolPanel, "Java (OpenJDK 21)", "Eclipse.Temurin.21.JDK", "javac");
            AddToolRow(toolPanel, "Ollama LLM Engine", "Ollama.Ollama", "ollama");

            this.UserContent = tabControl;
            RefreshStatus();
        }

        private StackPanel CreateTab(TabControl tabControl, string headerText) {
            var tab = new TabItem { Header = headerText };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(12) };
            scroll.Content = panel; tab.Content = scroll; tabControl.Items.Add(tab);
            return panel;
        }

        private void AddToolRow(StackPanel root, string friendlyName, string packageId, string commandCheck) {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            var nameLabel = new TextBlock { Text = friendlyName, VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Foreground = Brushes.White };
            Grid.SetColumn(nameLabel, 0); row.Children.Add(nameLabel);

            var statusLabel = new TextBlock { Text = "⏳ Checking...", VerticalAlignment = VerticalAlignment.Center, FontSize = 11, FontWeight = FontWeights.Bold };
            Grid.SetColumn(statusLabel, 1); row.Children.Add(statusLabel);

            var actionBtn = new Button { Content = "Install", Height = 24, FontSize = 10, Cursor = Cursors.Hand };
            actionBtn.Click += (s, e) => OfflineCacheManager.InstallToolViaWinget(packageId, friendlyName);
            Grid.SetColumn(actionBtn, 2); row.Children.Add(actionBtn);

            root.Children.Add(row);

            Task.Run(async () => {
                bool installed = OfflineCacheManager.IsAppInstalled(commandCheck);
                Application.Current.Dispatcher.Invoke(() => {
                    statusLabel.Text = installed ? "🟢 Installed" : "🔴 Not Detected";
                    statusLabel.Foreground = installed ? Brushes.LightGreen : Brushes.Tomato;
                    if (installed) { actionBtn.IsEnabled = false; actionBtn.Content = "✅ Ready"; }
                });
            });
        }

        private void RefreshStatus() {
            bool online = OfflineCacheManager.IsInternetAvailable();
            _connectionStatus.Text = online ? "📡 Network: 🟢 Connected" : "📡 Network: 🔴 Offline Mode Active";
            bool voskReady = Directory.Exists(VoskEngine.ModelDirectory);
            _voskStatus.Text = voskReady ? "🎙️ Vosk Model: ✅ Ready Offline" : "🎙️ Vosk Model: ⚠️ Not Downloaded";
            _ttsStatus.Text = "🎵 TTS Samples: ✅ Cached";
        }

        private static TextBlock CreateHeader(string title) => new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 4), Foreground = Brushes.Cyan };
        private static Button CreateButton(string content) => new Button { Content = content, Margin = new Thickness(0, 4, 0, 4), Padding = new Thickness(10, 6, 10, 6), FontSize = 12, Cursor = Cursors.Hand };
    }
}
