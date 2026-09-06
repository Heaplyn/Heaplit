// Developer: heaplyn
// Date: 2026-08-17
// Summary: Advanced Calculus & Symbolic Math Studio.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Linq;

namespace HeaplitLauncher
{
    public class CalculusStudioOverlay : BaseOverlay
    {
        private static CalculusStudioOverlay? _instance;
        private readonly TextBox _inputBox;
        private readonly StackPanel _historyPanel;
        private readonly ScrollViewer _historyScroll;

        public static void ShowStudio()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded) _instance = new CalculusStudioOverlay();
                _instance.Show();
                _instance.BringToFront();
            });
        }

        private CalculusStudioOverlay() : base("HEAPLIT CALCULUS STUDIO", 600, 700)
        {
            _instance = this;
            this.Closed += (s, e) => _instance = null;
            WindowPositionManager.RegisterWindow(this, nameof(CalculusStudioOverlay));

            var layout = new Grid { Margin = new Thickness(15) };
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _inputBox = CreateTextBox(); _inputBox.FontSize = 18;
            _inputBox.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) SolveCurrent(); };
            Grid.SetRow(_inputBox, 0); layout.Children.Add(_inputBox);

            _historyPanel = new StackPanel();
            _historyScroll = new ScrollViewer { Content = _historyPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(_historyScroll, 1); layout.Children.Add(_historyScroll);

            var toolbar = new System.Windows.Controls.Primitives.UniformGrid { Columns = 5, Margin = new Thickness(0, 10, 0, 0) };
            toolbar.Children.Add(CreateStyledButton("DIFF", (s, e) => AddCommand("diff ")));
            toolbar.Children.Add(CreateStyledButton("GRAPH", (s, e) => AddCommand("/graph ")));
            toolbar.Children.Add(CreateStyledButton("TRIG", (s, e) => AddCommand("sin(")));
            toolbar.Children.Add(CreateStyledButton("PI", (s, e) => AddCommand("pi")));
            toolbar.Children.Add(CreateStyledButton("SOLVE", (s, e) => SolveCurrent(), true));
            Grid.SetRow(toolbar, 2); layout.Children.Add(toolbar);

            this.UserContent = layout; _inputBox.Focus();
        }

        private void AddCommand(string cmd) { _inputBox.Text += cmd; _inputBox.CaretIndex = _inputBox.Text.Length; _inputBox.Focus(); }

        private async void SolveCurrent()
        {
            string query = _inputBox.Text.Trim(); if (string.IsNullOrEmpty(query)) return;
            _inputBox.Clear();

            if (query.StartsWith("/")) { HandleSlashCommand(query); return; }

            var item = AddHistoryItem(query, "Calculating...");

            try
            {
                // Run evaluation off-thread to prevent UI hang
                string res = await Task.Run(() => CoreRegistry.Intelligence.Math.Evaluate(query));
                UpdateHistoryItem(item, res);
            }
            catch (Exception ex) { UpdateHistoryItem(item, "Error: " + ex.Message); }
        }

        private void HandleSlashCommand(string cmd)
        {
            string[] parts = cmd.Split(' ', 2);
            string action = parts[0].ToLower();
            string args = parts.Length > 1 ? parts[1] : "";

            switch (action)
            {
                case "/graph":
                    if (string.IsNullOrEmpty(args)) AddHistoryItem(cmd, "Usage: /graph <expression with x>");
                    else { new GraphOverlay(args).Show(); AddHistoryItem(cmd, $"Plotted graph: {args}"); }
                    break;
                case "/clear":
                    _historyPanel.Children.Clear();
                    break;
                case "/analyze":
                    Task.Run(async () => {
                        AddHistoryItem(cmd, "AI performing deep math analysis...");
                        string prompt = $"Perform detailed step-by-step math analysis of: {args}";
                        string res = await CoreRegistry.Intelligence.Llm.AskAsync(prompt);
                        Application.Current.Dispatcher.Invoke(() => AddHistoryItem("AI ANALYSIS", res));
                    });
                    break;
                case "/help":
                    AddHistoryItem(cmd, "Available: /graph <expr>, /analyze <expr>, /clear, /help");
                    break;
                default:
                    AddHistoryItem(cmd, "Unknown slash command.");
                    break;
            }
        }

        private Border AddHistoryItem(string q, string r)
        {
            var b = new Border { Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 5) };
            var s = new StackPanel();
            s.Children.Add(new TextBlock { Text = q, Foreground = Brushes.Gray, FontSize = 10, FontWeight = FontWeights.Bold });
            s.Children.Add(new TextBlock { Text = r, Foreground = Brushes.White, FontSize = 14, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,4,0,0) });
            b.Child = s; _historyPanel.Children.Insert(0, b); return b;
        }

        private void UpdateHistoryItem(Border i, string r) {
            Application.Current.Dispatcher.Invoke(() => {
                var t = (TextBlock)((StackPanel)i.Child).Children[1];
                t.Text = r;
            });
        }
    }
}
