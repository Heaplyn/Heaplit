// Developer: heaplyn
// Date: 2026-08-19
// Summary: AI Tool Manager Overlay.
//          Displays active, synthesized, and manual tools. Allows user to add custom script tools.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HeaplitLauncher.AiTools;

namespace HeaplitLauncher
{
    public class ToolManagerOverlay : BaseOverlay
    {
        private static ToolManagerOverlay? _instance;
        private readonly StackPanel _toolList;

        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded) _instance = new ToolManagerOverlay();
                _instance.Show();
                _instance.BringToFront();
            });
        }

        private ToolManagerOverlay() : base("🛠️ AI TOOL ORCHESTRATOR", 600, 700)
        {
            _instance = this;
            var mainGrid = new Grid { Margin = new Thickness(15) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // List
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Add Tool

            var header = new TextBlock { Text = "Manage autonomous and built-in AI capabilities.", Foreground = Brushes.LightGray, Margin = new Thickness(0,0,0,15) };
            Grid.SetRow(header, 0); mainGrid.Children.Add(header);

            _toolList = new StackPanel();
            var scroll = new ScrollViewer { Content = _toolList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 1); mainGrid.Children.Add(scroll);

            var addBtn = CreateStyledButton("➕ REGISTER NEW CUSTOM SCRIPT TOOL", (s, e) => ShowAddToolDialog(), isPrimary: true);
            addBtn.Height = 40; Grid.SetRow(addBtn, 2); mainGrid.Children.Add(addBtn);

            this.UserContent = mainGrid;
            RefreshToolList();
        }

        private void RefreshToolList()
        {
            _toolList.Children.Clear();
            var tools = AiToolRegistry.GetAllTools();

            foreach (var tool in tools)
            {
                _toolList.Children.Add(CreateToolCard(tool));
            }
        }

        private UIElement CreateToolCard(IAiTool tool)
        {
            var border = new Border { Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0,0,0,10), BorderThickness = new Thickness(1), BorderBrush = Brushes.DimGray };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = $"TAG: @{tool.Tag}", FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, FontSize = 12 });
            info.Children.Add(new TextBlock { Text = $"Pattern: {tool.RegexPattern}", Foreground = Brushes.Gray, FontSize = 10, Margin = new Thickness(0,2,0,0) });

            if (tool is DynamicScriptTool dt)
            {
                var status = new TextBlock { Text = dt.IsVerified ? "✅ VERIFIED AUTO-SYNTH" : "🧪 EXPERIMENTAL", Foreground = dt.IsVerified ? Brushes.Lime : Brushes.Yellow, FontSize = 9, Margin = new Thickness(0,4,0,0) };
                info.Children.Add(status);
            }
            else
            {
                info.Children.Add(new TextBlock { Text = "⚙️ BUILT-IN CORE TOOL", Foreground = Brushes.White, FontSize = 9, Margin = new Thickness(0,4,0,0), Opacity = 0.6 });
            }

            Grid.SetColumn(info, 0); grid.Children.Add(info);

            var delBtn = CreateStyledButton("🗑️", (s, e) => { AiToolRegistry.Unregister(tool.Tag); RefreshToolList(); }, fontSize: 10);
            delBtn.Width = 30; Grid.SetColumn(delBtn, 1); grid.Children.Add(delBtn);

            border.Child = grid;
            return border;
        }

        private void ShowAddToolDialog()
        {
            // Simple popup for tool registration
            var win = new Window { Title = "Register Tool", Width = 450, Height = 400, Background = Brushes.Black, Foreground = Brushes.White, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            var stack = new StackPanel { Margin = new Thickness(20) };

            stack.Children.Add(new TextBlock { Text = "Tool Tag (e.g. MYTOOL):", Foreground = Brushes.Cyan });
            var tagBox = new TextBox { Margin = new Thickness(0,5,0,10) };
            stack.Children.Add(tagBox);

            stack.Children.Add(new TextBlock { Text = "Regex Pattern (e.g. @mytool{(.*)}):", Foreground = Brushes.Cyan });
            var patBox = new TextBox { Margin = new Thickness(0,5,0,10) };
            stack.Children.Add(patBox);

            stack.Children.Add(new TextBlock { Text = "PowerShell Script (use ${1} for groups):", Foreground = Brushes.Cyan });
            var scrBox = new TextBox { Margin = new Thickness(0,5,0,10), Height = 120, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
            stack.Children.Add(scrBox);

            var saveBtn = CreateStyledButton("REGISTER TOOL", (s, e) => {
                if (!string.IsNullOrEmpty(tagBox.Text)) {
                    var tool = new DynamicScriptTool(tagBox.Text.Trim().ToUpper(), patBox.Text, scrBox.Text);
                    AiToolRegistry.Register(tool);
                    RefreshToolList();
                    win.Close();
                }
            }, isPrimary: true);
            stack.Children.Add(saveBtn);

            win.Content = stack;
            win.Show();
        }
    }
}
