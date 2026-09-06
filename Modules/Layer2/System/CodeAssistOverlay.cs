// Developer: heaplyn
// Date: 2026-08-13
// Summary: WPF Side Panel Overlay for displaying real-time AI code suggestions and screen analysis.
// Integrates with CodeAssistManager, docking to the right of the screen automatically.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class CodeAssistOverlay : BaseOverlay
    {
        private static CodeAssistOverlay? _instance;

        private TextBlock _statusText = null!;
        private TextBlock _filesText = null!;
        private TextBox _adviceBox = null!;
        private Button _toggleBtn = null!;

        public static void ShowOverlay()
        {
            if (_instance == null || !_instance.IsLoaded || !_instance.IsVisible)
            {
                _instance = new CodeAssistOverlay();
                _instance.Show();
            }
            else
            {
                _instance.Activate();
                _instance.BringToFront();
                _instance.Focus();
            }
        }

        public static void HideOverlay()
        {
            _instance?.FadeOutAndHide();
        }

        public CodeAssistOverlay() : base("🤖 AI REAL-TIME CODE ASSIST SIDEBAR", 360, 680)
        {
            this.Closed += (s, e) => { _instance = null; };

            // Dock to the right of the primary work area
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Width - this.Width - 10;
            this.Top = (workArea.Height - this.Height) / 2;

            var mainGrid = new Grid { Margin = new Thickness(8) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                     // Status header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scroll advice
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                     // Action controls

            // Status header
            var headerStack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            _statusText = new TextBlock
            {
                Text = CodeAssistManager.IsRunning ? "🟢 CODE ASSIST ACTIVE (8s Loop)" : "🔴 Code Assist Suspended",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = CodeAssistManager.IsRunning ? Brushes.LimeGreen : Brushes.OrangeRed,
                Margin = new Thickness(0, 0, 0, 2)
            };
            headerStack.Children.Add(_statusText);

            _filesText = new TextBlock
            {
                Text = "Files: Detecting workspace files...",
                FontSize = 10,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap
            };
            headerStack.Children.Add(_filesText);
            Grid.SetRow(headerStack, 0);
            mainGrid.Children.Add(headerStack);

            // Advice log box
            _adviceBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11.5,
                Padding = new Thickness(8),
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = CodeAssistManager.CurrentCodeAdvice
            };
            _adviceBox.SetResourceReference(TextBox.BackgroundProperty, "WindowBackgroundBrush");
            _adviceBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            Grid.SetRow(_adviceBox, 1);
            mainGrid.Children.Add(_adviceBox);

            // Action controls
            var controlStack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

            _toggleBtn = CreateButton(CodeAssistManager.IsRunning ? "🛑 Turn Off Code Assist" : "🚀 Turn On Code Assist");
            _toggleBtn.FontWeight = FontWeights.Bold;
            _toggleBtn.Click += (s, e) =>
            {
                CodeAssistManager.Toggle();
                RefreshUiState();
            };
            controlStack.Children.Add(_toggleBtn);

            var queryManualBtn = CreateButton("🧠 Force AI Query Assist");
            queryManualBtn.Click += async (s, e) =>
            {
                queryManualBtn.IsEnabled = false;
                _adviceBox.Text = "⏳ Capture screen & scanning files, querying AI...";
                // Trigger one iteration manually
                await Task.Run(async () =>
                {
                    // Call manager tick directly
                    try
                    {
                        CodeAssistManager.Start();
                        // wait a bit
                        await Task.Delay(100);
                    }
                    catch { }
                });
                queryManualBtn.IsEnabled = true;
            };
            controlStack.Children.Add(queryManualBtn);

            Grid.SetRow(controlStack, 2);
            mainGrid.Children.Add(controlStack);

            this.UserContent = mainGrid;

            // Subscribe to live events
            CodeAssistManager.OnAdviceUpdated += advice =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _adviceBox.Text = advice;
                    _filesText.Text = $"Files: {CodeAssistManager.LastAnalyzedFiles}";
                });
            };

            CodeAssistManager.OnStateChanged += active =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RefreshUiState();
                });
            };

            RefreshUiState();
        }

        private void RefreshUiState()
        {
            bool running = CodeAssistManager.IsRunning;
            _statusText.Text = running ? "🟢 CODE ASSIST ACTIVE (8s Loop)" : "🔴 Code Assist Suspended";
            _statusText.Foreground = running ? Brushes.LimeGreen : Brushes.OrangeRed;
            _toggleBtn.Content = running ? "🛑 Turn Off Code Assist" : "🚀 Turn On Code Assist";
            _filesText.Text = $"Files: {CodeAssistManager.LastAnalyzedFiles}";
        }

        private static Button CreateButton(string content)
        {
            return new Button
            {
                Content = content,
                Margin = new Thickness(0, 2, 0, 4),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 11,
                Cursor = Cursors.Hand
            };
        }
    }
}
