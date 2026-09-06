// Developer: heaplyn
// Date: 2026-08-18
// Summary: High-Performance Glassmorphic AI Code Studio.
//          Improved syntax highlighting responsiveness and added AI Autocomplete Explanation.
//          Fixed autocomplete popup visibility and interaction.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.Diagnostics;

namespace HeaplitLauncher
{
    public class EditorTab
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public string OriginalText { get; set; } = string.Empty;
        public RichTextBox Editor { get; set; } = null!;
        public TabItem TabItem { get; set; } = null!;
        public bool IsModified => GetText(Editor) != OriginalText;

        public static string GetText(RichTextBox rtb) { var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd); return range.Text.Replace("\r\n", "\n").TrimEnd('\n'); }
        public static void SetText(RichTextBox rtb, string text) { rtb.Document.Blocks.Clear(); var p = new Paragraph(new Run(text)) { Margin = new Thickness(0) }; rtb.Document.Blocks.Add(p); }
    }

    public class TextEditorOverlay : BaseOverlay
    {
        private static TextEditorOverlay? _instance;
        private readonly List<EditorTab> _openTabs = new();
        private readonly TreeView _fileTreeView;
        private readonly ListBox _errorListBox;
        private readonly Border _errorPanel;
        private readonly TextBlock _statusLabel;
        private readonly StackPanel _sidePanelStack;
        private readonly TextBlock _sidePanelTitle;
        private readonly Border _autocompletePopup;
        private readonly ListBox _autocompleteListBox;
        private readonly TextBlock _explanationLabel;
        private readonly TabControl _editorTabControl;
        private CancellationTokenSource? _highlightCts;

        public static void ShowOverlay() {
            Application.Current.Dispatcher.Invoke(() => {
                if (_instance == null || !_instance.IsLoaded) _instance = new TextEditorOverlay();
                _instance.Show(); _instance.BringToFront();
            });
        }

        public static void OpenFile(string path) {
            Application.Current.Dispatcher.InvokeAsync(() => {
                if (_instance == null || !_instance.IsLoaded) _instance = new TextEditorOverlay();
                _instance.Show(); _instance.LoadFile(path); _instance.BringToFront();
            });
        }

        public static void OpenWorkspace(string path) {
            Application.Current.Dispatcher.InvokeAsync(() => {
                if (_instance == null || !_instance.IsLoaded) _instance = new TextEditorOverlay();
                _instance.Show(); _instance.LoadWorkspace(path); _instance.BringToFront();
            });
        }

        public static void PromptAndOpenFile() {
            Application.Current.Dispatcher.Invoke(() => {
                var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Open File" };
                if (dlg.ShowDialog() == true) OpenFile(dlg.FileName);
            });
        }

        private TextEditorOverlay() : base("HEAPLIT AI CODE STUDIO", 980, 680)
        {
            _instance = this;
            var layoutGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Toolbar
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Body
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // --- Toolbar ---
            var toolbar = new Border { Background = new SolidColorBrush(Color.FromArgb(45, 138, 43, 226)), Padding = new Thickness(10, 5, 10, 5), BorderThickness = new Thickness(0,0,0,1), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)) };
            var tbGrid = new Grid();
            tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbStack = new StackPanel { Orientation = Orientation.Horizontal };
            tbStack.Children.Add(CreateToolbarButton("📥 IMPORT", (s, e) => ShowImportDialog()));
            tbStack.Children.Add(CreateToolbarButton("🚀 RUN", (s, e) => RunActiveFile()));
            tbStack.Children.Add(CreateToolbarButton("🧩 PACKS", (s, e) => ShowLanguagePacks()));
            tbStack.Children.Add(CreateToolbarButton("🛒 STORE", (s, e) => ShowMarketplace()));
            tbStack.Children.Add(CreateToolbarButton("🧠 AI FIX", (s, e) => RunAiFix()));
            tbStack.Children.Add(CreateToolbarButton("⚙️", (s, e) => SettingsOverlay.OpenSettings()));
            tbStack.Children.Add(CreateToolbarButton("💾", (s, e) => SaveActiveFile()));
            Grid.SetColumn(tbStack, 1); tbGrid.Children.Add(tbStack);

            _statusLabel = new TextBlock { Text = "Ready", Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center, FontSize = 10, Opacity = 0.7 };
            tbGrid.Children.Add(_statusLabel);
            toolbar.Child = tbGrid; Grid.SetRow(toolbar, 0); layoutGrid.Children.Add(toolbar);

            // --- Body ---
            var mainContainer = new Grid();
            mainContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Explorer
            mainContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Editor
            mainContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) }); // Side Panel

            _fileTreeView = new TreeView { Margin = new Thickness(5), BorderThickness = new Thickness(0,0,1,0), BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) };
            BaseOverlay.StyleTreeView(_fileTreeView);
            _fileTreeView.SelectedItemChanged += (s, e) => { if (_fileTreeView.SelectedItem is TreeViewItem t && t.Tag is string p && File.Exists(p)) LoadFile(p); };
            Grid.SetColumn(_fileTreeView, 0); mainContainer.Children.Add(_fileTreeView);

            _editorTabControl = new TabControl();
            StyleTabControl(_editorTabControl);
            Grid.SetColumn(_editorTabControl, 1); mainContainer.Children.Add(_editorTabControl);

            var sidePanelBorder = new Border { Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.DimGray };
            var sideStack = new StackPanel { Margin = new Thickness(12) };
            _sidePanelTitle = new TextBlock { FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, Margin = new Thickness(0,0,0,12) };
            sideStack.Children.Add(_sidePanelTitle);
            _sidePanelStack = new StackPanel(); sideStack.Children.Add(_sidePanelStack);
            sidePanelBorder.Child = new ScrollViewer { Content = sideStack };
            Grid.SetColumn(sidePanelBorder, 2); mainContainer.Children.Add(sidePanelBorder);

            Grid.SetRow(mainContainer, 1); layoutGrid.Children.Add(mainContainer);

            // --- Footer ---
            _errorPanel = new Border { Height = 100, Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)), BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = Brushes.DimGray, Visibility = Visibility.Collapsed };
            _errorListBox = new ListBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.White, Margin = new Thickness(5) };
            _errorPanel.Child = _errorListBox; Grid.SetRow(_errorPanel, 2); layoutGrid.Children.Add(_errorPanel);

            // --- Autocomplete Popup ---
            _autocompletePopup = new Border { Width = 350, Height = 320, Background = new SolidColorBrush(Color.FromArgb(250, 15, 15, 25)), BorderThickness = new Thickness(1), BorderBrush = Brushes.Cyan, CornerRadius = new CornerRadius(5), Visibility = Visibility.Collapsed, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
            var autoGrid = new Grid();
            autoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            autoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _autocompleteListBox = new ListBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.White, FontSize = 12 };
            _autocompleteListBox.SelectionChanged += OnAutocompleteSelectionChanged;
            autoGrid.Children.Add(_autocompleteListBox);

            _explanationLabel = new TextBlock { Text = "Explain...", FontSize = 10, Foreground = Brushes.Gray, Padding = new Thickness(12, 8, 12, 12), TextWrapping = TextWrapping.Wrap, FontStyle = FontStyles.Italic, Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)) };
            Grid.SetRow(_explanationLabel, 1); autoGrid.Children.Add(_explanationLabel);
            _autocompletePopup.Child = autoGrid;

            var finalGrid = new Grid();
            finalGrid.Children.Add(layoutGrid);
            var canvas = new Canvas { IsHitTestVisible = false };
            finalGrid.Children.Add(canvas); canvas.Children.Add(_autocompletePopup);

            this.UserContent = finalGrid;
            this.PreviewKeyDown += OnPreviewKeyDown;
        }

        private async void OnAutocompleteSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (_autocompleteListBox.SelectedItem is AutocompleteSuggestion s) {
                _explanationLabel.Text = "🧠 AI is analyzing purpose...";
                string code = Application.Current.Dispatcher.Invoke(() => GetActiveEditorText());
                string ext = GetActiveEditorExtension();
                string explanation = await EditorIntelligenceManager.GetAiExplanationAsync(s.Text, code, ext);
                _explanationLabel.Text = explanation;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e) {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S) { e.Handled = true; SaveActiveFile(); }
            if (e.Key == Key.Escape) { _autocompletePopup.Visibility = Visibility.Collapsed; }
            if (_autocompletePopup.Visibility == Visibility.Visible) {
                if (e.Key == Key.Enter || e.Key == Key.Tab) {
                    if (_autocompleteListBox.SelectedItem is AutocompleteSuggestion s) InsertText(s.Text);
                    _autocompletePopup.Visibility = Visibility.Collapsed; e.Handled = true;
                }
                if (e.Key == Key.Down) { _autocompleteListBox.SelectedIndex = (_autocompleteListBox.SelectedIndex + 1) % _autocompleteListBox.Items.Count; e.Handled = true; }
                if (e.Key == Key.Up) { _autocompleteListBox.SelectedIndex = (_autocompleteListBox.SelectedIndex <= 0) ? _autocompleteListBox.Items.Count - 1 : _autocompleteListBox.SelectedIndex - 1; e.Handled = true; }
            }
        }

        private void InsertText(string text) {
            if (_editorTabControl.SelectedItem is TabItem ti && ti.Tag is EditorTab t) {
                var rtb = t.Editor;
                var range = rtb.Selection;
                range.Text = text;
                rtb.CaretPosition = range.End;
            }
        }

        private void RequestHighlight(RichTextBox rtb, string ext) {
            _highlightCts?.Cancel();
            _highlightCts = new CancellationTokenSource();
            var token = _highlightCts.Token;
            Task.Delay(300, token).ContinueWith(task => {
                if (task.IsCanceled) return;
                Application.Current.Dispatcher.Invoke(() => SyntaxHighlighter.Highlight(rtb, ext));
            }, token);
        }

        public void LoadFile(string path) {
            if (!File.Exists(path)) return;
            if (_openTabs.Any(t => t.FilePath == path)) { _editorTabControl.SelectedItem = _openTabs.First(t => t.FilePath == path).TabItem; return; }

            var tab = new EditorTab { FilePath = path, OriginalText = File.ReadAllText(path) };
            var editor = new RichTextBox {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                CaretBrush = Brushes.Cyan,
                Padding = new Thickness(15),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                AcceptsTab = true,
                AutoWordSelection = false,
                BorderThickness = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = false
            };
            editor.Document.PagePadding = new Thickness(0); EditorTab.SetText(editor, tab.OriginalText);
            string ext = Path.GetExtension(path);

            SyntaxHighlighter.Highlight(editor, ext);
            editor.TextChanged += (s, e) => RequestHighlight(editor, ext);
            editor.KeyUp += (s, e) => HandleKeyUp(editor, ext, e);

            tab.Editor = editor;
            tab.TabItem = new TabItem { Header = tab.FileName, Content = editor, Tag = tab };
            _openTabs.Add(tab); _editorTabControl.Items.Add(tab.TabItem); _editorTabControl.SelectedItem = tab.TabItem;
        }

        private void HandleKeyUp(RichTextBox editor, string ext, KeyEventArgs e) {
            if (e.Key == Key.OemPeriod || (e.Key >= Key.A && e.Key <= Key.Z)) {
                var rect = editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
                var container = (Grid)this.UserContent;

                // Position popup near cursor
                Canvas.SetLeft(_autocompletePopup, Math.Min(rect.Left + 50, this.ActualWidth - 400));
                Canvas.SetTop(_autocompletePopup, Math.Min(rect.Top + 80, this.ActualHeight - 350));

                var suggestions = EditorIntelligenceManager.GetSuggestions("", ext, EditorTab.GetText(editor));
                if (suggestions.Any()) {
                    _autocompleteListBox.ItemsSource = suggestions;
                    _autocompleteListBox.SelectedIndex = 0;
                    _autocompletePopup.Visibility = Visibility.Visible;
                } else _autocompletePopup.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleSidePanel(string title, Action populateAction) {
            var col = ((Grid)((Grid)this.UserContent).Children[0]).ColumnDefinitions[2];
            if (col.Width.Value > 0 && _sidePanelTitle.Text == title) { col.Width = new GridLength(0); return; }
            _sidePanelTitle.Text = title; _sidePanelStack.Children.Clear();
            populateAction(); col.Width = new GridLength(240);
        }

        private void ShowLanguagePacks() => ToggleSidePanel("LANGUAGE PACKS", () => {
            AddSideItem("C++ / C Core", "Full STL, Preprocessor, Header support", true);
            AddSideItem("x64 Assembly", "NASM 2.15, Registers, Hex views", true);
            AddSideItem("SQL Studio", "Syntax, Keyword auto-completion", true);
            AddSideItem("Lua Runtime", "Lightweight scripts, End-tag matching", true);
        });

        private void ShowMarketplace() => ToggleSidePanel("EXTENSION MARKETPLACE", () => {
            AddSideItem("Heaplit VIM", "VIM motions for HUD editor", false);
            AddSideItem("GitLens Lite", "Inline commit authorship", false);
            AddSideItem("Auto-Doc AI", "Generate Javadoc/KDoc via AI", false);
        });

        private void AddSideItem(string name, string desc, bool installed) {
            var b = new Border { Padding = new Thickness(10), Margin = new Thickness(0,0,0,6), CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)) };
            var s = new StackPanel();
            s.Children.Add(new TextBlock { Text = name, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s.Children.Add(new TextBlock { Text = desc, FontSize = 10, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,2,0,0) });
            var btn = new Button { Content = installed ? "✅ Ready" : "📥 Install", FontSize = 9, Margin = new Thickness(0,8,0,0), Height = 22, IsEnabled = !installed, Cursor = Cursors.Hand };
            s.Children.Add(btn); b.Child = s; _sidePanelStack.Children.Add(b);
        }

        private void ShowImportDialog() {
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Import Project File", Filter = "Source Files (*.cs;*.cpp;*.c;*.h;*.asm;*.sql;*.lua;*.py;*.js)|*.cs;*.cpp;*.c;*.h;*.asm;*.sql;*.lua;*.py;*.js|All Files|*.*" };
            if (dlg.ShowDialog() == true) LoadFile(dlg.FileName);
        }

        public void LoadWorkspace(string path) {
            if (!Directory.Exists(path)) return;
            _fileTreeView.Items.Clear();
            var rootNode = CreateDirectoryNode(path);
            if (rootNode != null) { rootNode.IsExpanded = true; _fileTreeView.Items.Add(rootNode); }
        }

        private TreeViewItem? CreateDirectoryNode(string path) {
            try {
                var node = new TreeViewItem { Header = $"📁 {Path.GetFileName(path)}", Tag = path };
                node.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush");
                foreach (var dir in Directory.GetDirectories(path)) { var d = CreateDirectoryNode(dir); if (d != null) node.Items.Add(d); }
                foreach (var file in Directory.GetFiles(path)) { var f = new TreeViewItem { Header = $"📄 {Path.GetFileName(file)}", Tag = file }; f.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimaryBrush"); node.Items.Add(f); }
                return node;
            } catch { return null; }
        }

        private void SaveActiveFile() {
            if (_editorTabControl.SelectedItem is TabItem ti && ti.Tag is EditorTab t) {
                File.WriteAllText(t.FilePath, EditorTab.GetText(t.Editor));
                t.OriginalText = EditorTab.GetText(t.Editor);
                _statusLabel.Text = $"Saved: {t.FileName}";
                TextOverlay.Show($"✅ Saved {t.FileName}", 1500);
            }
        }

        private void RunActiveFile() {
            if (_editorTabControl.SelectedItem is TabItem ti && ti.Tag is EditorTab t) {
                string ext = Path.GetExtension(t.FilePath).ToLower();
                string dir = Path.GetDirectoryName(t.FilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(t.FilePath);
                Task.Run(() => {
                    ProcessStartInfo? psi = null;
                    if (ext == ".cpp" || ext == ".c") psi = new ProcessStartInfo("cmd.exe", $"/c g++ \"{t.FilePath}\" -o \"{name}.exe\" && \"{name}.exe\" & pause") { WorkingDirectory = dir };
                    else if (ext == ".py") psi = new ProcessStartInfo("python", $"\"{t.FilePath}\"") { WorkingDirectory = dir };
                    else if (ext == ".asm") psi = new ProcessStartInfo("cmd.exe", $"/c nasm -f win64 \"{t.FilePath}\" -o \"{name}.obj\" && gcc \"{name}.obj\" -o \"{name}.exe\" && \"{name}.exe\" & pause") { WorkingDirectory = dir };
                    else if (ext == ".cs") psi = new ProcessStartInfo("dotnet", "run") { WorkingDirectory = dir };
                    else if (ext == ".lua") psi = new ProcessStartInfo("lua", $"\"{t.FilePath}\"") { WorkingDirectory = dir };

                    if (psi != null) {
                        psi.UseShellExecute = true; try { Process.Start(psi); } catch (Exception ex) { Application.Current.Dispatcher.Invoke(() => TextOverlay.Show($"❌ Run Failed: {ex.Message}", 3000)); }
                    } else Application.Current.Dispatcher.Invoke(() => TextOverlay.Show("⚠️ No runner configured.", 3000));
                });
            }
        }

        private void RunAiFix() {
            if (_editorTabControl.SelectedItem is TabItem ti && ti.Tag is EditorTab t) {
                Task.Run(async () => {
                    string code = Application.Current.Dispatcher.Invoke(() => EditorTab.GetText(t.Editor));
                    string prompt = $"## TASK\nFix bugs and optimize this {Path.GetExtension(t.FilePath)} code:\n\n{code}";
                    TextOverlay.Show("🧠 AI is analyzing...", 3000);
                    var res = await CoreRegistry.Intelligence.Llm.AskAsync(prompt);
                    Application.Current.Dispatcher.Invoke(() => { ChatOverlay.ShowChat(); });
                });
            }
        }

        private Button CreateToolbarButton(string c, RoutedEventHandler h) {
            var b = new Button { Content = c, Margin = new Thickness(0,0,8,0), Padding = new Thickness(12,4,12,4), Background = Brushes.Transparent, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 9, Cursor = Cursors.Hand };
            b.Click += h; return b;
        }

        private string GetActiveEditorText() => (_editorTabControl.SelectedItem is TabItem ti && ti.Tag is EditorTab t) ? EditorTab.GetText(t.Editor) : "";
        private string GetActiveEditorExtension() => (_editorTabControl.SelectedItem is TabItem ti && ti.Tag is EditorTab t) ? Path.GetExtension(t.FilePath) : "";
    }
}
