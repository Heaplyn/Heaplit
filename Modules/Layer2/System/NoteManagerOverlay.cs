// Developer: heaplyn
// Date: 2026-08-19
// Summary: Advanced Hierarchical Note Manager Studio.
//          Features: Markdown Rendering, HTML Support, Multi-Mode Editor, and Autosave.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class NoteManagerOverlay : BaseOverlay
    {
        private static NoteManagerOverlay? _instance;
        private TreeView _treeView = null!;
        private TextBox _editor = null!;
        private RichTextBox _previewBox = null!;
        private TabControl _editorTabs = null!;
        private TextBlock _statusLabel = null!;
        private string? _activeNoteRelativePath;
        private DispatcherTimer _autosaveTimer;
        private bool _isChangingNote = false;
        private Grid? _pdfViewerGrid;
        private TextBlock? _pdfBookTitleLabel;
        private TextBlock? _pdfInfoLabel;
        private string? _activePdfPath;

        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() => {
                if (_instance == null || !_instance.IsLoaded) {
                    _instance = new NoteManagerOverlay();
                    _instance.Show();
                }
                _instance.Activate();
                _instance.BringToFront();
            });
        }

        public class NoteManagerState { public string? LastNotePath { get; set; } }

        public NoteManagerOverlay() : base("📓 HEAPLIT NOTES STUDIO", width: 950, height: 650)
        {
            var state = PersistentStateManager.LoadState<NoteManagerState>("Notes");

            this.Closed += (s, e) => {
                _instance = null;
                SaveActiveNote();
                PersistentStateManager.SaveState("Notes", new NoteManagerState { LastNotePath = _activeNoteRelativePath ?? _activePdfPath });
            };

            var mainGrid = new Grid { Margin = new Thickness(12) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // --- Sidebar ---
            var sidebar = new Grid();
            sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sidebar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            toolbar.Children.Add(CreateSmallButton("📁+", "New Category", (s, e) => CreateNewCategory()));
            toolbar.Children.Add(CreateSmallButton("📄+", "New Note", (s, e) => CreateNewNote()));
            toolbar.Children.Add(CreateSmallButton("📚+", "Add PDF Book from URL", (s, e) => DownloadPdfFromUrlPrompt()));
            toolbar.Children.Add(CreateSmallButton("📎", "Add Local PDF File", (s, e) => AddLocalPdfPrompt()));
            toolbar.Children.Add(CreateSmallButton("🔄", "Refresh", (s, e) => RefreshTree()));
            toolbar.Children.Add(CreateSmallButton("🗑️", "Delete Item", (s, e) => DeleteSelected()));
            toolbar.Children.Add(CreateSmallButton("🤖", "Trigger Autonomous AI Notes Curation", (s, e) => TriggerAiCuration()));
            sidebar.Children.Add(toolbar);

            _treeView = new TreeView { Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), Foreground = Brushes.White, BorderThickness = new Thickness(0,0,1,0), BorderBrush = Brushes.DimGray };
            _treeView.SelectedItemChanged += (s, e) => { if (_treeView.SelectedItem is TreeViewItem tvi && tvi.Tag is NoteItem item && !item.IS_FOLDER) LoadNote(item.RELATIVE_PATH); };
            Grid.SetRow(_treeView, 1); sidebar.Children.Add(_treeView);
            Grid.SetColumn(sidebar, 0); mainGrid.Children.Add(sidebar);

            // --- Editor Area ---
            var editorContainer = new Grid { Margin = new Thickness(15, 0, 0, 0) };
            editorContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Formatting Toolbar
            editorContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Tabs
            editorContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status

            // Formatting Toolbar
            var fmtToolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,10) };
            fmtToolbar.Children.Add(CreateFormatButton("B", "Bold", "**", "**"));
            fmtToolbar.Children.Add(CreateFormatButton("I", "Italic", "*", "*"));
            fmtToolbar.Children.Add(CreateFormatButton("#", "Header", "# ", ""));
            fmtToolbar.Children.Add(CreateFormatButton("<>", "Code", "```\n", "\n```"));
            fmtToolbar.Children.Add(CreateFormatButton("🔗", "Link", "[", "](url)"));
            fmtToolbar.Children.Add(CreateSmallButton("🤖", "Ask AI to expand/rewrite/fix this note", (s, e) => AskAiToHelpWithNote()));
            Grid.SetRow(fmtToolbar, 0); editorContainer.Children.Add(fmtToolbar);

            _editorTabs = new TabControl { Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
            BaseOverlay.StyleTabControl(_editorTabs);

            // Tab 1: Editor
            _editor = new TextBox { AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = new SolidColorBrush(Color.FromArgb(40, 0,0,0)), Foreground = Brushes.White, CaretBrush = Brushes.Cyan, Padding = new Thickness(15), FontSize = 14, FontFamily = new FontFamily("Consolas"), BorderThickness = new Thickness(1), BorderBrush = Brushes.DimGray };
            _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autosaveTimer.Tick += (s, e) => { _autosaveTimer.Stop(); SaveActiveNote(); };
            _editor.TextChanged += (s, e) => { if (!_isChangingNote) _autosaveTimer.Start(); };

            var editTab = new TabItem { Header = "✏️ SOURCE", Content = _editor };
            _editorTabs.Items.Add(editTab);

            // Tab 2: Preview
            _previewBox = new RichTextBox { IsReadOnly = true, Background = new SolidColorBrush(Color.FromArgb(30, 20, 20, 30)), Foreground = Brushes.White, BorderThickness = new Thickness(1), BorderBrush = Brushes.DimGray, Padding = new Thickness(20), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _previewBox.Document.PagePadding = new Thickness(0);
            var previewTab = new TabItem { Header = "👁️ PREVIEW", Content = _previewBox };
            _editorTabs.SelectionChanged += (s, e) => { if (_editorTabs.SelectedIndex == 1) RenderPreview(); };
            _editorTabs.Items.Add(previewTab);

            Grid.SetRow(_editorTabs, 1); editorContainer.Children.Add(_editorTabs);

            // PDF Viewer Grid (hidden by default)
            _pdfViewerGrid = new Grid { Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)) };
            _pdfViewerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            var pdfStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            
            var bookIcon = new TextBlock { Text = "📚", FontSize = 72, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,20) };
            pdfStack.Children.Add(bookIcon);
            
            _pdfBookTitleLabel = new TextBlock { Text = "Book Title", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,8), TextWrapping = TextWrapping.Wrap, MaxWidth = 500, TextAlignment = TextAlignment.Center };
            pdfStack.Children.Add(_pdfBookTitleLabel);

            // File size / metadata line
            var pdfInfoLabel = new TextBlock { Text = "", FontSize = 12, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,20) };
            pdfStack.Children.Add(pdfInfoLabel);
            // Wire LoadNote to update it
            this.Loaded += (_, __) => { /* resolved below via _pdfInfoLabel field */ };

            var openPdfBtn = new Button { Content = "📖 OPEN PDF", Width = 200, Height = 45, FontSize = 14, FontWeight = FontWeights.Bold, Background = Brushes.DarkCyan, Foreground = Brushes.White, Cursor = Cursors.Hand, Margin = new Thickness(0,0,0,10), BorderThickness = new Thickness(0) };
            openPdfBtn.Click += (s, e) => OpenActivePdf();
            pdfStack.Children.Add(openPdfBtn);

            var copyPathBtn = new Button { Content = "📋 Copy Path", Width = 150, Height = 30, Background = new SolidColorBrush(Color.FromArgb(80, 255,255,255)), Foreground = Brushes.White, Cursor = Cursors.Hand, BorderThickness = new Thickness(0), Margin = new Thickness(0,0,0,6) };
            copyPathBtn.Click += (s, e) => {
                if (!string.IsNullOrEmpty(_activePdfPath))
                {
                    Clipboard.SetText(Path.Combine(NotesManager.GetNotesDirectory(), _activePdfPath));
                    TextOverlay.Show("📋 Path copied to clipboard!", 2000);
                }
            };
            pdfStack.Children.Add(copyPathBtn);
            
            var deletePdfBtn = new Button { Content = "🗑️ Delete Book", Width = 150, Height = 30, Background = new SolidColorBrush(Color.FromRgb(150, 40, 40)), Foreground = Brushes.White, Cursor = Cursors.Hand, BorderThickness = new Thickness(0) };
            deletePdfBtn.Click += (s, e) => DeleteActivePdf();
            pdfStack.Children.Add(deletePdfBtn);
            
            _pdfViewerGrid.Children.Add(pdfStack);
            _pdfInfoLabel = pdfInfoLabel;  // store ref so LoadNote can update it
            Grid.SetRow(_pdfViewerGrid, 1); editorContainer.Children.Add(_pdfViewerGrid);

            _statusLabel = new TextBlock { Text = "Standing by.", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 8, 0, 0) };
            Grid.SetRow(_statusLabel, 2); editorContainer.Children.Add(_statusLabel);

            Grid.SetColumn(editorContainer, 1); mainGrid.Children.Add(editorContainer);

            this.UserContent = mainGrid;
            RefreshTree();
            if (state != null && !string.IsNullOrEmpty(state.LastNotePath)) LoadNote(state.LastNotePath);
        }

        private void RenderPreview()
        {
            _previewBox.Document.Blocks.Clear();
            string text = _editor.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            // Simple Markdown Parser
            var blocks = Regex.Split(text, @"(\n{2,})");
            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                var p = new Paragraph { Margin = new Thickness(0,0,0,10) };

                string line = block.Trim();
                if (line.StartsWith("# ")) {
                    p.Inlines.Add(new Run(line.Substring(2)) { FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan });
                } else if (line.StartsWith("## ")) {
                    p.Inlines.Add(new Run(line.Substring(3)) { FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.LightCyan });
                } else if (line.StartsWith("```") && line.EndsWith("```")) {
                    var codeBdr = new Border { Background = new SolidColorBrush(Color.FromRgb(15,15,15)), Padding = new Thickness(10), CornerRadius = new CornerRadius(4), Margin = new Thickness(0,5,0,5) };
                    codeBdr.Child = new TextBlock { Text = line.Trim('`').Trim(), FontFamily = new FontFamily("Consolas"), Foreground = Brushes.SpringGreen, FontSize = 12, TextWrapping = TextWrapping.Wrap };
                    _previewBox.Document.Blocks.Add(new BlockUIContainer(codeBdr));
                    continue;
                } else {
                    // Inline formatting
                    string pattern = @"(\*\*.*?\*\*|\*.*?\*|`.*?`)";
                    var parts = Regex.Split(line, pattern);
                    foreach (var part in parts) {
                        if (part.StartsWith("**") && part.EndsWith("**")) p.Inlines.Add(new Bold(new Run(part.Trim('*'))) { Foreground = Brushes.Cyan });
                        else if (part.StartsWith("*") && part.EndsWith("*")) p.Inlines.Add(new Italic(new Run(part.Trim('*'))));
                        else if (part.StartsWith("`") && part.EndsWith("`")) p.Inlines.Add(new Run(part.Trim('`')) { Background = Brushes.Black, FontFamily = new FontFamily("Consolas"), Foreground = Brushes.Lime });
                        else p.Inlines.Add(new Run(part));
                    }
                }
                _previewBox.Document.Blocks.Add(p);
            }
        }

        private void LoadNote(string relativePath)
        {
            _isChangingNote = true;

            if (relativePath.ToLower().EndsWith(".pdf"))
            {
                _activePdfPath = relativePath;
                _activeNoteRelativePath = null;
                _pdfBookTitleLabel!.Text = Path.GetFileNameWithoutExtension(relativePath);

                // Populate file info label
                if (_pdfInfoLabel != null)
                {
                    try
                    {
                        var fi = new FileInfo(Path.Combine(NotesManager.GetNotesDirectory(), relativePath));
                        string size = fi.Length < 1024 * 1024
                            ? $"{fi.Length / 1024.0:F1} KB"
                            : $"{fi.Length / (1024.0 * 1024):F2} MB";
                        _pdfInfoLabel.Text = $"{size}  ·  Modified {fi.LastWriteTime:MMM d, yyyy}";
                    }
                    catch { _pdfInfoLabel.Text = ""; }
                }
                _editorTabs.Visibility = Visibility.Collapsed;
                _pdfViewerGrid.Visibility = Visibility.Visible;
                _isChangingNote = false;
                _statusLabel.Text = $"PDF: {Path.GetFileName(relativePath)}";
                return;
            }

            _activePdfPath = null;
            _pdfViewerGrid.Visibility = Visibility.Collapsed;
            _editorTabs.Visibility = Visibility.Visible;

            _activeNoteRelativePath = relativePath;
            _editor.Text = NotesManager.LoadNote(relativePath);
            _isChangingNote = false;
            _statusLabel.Text = $"OPEN: {Path.GetFileName(relativePath)}";
            if (_editorTabs.SelectedIndex == 1) RenderPreview();
        }

        private void SaveActiveNote()
        {
            if (_activeNoteRelativePath != null) {
                NotesManager.SaveNote(_activeNoteRelativePath, _editor.Text);
                _statusLabel.Text = $"SAVED: {DateTime.Now:HH:mm:ss}";
            }
        }

        private void RefreshTree()
        {
            _treeView.Items.Clear();
            foreach (var item in NotesManager.GetHierarchy()) _treeView.Items.Add(CreateTreeViewItem(item));
        }

        private TreeViewItem CreateTreeViewItem(NoteItem item)
        {
            string icon = item.IS_FOLDER ? "📁 "
                : item.NAME.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "📚 "
                : "📝 ";
            var tvi = new TreeViewItem { Header = icon + item.NAME, Tag = item, Foreground = Brushes.White, FontSize = 13 };
            foreach (var child in item.CHILDREN) tvi.Items.Add(CreateTreeViewItem(child));
            return tvi;
        }

        private void CreateNewCategory() {
            string parent = (_treeView.SelectedItem is TreeViewItem tvi && tvi.Tag is NoteItem item && item.IS_FOLDER) ? item.RELATIVE_PATH : "";
            InputPromptOverlay.Show("Category Name:", (n) => { if (!string.IsNullOrEmpty(n)) { NotesManager.CreateCategory(parent, n); RefreshTree(); } });
        }

        private void CreateNewNote() {
            string parent = (_treeView.SelectedItem is TreeViewItem tvi && tvi.Tag is NoteItem item && item.IS_FOLDER) ? item.RELATIVE_PATH : "";
            InputPromptOverlay.Show("Note Title:", (n) => { if (!string.IsNullOrEmpty(n)) { string p = NotesManager.CreateNote(parent, n); RefreshTree(); LoadNote(Path.GetRelativePath(NotesManager.GetNotesDirectory(), p)); } });
        }

        private void DeleteSelected() {
            if (_treeView.SelectedItem is TreeViewItem tvi && tvi.Tag is NoteItem item) {
                if (MessageBox.Show($"Nuke '{item.NAME}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                    if (_activeNoteRelativePath == item.RELATIVE_PATH) _activeNoteRelativePath = null;
                    NotesManager.DeleteItem(item.RELATIVE_PATH); RefreshTree(); _editor.Text = "";
                }
            }
        }

        private void TriggerAiCuration()
        {
            _statusLabel.Text = "🤖 Initiating AI Notes Curation...";
            Task.Run(async () =>
            {
                try
                {
                    await NotesCuratorManager.PerformAutonomousCurationAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RefreshTree();
                        _statusLabel.Text = "🤖 Curation complete!";
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _statusLabel.Text = $"⚠️ Curation failed: {ex.Message}";
                    });
                }
            });
        }

        private void AskAiToHelpWithNote()
        {
            string selectedText = _editor.SelectedText;
            string textToProcess = string.IsNullOrEmpty(selectedText) ? _editor.Text : selectedText;

            if (string.IsNullOrEmpty(textToProcess))
            {
                TextOverlay.Show("⚠️ Note is empty. Type something first!", 3000);
                return;
            }

            InputPromptOverlay.Show("AI Prompt (e.g. summarize, expand, fix grammar):", async (instruction) =>
            {
                if (string.IsNullOrWhiteSpace(instruction)) return;

                Application.Current.Dispatcher.Invoke(() => {
                    _statusLabel.Text = "🤖 HEAPLIT is thinking...";
                });

                try
                {
                    string prompt = $"### USER INSTRUCTION\n{instruction}\n\n### TARGET TEXT\n{textToProcess}\n\n### TASK\nProcess the target text according to the user instruction. Return ONLY the processed text with no conversational filler or markdown wrapping blocks unless asked.";
                    string result = await LlmRouter.AskAsync(prompt);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (string.IsNullOrEmpty(selectedText))
                        {
                            _editor.Text = result;
                        }
                        else
                        {
                            _editor.SelectedText = result;
                        }
                        _statusLabel.Text = "🤖 AI edit applied!";
                        SaveActiveNote();
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _statusLabel.Text = $"⚠️ AI Error: {ex.Message}";
                    });
                }
            });
        }

        private void DownloadPdfFromUrlPrompt()
        {
            string parent = (_treeView.SelectedItem is TreeViewItem tvi && tvi.Tag is NoteItem item && item.IS_FOLDER) ? item.RELATIVE_PATH : "";

            InputPromptOverlay.Show("Enter PDF Book URL:", (url) =>
            {
                if (string.IsNullOrWhiteSpace(url)) return;

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) || 
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    TextOverlay.Show("⚠️ Invalid HTTP/HTTPS URL", 3000);
                    return;
                }

                string defaultName = Path.GetFileName(uriResult.LocalPath);
                if (string.IsNullOrWhiteSpace(defaultName) || !defaultName.ToLower().EndsWith(".pdf"))
                {
                    defaultName = "NewBook.pdf";
                }

                InputPromptOverlay.Show("Enter Book Filename (e.g. MyBook.pdf):", (filename) =>
                {
                    if (string.IsNullOrWhiteSpace(filename)) filename = defaultName;
                    if (!filename.ToLower().EndsWith(".pdf")) filename += ".pdf";

                    _statusLabel.Text = $"📥 Downloading PDF: {filename}...";

                    Task.Run(async () =>
                    {
                        try
                        {
                            using var client = new System.Net.Http.HttpClient();
                            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                            
                            byte[] pdfData = await client.GetByteArrayAsync(url);

                            string booksFolder = Path.Combine(NotesManager.GetNotesDirectory(), parent);
                            if (!Directory.Exists(booksFolder)) Directory.CreateDirectory(booksFolder);

                            string targetPath = Path.Combine(booksFolder, filename);
                            await File.WriteAllBytesAsync(targetPath, pdfData);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                RefreshTree();
                                _statusLabel.Text = $"✅ Downloaded PDF: {filename}";
                                TextOverlay.Show($"📚 PDF Book '{filename}' added successfully!", 3500);
                                LoadNote(Path.Combine(parent, filename));
                            });
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _statusLabel.Text = $"⚠️ Download failed: {ex.Message}";
                                TextOverlay.Show($"⚠️ PDF Download failed: {ex.Message}", 4000);
                            });
                        }
                    });
                }, defaultName);
            });
        }

        private void OpenActivePdf()
        {
            if (string.IsNullOrEmpty(_activePdfPath)) return;
            string fullPath = Path.Combine(NotesManager.GetNotesDirectory(), _activePdfPath);
            if (File.Exists(fullPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    TextOverlay.Show($"⚠️ Failed to open PDF: {ex.Message}", 3500);
                }
            }
        }

        private void DeleteActivePdf()
        {
            if (string.IsNullOrEmpty(_activePdfPath)) return;
            if (MessageBox.Show($"Are you sure you want to delete this PDF book permanently?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                NotesManager.DeleteItem(_activePdfPath);
                string deletedName = Path.GetFileName(_activePdfPath);
                _activePdfPath = null;
                _pdfViewerGrid!.Visibility = Visibility.Collapsed;
                _editorTabs.Visibility = Visibility.Visible;
                RefreshTree();
                _statusLabel.Text = $"DELETED: {deletedName}";
            }
        }

        private void AddLocalPdfPrompt()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title       = "Select a PDF to add to Notes Studio",
                Filter      = "PDF Files|*.pdf",
                Multiselect = true
            };

            if (dlg.ShowDialog() != true) return;

            string parent = (_treeView.SelectedItem is TreeViewItem tvi && tvi.Tag is NoteItem item && item.IS_FOLDER)
                ? item.RELATIVE_PATH : "";

            string destFolder = Path.Combine(NotesManager.GetNotesDirectory(), parent);
            Directory.CreateDirectory(destFolder);

            int copied = 0, skipped = 0;
            string lastImported = "";

            foreach (var sourcePath in dlg.FileNames)
            {
                string filename = Path.GetFileName(sourcePath);
                string dest = Path.Combine(destFolder, filename);

                // If same file is already there, ask once
                if (File.Exists(dest))
                {
                    var result = MessageBox.Show(
                        $"'{filename}' already exists in Notes. Overwrite?",
                        "File Exists", MessageBoxButton.YesNo);
                    if (result != MessageBoxResult.Yes) { skipped++; continue; }
                }

                try
                {
                    File.Copy(sourcePath, dest, overwrite: true);
                    lastImported = Path.Combine(parent, filename);
                    copied++;
                }
                catch (Exception ex)
                {
                    TextOverlay.Show($"⚠️ Failed to copy '{filename}': {ex.Message}", 4000);
                }
            }

            RefreshTree();

            if (copied > 0)
            {
                string msg = copied == 1
                    ? $"✅ Added '{Path.GetFileName(lastImported)}' to Notes"
                    : $"✅ Added {copied} PDF(s) to Notes";
                if (skipped > 0) msg += $" ({skipped} skipped)";
                _statusLabel.Text = msg;
                TextOverlay.Show(msg, 3000);
                if (!string.IsNullOrEmpty(lastImported)) LoadNote(lastImported);
            }
        }

        private Button CreateSmallButton(string text, string tip, RoutedEventHandler onClick) {
            var b = new Button { Content = text, ToolTip = tip, Width = 32, Height = 32, Margin = new Thickness(0, 0, 4, 0), Cursor = Cursors.Hand, Background = new SolidColorBrush(Color.FromArgb(40, 255,255,255)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            b.Click += onClick; return b;
        }

        private Button CreateFormatButton(string content, string tip, string prefix, string suffix) {
            var b = CreateSmallButton(content, tip, (s, e) => {
                var sel = _editor.SelectedText;
                _editor.SelectedText = prefix + sel + suffix;
                _editor.Focus();
            });
            return b;
        }
    }
}
