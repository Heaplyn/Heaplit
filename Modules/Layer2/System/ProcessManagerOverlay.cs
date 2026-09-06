// Developer: heaplyn
// Date: 2026-08-12
// Summary: Advanced Glassmorphic Task Manager overlay with real-time process filtering, sorting, and termination.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace HeaplitLauncher
{
    public class ProcessInfo
    {
        public string Name { get; set; } = "";
        public int Id { get; set; }
        public double MemoryMB { get; set; }
        public Process ProcessRef { get; set; } = null!;
    }

    public class ProcessManagerOverlay : BaseOverlay
    {
        private static ProcessManagerOverlay? _instance;
        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded) _instance = new ProcessManagerOverlay();
                _instance.Show();
                _instance.BringToFront();
            });
        }
        private readonly DispatcherTimer _timer;
        private readonly TextBox _searchBox;
        private string _searchFilter = string.Empty;
        private DataGrid _processGrid = null!;

        public static void OpenManager() => ShowOverlay();

        private ProcessManagerOverlay()
            : base("HEAPLIT PROCESS STUDIO", width: 800, height: 600)
        {
            this.Closed += (s, e) =>
            {
                _timer?.Stop();
                _instance = null;
            };

            var rootGrid = new Grid { Margin = new Thickness(15) };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Toolbar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Grid
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // 1. Toolbar
            var toolbarGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchLabel = new TextBlock { Text = "🔍 SEARCH PROTOCOL: ", VerticalAlignment = VerticalAlignment.Center, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan };
            Grid.SetColumn(searchLabel, 0);
            toolbarGrid.Children.Add(searchLabel);

            _searchBox = CreateTextBox();
            _searchBox.TextChanged += (s, e) => {
                _searchFilter = _searchBox.Text.ToLower().Trim();
                RefreshProcessList();
            };
            Grid.SetColumn(_searchBox, 1);
            toolbarGrid.Children.Add(_searchBox);

            var killBtn = CreateStyledButton("🧨 TERMINATE SELECTED", (s, e) => KillSelectedProcess(), isPrimary: true);
            Grid.SetColumn(killBtn, 2);
            toolbarGrid.Children.Add(killBtn);

            Grid.SetRow(toolbarGrid, 0);
            rootGrid.Children.Add(toolbarGrid);

            // 2. DataGrid
            _processGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                RowBackground = Brushes.Transparent,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                SelectionMode = DataGridSelectionMode.Single,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };
            _processGrid.Columns.Add(new DataGridTextColumn { Header = "Process Name", Binding = new System.Windows.Data.Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            _processGrid.Columns.Add(new DataGridTextColumn { Header = "PID", Binding = new System.Windows.Data.Binding("Id"), Width = 80 });
            _processGrid.Columns.Add(new DataGridTextColumn { Header = "Memory (MB)", Binding = new System.Windows.Data.Binding("MemoryMB") { StringFormat = "{0:N1}" }, Width = 120 });

            Grid.SetRow(_processGrid, 1);
            rootGrid.Children.Add(_processGrid);

            // 3. Footer
            var footer = new TextBlock { FontSize = 10, Margin = new Thickness(0,10,0,0), Opacity = 0.6, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Gray };
            footer.Text = "Telemetry Active. Monitoring local threads...";
            Grid.SetRow(footer, 2);
            rootGrid.Children.Add(footer);

            this.UserContent = rootGrid;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += (s, e) => RefreshProcessList();
            _timer.Start();

            RefreshProcessList();
        }

        public void RefreshProcessList()
        {
            try
            {
                var currentSelection = _processGrid.SelectedItem as ProcessInfo;

                var processes = Process.GetProcesses()
                    .Select(p => {
                        try {
                            return new ProcessInfo {
                                Name = p.ProcessName,
                                Id = p.Id,
                                MemoryMB = p.PrivateMemorySize64 / 1024.0 / 1024.0,
                                ProcessRef = p
                            };
                        } catch { return null; }
                    })
                    .Where(p => p != null)
                    .Where(p => string.IsNullOrEmpty(_searchFilter) || p!.Name.ToLower().Contains(_searchFilter))
                    .OrderByDescending(p => p!.MemoryMB)
                    .Take(50)
                    .ToList();

                _processGrid.ItemsSource = processes;

                if (currentSelection != null)
                {
                    _processGrid.SelectedItem = processes.FirstOrDefault(p => p!.Id == currentSelection.Id);
                }
            }
            catch { }
        }

        private void KillSelectedProcess()
        {
            if (_processGrid.SelectedItem is ProcessInfo info)
            {
                try
                {
                    info.ProcessRef.Kill();
                    DebugConsoleOverlay.Log("System", $"Command: Terminated {info.Name} ({info.Id})");
                    RefreshProcessList();
                }
                catch (Exception ex)
                {
                    TextOverlay.Show($"⚠️ Failed: {ex.Message}", 2000);
                }
            }
        }
    }
}
