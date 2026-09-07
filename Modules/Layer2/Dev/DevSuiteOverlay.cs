// Developer: heaplyn
// Date: 2026-08-19
// Summary: Universal Developer & Offline Suite GUI.
//          One-click setup for Languages, Game Engines, and Tools.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class DevSuiteOverlay : BaseOverlay
    {
        private static DevSuiteOverlay? _instance;
        private readonly StackPanel _mainList;

        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded) _instance = new DevSuiteOverlay();
                _instance.Show();
                _instance.BringToFront();
            });
        }

        private DevSuiteOverlay() : base("🛠️ UNIVERSAL DEV & OFFLINE SUITE", 700, 600)
        {
            _instance = this;

            var mainGrid = new Grid { Margin = new Thickness(15) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var headerStack = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            headerStack.Children.Add(new TextBlock
            {
                Text = "Manage your local development environment and offline tools.",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchBox = new TextBox
            {
                Padding = new Thickness(8, 5, 8, 5),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.DimGray,
                Tag = "Search for any package (e.g. vlc, steam, zoom)..."
            };
            searchBox.Text = searchBox.Tag.ToString();
            searchBox.GotFocus += (s, e) => { if (searchBox.Text == searchBox.Tag.ToString()) searchBox.Text = ""; };
            searchBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(searchBox.Text)) searchBox.Text = searchBox.Tag.ToString(); };

            var searchBtn = CreateStyledButton("SEARCH WINGET", async (s, e) => {
                string q = searchBox.Text.Trim();
                if (!string.IsNullOrEmpty(q) && q != searchBox.Tag.ToString()) await SearchWingetAsync(q);
            }, isPrimary: true, fontSize: 11);

            searchGrid.Children.Add(searchBox);
            Grid.SetColumn(searchBtn, 1);
            searchGrid.Children.Add(searchBtn);
            headerStack.Children.Add(searchGrid);

            var btnRow = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };

            var batchBtn = CreateStyledButton("📥 INSTALL ALL MISSING TOOLS IN SUITE", (s, e) => {
                DevSuiteManager.InstallAllMissing();
            }, isPrimary: true, fontSize: 11);
            batchBtn.Margin = new Thickness(0, 0, 8, 4);
            btnRow.Children.Add(batchBtn);

            var repoImportBtn = CreateStyledButton("🚀 IMPORT REPO & SMART SETUP", (s, e) => {
                UniversalRepoImporterOverlay.ShowOverlay();
            }, isPrimary: false, fontSize: 11);
            repoImportBtn.Margin = new Thickness(0, 0, 0, 4);
            btnRow.Children.Add(repoImportBtn);

            headerStack.Children.Add(btnRow);

            Grid.SetRow(headerStack, 0);
            mainGrid.Children.Add(headerStack);

            _mainList = new StackPanel();
            var scroll = new ScrollViewer { Content = _mainList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 1);
            mainGrid.Children.Add(scroll);

            this.UserContent = mainGrid;

            RefreshListAsync();
        }

        private async Task SearchWingetAsync(string query)
        {
            _mainList.Children.Clear();
            _mainList.Children.Add(new TextBlock { Text = $"🔍 Searching Winget for '{query}'...", Foreground = Brushes.Cyan, Margin = new Thickness(10), HorizontalAlignment = HorizontalAlignment.Center });

            try
            {
                string output = await DevSuiteManager.RunGenericCommandAsync($"winget search \"{query}\"");
                _mainList.Children.Clear();
                _mainList.Children.Add(new TextBlock { Text = $"SEARCH RESULTS FOR '{query.ToUpper()}':", FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, Margin = new Thickness(0, 5, 0, 10), FontSize = 13 });

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(2); // Skip headers
                bool found = false;
                if (lines != null)
                {
                    foreach (var line in lines.Take(20))
                    {
                        var parts = System.Text.RegularExpressions.Regex.Split(line.Trim(), @"\s{2,}");
                        if (parts != null && parts.Length >= 2)
                        {
                            string name = parts[0];
                            string id = parts[1];
                            string version = parts.Length > 2 ? parts[2] : "";

                            var tool = new DevToolInfo { Name = name, WingetId = id, Description = $"Version: {version}", Category = "Search Results" };
                            tool.IsInstalled = await DevSuiteManager.CheckIfInstalledAsync(id);
                            _mainList.Children.Add(CreateToolRow(tool));
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    _mainList.Children.Add(new TextBlock { Text = "No results found on Winget hub.", Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
                }

                var backBtn = CreateStyledButton("🔙 BACK TO CURATED SUITE", (s, e) => RefreshListAsync(), fontSize: 11);
                backBtn.Margin = new Thickness(0, 20, 0, 0);
                _mainList.Children.Add(backBtn);
            }
            catch (Exception ex)
            {
                _mainList.Children.Add(new TextBlock { Text = $"Search failed: {ex.Message}", Foreground = Brushes.Tomato });
            }
        }

        private async void RefreshListAsync()
        {
            _mainList.Children.Clear();

            // 1. Initial Render (Quick) - show tools immediately while probing
            RenderCategories(DevSuiteManager.GetAllTools());

            _mainList.Children.Insert(0, new TextBlock { Text = "⌛ Probing system for installed environments...", Foreground = Brushes.Cyan, Margin = new Thickness(10), HorizontalAlignment = HorizontalAlignment.Center });

            // 2. Background Probe (Optimized)
            await DevSuiteManager.RefreshInstallationStatusAsync();

            // 3. Final Render (Update statuses)
            RefreshToolStatuses();
        }

        private void RenderCategories(List<DevToolInfo> tools)
        {
            _mainList.Children.Clear();
            var categories = tools.Select(t => t.Category).Distinct().OrderBy(c => c);

            foreach (var cat in categories)
            {
                _mainList.Children.Add(new TextBlock { Text = cat.ToUpper(), FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, Margin = new Thickness(0, 15, 0, 5), FontSize = 13 });

                foreach (var tool in tools.Where(t => t.Category == cat))
                {
                    _mainList.Children.Add(CreateToolRow(tool));
                }
            }
        }

        private void RefreshToolStatuses()
        {
            RenderCategories(DevSuiteManager.GetAllTools());
        }

        private UIElement CreateToolRow(DevToolInfo tool)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 2, 0, 4)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock { Text = tool.Name, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 12 });
            infoStack.Children.Add(new TextBlock { Text = tool.Description, Foreground = Brushes.Gray, FontSize = 10 });
            Grid.SetColumn(infoStack, 0);
            grid.Children.Add(infoStack);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal };

            if (tool.IsInstalled)
            {
                var status = new TextBlock { Text = "INSTALLED", Foreground = Brushes.Lime, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), FontSize = 10, FontWeight = FontWeights.Bold };
                btnStack.Children.Add(status);

                var uninstallBtn = CreateStyledButton("UNINSTALL", (s, e) => {
                    DevSuiteManager.UninstallTool(tool.WingetId);
                }, fontSize: 10);
                btnStack.Children.Add(uninstallBtn);
            }
            else
            {
                var installBtn = CreateStyledButton("INSTALL", (s, e) => {
                    DevSuiteManager.InstallTool(tool.WingetId);
                }, isPrimary: true, fontSize: 10);
                btnStack.Children.Add(installBtn);
            }

            Grid.SetColumn(btnStack, 1);
            grid.Children.Add(btnStack);

            border.Child = grid;
            return border;
        }
    }
}
