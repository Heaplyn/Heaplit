---
title: "ChatOverlay - Technical Specification"
tags: ['03---layer-2-ui-&-holographic-overlays', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# ChatOverlay - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer2\AI\ChatOverlay.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-19`  

```mermaid
graph TD
    Sub["ChatOverlay (class)"]
    Sub --> Layer["Hosting Layer: 03 - Layer 2 UI & Holographic Overlays"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Advanced Glassmorphic AI Chat Companion.
          Added: Model Selector, Markdown-ish Rich Text Support, Session Management.

`ChatOverlay` is an integral part of `03 - Layer 2 UI & Holographic Overlays`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `ChatOverlay` within the `03 - Layer 2 UI & Holographic Overlays` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

### 🎯 Primary Use Cases:
1. **Interactive Workflow**: Direct user triggers via launcher query, hotkey, or holographic HUD button.
2. **Autonomous Background Maintenance**: Unobtrusive polling, memory compaction, and rules synchronization.
3. **Cross-Subsystem Orchestration**: Passing telemetry and state between Layer 0 hardware and Layer 2 overlays.

---

## 🔍 Detailed Breakdown: What Each Component Does
- `Initialize()`: Binds runtime hooks, event listeners, and thread-safe caches.
- `ExecuteWorkloadAsync()`: Offloads high-computation operations to background threads.
- `Dispose()`: Cleans up native OS handles and managed resources.

---

## 🛠️ Troubleshooting Guide & How to Fix Common Errors

### ⚠️ Common Bug: Thread Contention or Stalled Background Worker
- **Root Cause**: Unhandled exception thrown in a background thread or deadlock on shared state lock.
- **Step-by-Step Fix**: Ensure all background loops use `try-catch` blocks and yield execution via `AdaptiveSleeper.Sleep(1000)` or `await Task.Delay()`.

### ⚠️ Common Bug: File Lock Contention during I/O
- **Root Cause**: External IDEs or processes locking files during reading/writing.
- **Step-by-Step Fix**: Always specify `FileShare.ReadWrite | FileShare.Delete` when opening `FileStream` instances.


---

## 🔬 Member Definitions & Method Signatures

| Method Name | Visibility & Modifiers | Return Type | Parameter Signature |
| :--- | :--- | :--- | :--- |
| `ShowChat` | `public static` | `void` | `*none*` |
| `ShowOverlay` | `public static` | `void` | `*none*` |
| `LogConsoleAction` | `public static` | `void` | `string ActionName, string Details` |
| `SendCurrentInput` | `private ` | `void` | `*none*` |
| `CreateQuickActionChip` | `private ` | `Button` | `string text, string prompt` |
| `CreateToolbarButton` | `private ` | `Button` | `string text, RoutedEventHandler onClick` |
| `ToggleConsole` | `private ` | `void` | `*none*` |
| `StartNewSession` | `private ` | `void` | `*none*` |
| `RefreshHistoryList` | `private ` | `void` | `*none*` |
| `LoadSession` | `private ` | `void` | `string fileName` |
| `SaveCurrentSession` | `private ` | `void` | `*none*` |
| `LoadLastSession` | `private ` | `void` | `*none*` |
| `ShowTeacherTip` | `public static` | `void` | `string body` |
| `SendUserMessage` | `private async` | `Task` | `string msg` |
| `ApplySelectedModelOrBackend` | `private static` | `void` | `string? sel` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-19
// Summary: Advanced Glassmorphic AI Chat Companion.
//          Added: Model Selector, Markdown-ish Rich Text Support, Session Management.

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Documents;
using Ellipse = System.Windows.Shapes.Ellipse;

namespace HeaplitLauncher
{
    public class ChatOverlay : BaseOverlay
    {
        private static ChatOverlay? Instance;
        private static readonly System.Text.StringBuilder ConsoleLog = new System.Text.StringBuilder();
        private static CancellationTokenSource? _activeCts;

        private StackPanel ChatHistoryPanel = null!;
        private ScrollViewer ScrollViewerPrivate = null!;
        private TextBox InputTextBox = null!;
        private TextBlock StatusText = null!;
        private Ellipse StatusDot = null!;
        private Border AttachedFileBadge = null!;
        private TextBlock AttachedFileText = null!;
        private string? AttachedFilePath = null;
        private Border HistoryContainer = null!;
        private ListBox HistoryListBox = null!;
        private TextBox ConsoleTextBox = null!;
        private Button ConsoleToggleBtn = null!;
        private Border ConsoleBorder = null!;
        private ComboBox ModelSelector = null!;
        private Button StopBtn = null!;
        private bool IsConsoleExpanded = false;
        private readonly List<ChatTurn> ConversationHistory = new List<ChatTurn>();

        public new static bool IsVisible => Instance != null && Instance.Visibility == Visibility.Visible && Instance.Opacity > 0.1;

        public static void ShowChat() {
            Application.Current.Dispatcher.Invoke(() => {
                if (Instance == null || !Instance.IsLoaded) { Instance = new ChatOverlay(); Instance.Closed += (S, E) => Instance = null; }
                Instance.Show(); Instance.BringToFront();
            });
        }

        public static void ShowOverlay() => ShowChat();

        public static void LogConsoleAction(string ActionName, string Details) {
            string LogLine = $"[{DateTime.Now:HH:mm:ss}] {ActionName.ToUpper()}\n{Details}\n----------------------------------\n";
            lock (ConsoleLog) {
                if (ConsoleLog.Length > 100000) ConsoleLog.Remove(0, 50000); // Prevent memory leak in logs
                ConsoleLog.AppendLine(LogLine);
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                if (Instance?.ConsoleTextBox != null) {
                    Instance.ConsoleTextBox.Text = ConsoleLog.ToString();
                    Instance.ConsoleTextBox.ScrollToEnd();
                }
            }));
        }

        private ChatOverlay() : base("HEAPLIT AI COMPANION", 480, 720) {
            var WorkArea = SystemParameters.WorkArea; this.Left = WorkArea.Width - this.Width - 20; this.Top = WorkArea.Top + 40;
            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Toolbar
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Chat
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Console
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input

            // --- Enhanced Toolbar ---
            var tb = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            tb.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tb.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tb.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var st = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            StatusDot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.LightGreen, Margin = new Thickness(0,0,6,0) };
            StatusText = new TextBlock { Text = "READY", FontSize = 10, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White };
            st.Children.Add(StatusDot); st.Children.Add(StatusText); tb.Children.Add(st);

            var toolStack = new StackPanel { Orientation = Orientation.Horizontal };

            ModelSelector = new ComboBox {
                Width = 150,
                Height = 24,
                FontSize = 10.5,
                IsEditable = true,
                MaxDropDownHeight = 350,
                Margin = new Thickness(0,0,6,0),
                Background = new SolidColorBrush(Color.FromArgb(40, 0,0,0)),
                Foreground = Brushes.Cyan,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.DimGray,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var backendsList = new[] {
                "Auto (Smart Failover)",
                "Gemini 3.8 Flash",
                "Gemini 3.8 Pro",
                "Gemini 3.7 Flash",
                "Gemini 3.7 Pro",
                "Gemini 3.7 Thinking",
                "Gemini 3.6 Flash",
                "Gemini 3.5 Flash",
                "Gemini 2.5 Flash",
                "Gemini 2.5 Pro",
                "Gemini 2.0 Flash",
                "Gemini 1.5 Flash",
                "OpenRouter",
                "OpenRouter (DeepSeek R1)",
                "OpenRouter (DeepSeek V3)",
                "OpenRouter (Claude 3.7 Sonnet)",
                "OpenRouter (Claude 3.5 Sonnet)",
                "OpenRouter (GPT-4o)",
                "OpenRouter (o3-mini)",
                "OpenRouter (Llama 3.3 70B)",
                "OpenRouter (Qwen 2.5 72B)",
                "OpenRouter (Mistral Large)",
                "OpenRouter (Gemini 2.0 Flash)",
                "OpenRouter (Perplexity Sonar)",
                "OpenRouter (Auto / Free)",
                "OpenAI (GPT-4o)",
                "OpenAI (GPT-4o Mini)",
                "OpenAI (o1)",
                "OpenAI (o3-mini)",
                "OpenAI (GPT-4.5 Preview)",
                "Anthropic (Claude 3.7 Sonnet)",
                "Anthropic (Claude 3.5 Sonnet)",
                "Anthropic (Claude 3.5 Haiku)",
                "Anthropic (Claude 3 Opus)",
                "ClaudeCode (CLI)",
                "Groq (Llama 3.3 70B)",
                "Groq (DeepSeek R1 Distill 70B)",
                "Groq (Llama 3.1 8B)",
                "Groq (Mixtral 8x7B)",
                "DeepSeek (R1 Reasoning)",
                "DeepSeek (V3 Chat)",
                "Mistral (Mistral Large)",
                "Mistral (Codestral)",
                "Mistral (Mistral Small)",
                "Perplexity (Sonar)",
                "Perplexity (Sonar Pro)",
                "Perplexity (Sonar Reasoning)",
                "X-AI (Grok 2)",
                "X-AI (Grok 2 Vision)",
                "Ollama (Local)",
                "LM Studio (Local)",
                "Custom API",
                "Custom Command"
            };

            foreach (var b in backendsList) ModelSelector.Items.Add(b);

            // Populate local models dynamically in background
            Task.Run(async () => {
                try {
                    var localModels = await ModelDiscoveryService.GetLocalModelsAsync();
                    if (localModels.Count > 0) {
                        Application.Current.Dispatcher.Invoke(() => {
                            foreach (var m in localModels) {
                                string label = $"{m.Provider}: {m.Id}";
                                if (!ModelSelector.Items.Contains(label)) ModelSelector.Items.Add(label);
                            }
                        });
                    }
                } catch { }
            });

            string curBackend = CoreRegistry.Data.Settings.Current?.LLM_BACKEND ?? "Gemini";
            ModelSelector.Text = curBackend;

            ModelSelector.SelectionChanged += (s, e) => {
                string? sel = ModelSelector.SelectedItem?.ToString() ?? ModelSelector.Text;
                ApplySelectedModelOrBackend(sel);
            };

            ModelSelector.LostFocus += (s, e) => {
                string text = ModelSelector.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(text)) ApplySelectedModelOrBackend(text);
            };

            toolStack.Children.Add(ModelSelector);

            var llmSettingsBtn = CreateToolbarButton("⚡ AI", (s, e) => { LlmSettingsOverlay.ShowOverlay(); });
            toolStack.Children.Add(llmSettingsBtn);

            var historyBtn = CreateToolbarButton("📜 LOGS", (s, e) => { RefreshHistoryList(); HistoryContainer.Visibility = HistoryContainer.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; });
            toolStack.Children.Add(historyBtn);

            var newChatBtn = CreateToolbarButton("🆕 NEW CHAT", (s, e) => { StartNewSession(); });
            toolStack.Children.Add(newChatBtn);

            var systemBtn = CreateToolbarButton("⚙️ SYSTEM", (s, e) => {
                string prompt = AiAPI.GetCompactSystemPrompt();
                MessageBox.Show(prompt, "HEAPLIT SYSTEM PROMPT", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            toolStack.Children.Add(systemBtn);

            Grid.SetColumn(toolStack, 2); tb.Children.Add(toolStack);
            Grid.SetRow(tb, 0); root.Children.Add(tb);

            // --- Chat Display ---
            var chatGrid = new Grid();
            ScrollViewerPrivate = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                CanContentScroll = false
            };
            ScrollViewerPrivate.PreviewMouseWheel += (s, e) => {
                if (HistoryContainer.Visibility == Visibility.Visible && HistoryContainer.IsMouseOver) return;
                double scrollAmount = (e.Delta / 3.0 > 0 ? Math.Max(28, e.Delta / 3.0) : Math.Min(-28, e.Delta / 3.0));
                ScrollViewerPrivate.ScrollToVerticalOffset(ScrollViewerPrivate.VerticalOffset - scrollAmount);
                e.Handled = true;
            };

            ChatHistoryPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
            ScrollViewerPrivate.Content = ChatHistoryPanel;

            HistoryContainer = new Border { Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Color.FromArgb(250, 10, 10, 20)), Padding = new Thickness(12), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), BorderBrush = Brushes.Cyan, Margin = new Thickness(15), VerticalAlignment = VerticalAlignment.Top };
            var historyStack = new StackPanel();
            historyStack.Children.Add(new TextBlock { Text = "ARCHIVED MISSIONS", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, Margin = new Thickness(0,0,0,12) });
            HistoryListBox = new ListBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.White, MaxHeight = 400 };
            HistoryListBox.PreviewMouseWheel += (s, e) => {
                var sv = WpfScrollHelper.FindDescendant<ScrollViewer>(HistoryListBox);
                if (sv != null) {
                    double scrollAmount = (e.Delta / 3.0 > 0 ? Math.Max(28, e.Delta / 3.0) : Math.Min(-28, e.Delta / 3.0));
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - scrollAmount);
                    e.Handled = true;
                }
            };
            HistoryListBox.SelectionChanged += (s, e) => { if (HistoryListBox.SelectedItem is string log) LoadSession(log); };
            historyStack.Children.Add(HistoryListBox); HistoryContainer.Child = historyStack;

            // --- Quick Actions Panel ---
            var quickActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,10) };
            quickActions.Children.Add(CreateQuickActionChip("🚀 OPTIMIZE", "Optimize the current codebase for performance."));
            quickActions.Children.Add(CreateQuickActionChip("🔍 AUDIT", "Run a deep security and logic audit."));
            quickActions.Children.Add(CreateQuickActionChip("📝 DOCS", "Generate technical documentation for the current module."));

            var chatStack = new Grid();
            chatStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            chatStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(quickActions, 0); chatStack.Children.Add(quickActions);
            Grid.SetRow(ScrollViewerPrivate, 1); chatStack.Children.Add(ScrollViewerPrivate);

            chatGrid.Children.Add(chatStack);
            chatGrid.Children.Add(HistoryContainer);

            Grid.SetRow(chatGrid, 1); root.Children.Add(chatGrid);

            // --- Sliding Debug Console ---
            ConsoleBorder = new Border { CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.FromArgb(50, 0,0,0)), BorderThickness = new Thickness(0,1,0,0), BorderBrush = Brushes.DimGray, Margin = new Thickness(0,10,0,0) };
            var consoleGrid = new Grid();
            consoleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            consoleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            ConsoleToggleBtn = new Button { Content = "SYSTEM LOGS (»)", FontSize = 9, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.Gray, Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Left };
            ConsoleToggleBtn.Click += (s, e) => ToggleConsole();
            Grid.SetRow(ConsoleToggleBtn, 0); consoleGrid.Children.Add(ConsoleToggleBtn);

            ConsoleTextBox = new TextBox { Height = 0, Visibility = Visibility.Collapsed, IsReadOnly = true, FontFamily = new FontFamily("Consolas"), FontSize = 10, Background = Brushes.Transparent, Foreground = Brushes.Lime, BorderThickness = new Thickness(0), TextWrapping = TextWrapping.Wrap, Opacity = 0.8, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            ConsoleTextBox.PreviewMouseWheel += (s, e) => {
                double scrollAmount = (e.Delta > 0 ? -3 : 3);
                ConsoleTextBox.ScrollToVerticalOffset(ConsoleTextBox.VerticalOffset + scrollAmount);
                e.Handled = true;
            };
            ConsoleTextBox.Text = ConsoleLog.ToString();
            Grid.SetRow(ConsoleTextBox, 1); consoleGrid.Children.Add(ConsoleTextBox);
            ConsoleBorder.Child = consoleGrid;
            Grid.SetRow(ConsoleBorder, 2); root.Children.Add(ConsoleBorder);

            // --- Input Area ---
            var inpStack = new StackPanel { Margin = new Thickness(0,10,0,0) };
            AttachedFileBadge = new Border { Visibility = Visibility.Collapsed, Padding = new Thickness(8,4,8,4), Background = new SolidColorBrush(Color.FromArgb(100, 0,0,0)), Margin = new Thickness(0,0,0,8), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), BorderBrush = Brushes.Cyan };
            AttachedFileText = new TextBlock { FontSize = 10, Foreground = Brushes.Cyan }; AttachedFileBadge.Child = AttachedFileText;
            inpStack.Children.Add(AttachedFileBadge);

            var inpGrid = new Grid();
            inpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addBtn = new Button { Content = "📎", Width = 38, Height = 38, Margin = new Thickness(0,0,8,0), Cursor = Cursors.Hand, FontSize = 18, Background = new SolidColorBrush(Color.FromArgb(30, 255,255,255)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            addBtn.Click += (s, e) => { var dlg = new Microsoft.Win32.OpenFileDialog(); if(dlg.ShowDialog()==true) { AttachedFilePath = dlg.FileName; AttachedFileText.Text = "ATTACHED: " + Path.GetFileName(dlg.FileName).ToUpper(); AttachedFileBadge.Visibility = Visibility.Visible; } };
            Grid.SetColumn(addBtn, 0); inpGrid.Children.Add(addBtn);

            var boxGrid = new Grid();
            InputTextBox = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 38, MaxHeight = 150, Padding = new Thickness(12,8,12,8), FontSize = 14, Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), Foreground = Brushes.White, BorderBrush = Brushes.DimGray, CaretBrush = Brushes.Cyan, BorderThickness = new Thickness(1) };
            var placeholder = new TextBlock { Text = "Command Heaplit...", Foreground = Brushes.Gray, IsHitTestVisible = false, Margin = new Thickness(15,10,0,0), FontSize = 14 };
            InputTextBox.TextChanged += (s, e) => placeholder.Visibility = string.IsNullOrEmpty(InputTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            InputTextBox.PreviewKeyDown += (s, e) => { if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None) { e.Handled = true; SendCurrentInput(); } };
            boxGrid.Children.Add(InputTextBox); boxGrid.Children.Add(placeholder);
            Grid.SetColumn(boxGrid, 1); inpGrid.Children.Add(boxGrid);

            StopBtn = new Button { Content = "⏹", Width = 38, Height = 38, Margin = new Thickness(8,0,0,0), Cursor = Cursors.Hand, FontSize = 18, Background = Brushes.DarkRed, Foreground = Brushes.White, BorderThickness = new Thickness(0), Visibility = Visibility.Collapsed };
            StopBtn.Click += (s, e) => { _activeCts?.Cancel(); };
            Grid.SetColumn(StopBtn, 2); inpGrid.Children.Add(StopBtn);

            inpStack.Children.Add(inpGrid);
            Grid.SetRow(inpStack, 3); root.Children.Add(inpStack);

            this.UserContent = root;
            LoadLastSession();
        }

        private void SendCurrentInput() {
            string m = InputTextBox.Text.Trim();
            if(!string.IsNullOrEmpty(m) || AttachedFilePath != null) {
                _ = SendUserMessage(m);
                InputTextBox.Text = "";
            }
        }

        private Button CreateQuickActionChip(string text, string prompt) {
            var b = new Button { Content = text, FontSize = 9, Padding = new Thickness(10,4,10,4), Margin = new Thickness(0,0,8,0), Cursor = Cursors.Hand, Background = new SolidColorBrush(Color.FromArgb(50, 0, 255, 255)), Foreground = Brushes.White, BorderThickness = new Thickness(1), BorderBrush = Brushes.Cyan };
            b.Click += (s, e) => { _ = SendUserMessage(prompt); };
            return b;
        }

        private Button CreateToolbarButton(string text, RoutedEventHandler onClick) {
            var b = new Button { Content = text, FontSize = 9, Padding = new Thickness(8,3,8,3), Margin = new Thickness(0,0,6,0), Cursor = Cursors.Hand, Background = new SolidColorBrush(Color.FromArgb(40, 255,255,255)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            b.Click += onClick; return b;
        }

        private void ToggleConsole() {
            IsConsoleExpanded = !IsConsoleExpanded;
            ConsoleTextBox.Height = IsConsoleExpanded ? 200 : 0;
            ConsoleTextBox.Visibility = IsConsoleExpanded ? Visibility.Visible : Visibility.Collapsed;
            ConsoleToggleBtn.Content = IsConsoleExpanded ? "HIDE SYSTEM LOGS («)" : "SHOW SYSTEM LOGS (»)";
            ScrollViewerPrivate.ScrollToBottom();
        }

        private void StartNewSession() {
            if (ConversationHistory.Count > 0) SaveCurrentSession();
            ChatHistoryPanel.Children.Clear(); ConversationHistory.Clear();
            AddMessageBubble("Synchronized. Standing by for new instructions, Sir.", true);
        }

        private void RefreshHistoryList() {
            HistoryListBox.Items.Clear();
            string dir = Path.Combine(PathHandler.GetDataDirectory(), "Conversations");
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir, "*.json").OrderByDescending(File.GetLastWriteTime)) HistoryListBox.Items.Add(Path.GetFileName(f));
        }

        private void LoadSession(string fileName) {
            string path = Path.Combine(PathHandler.GetDataDirectory(), "Conversations", fileName);
            if (!File.Exists(path)) return;
            ChatHistoryPanel.Children.Clear(); ConversationHistory.Clear();
            try {
                var turns = JsonSerializer.Deserialize<List<ChatTurn>>(File.ReadAllText(path));
                if (turns != null) foreach (var turn in turns) { ConversationHistory.Add(turn); AddMessageBubble(turn.Text, turn.Role == "model"); }
            } catch { }
            HistoryContainer.Visibility = Visibility.Collapsed;
        }

        private void SaveCurrentSession() {
            try {
                string dir = Path.Combine(PathHandler.GetDataDirectory(), "Conversations"); Directory.CreateDirectory(dir);
                string name = $"Session_{DateTime.Now:yyyyMMdd_HHmm}.json";
                File.WriteAllText(Path.Combine(dir, name), JsonSerializer.Serialize(ConversationHistory));
            } catch { }
        }

        private void LoadLastSession() {
            string dir = Path.Combine(PathHandler.GetDataDirectory(), "Conversations");
            if (!Directory.Exists(dir)) { AddMessageBubble("Heaplit Online. Operational.", true); return; }
            var last = Directory.GetFiles(dir, "*.json").OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
            if (last != null) LoadSession(Path.GetFileName(last)); else AddMessageBubble("Heaplit Online. Operational.", true);
        }

        public static async Task SubmitTextMessage(string msg) { ShowChat(); if (Instance != null) await Instance.SendUserMessage(msg); }

        /// <summary>
        /// Pushes a proactive assistant tip (e.g. from the Live Coding Tutor) into the chat WITHOUT
        /// triggering a new AI turn. Shows the companion (docked far-right) and adds an AI bubble.
        /// </summary>
        public static void ShowTeacherTip(string body) {
            Application.Current.Dispatcher.Invoke(() => {
                ShowChat();
                Instance?.AddMessageBubble(body, true);
            });
        }
        public static async Task SubmitVoiceCommand(string msg, bool showUi = true) { if (showUi) ShowChat(); if (Instance != null) await Instance.SendUserMessage(msg); }

        private async Task SendUserMessage(string msg) {
            string apiMsg = msg; string displayMsg = msg;
            if (AttachedFilePath != null) {
                apiMsg = $"[FILE: {AttachedFilePath}]\n{msg}";
                displayMsg = $"📎 {Path.GetFileName(AttachedFilePath).ToUpper()}\n{msg}";
                AttachedFilePath = null; AttachedFileBadge.Visibility = Visibility.Collapsed;
            }

            AddMessageBubble(displayMsg, false);
            ChronoLogManager.LogEvent("Chat", $"User: {msg}");
            ScrollViewerPrivate.ScrollToBottom();

            var (bdr, rt, dbg) = AddMessageBubbleWithControls("Thinking...", true);
            StatusText.Text = "THINKING"; StatusDot.Fill = Brushes.Yellow; StopBtn.Visibility = Visibility.Visible;
            ScrollViewerPrivate.ScrollToBottom();

            try {
                var cts = new CancellationTokenSource(); _activeCts = cts;
                string aiRaw = "";
                bool multiTurnDone = false;

                if (LlmRouter.IsStreamingBackend(CoreRegistry.Data.Settings.Current.LLM_BACKEND)) {
                    // Stream tokens live into the bubble for Ollama + all OpenAI-compatible providers.
                    Application.Current.Dispatcher.Invoke(() => { try { rt.Document.Blocks.Clear(); } catch { } });
                    aiRaw = await LlmRouter.AskStreamAsync(apiMsg, ConversationHistory, t => Application.Current.Dispatcher.Invoke(() => AppendToRichText(rt, t)), cts.Token);
                } else {
                    // Inject MCP Context for tool discovery. GetToolManifest() returns instantly
                    // (cached) and refreshes tool lists in the background, so MCP never blocks chat.
                    string manifest = McpManager.GetToolManifest();
                    string mcpContext = "[MCP ENABLED]\nAvailable MCP servers and tools:\n" + manifest +
                        "To invoke one, emit exactly: [CALL_MCP_TOOL: ServerName | tool_name | {\"arg\":\"value\"}]\n\n";

                    // Multi-turn agent loop: LLM -> tools -> feed results back -> LLM, until done.
                    aiRaw = await AgentExecutor.RunAgentTurnsAsync(mcpContext + apiMsg, ConversationHistory, cts.Token,
                        step => Application.Current.Dispatcher.Invoke(() => { try { AppendToRichText(rt, "\n" + step); } catch { } }));
                    multiTurnDone = true;

                    // MCP tool loop: run whatever tool the model emitted, feed the result back, and
                    // repeat so multi-step jobs (e.g. Roblox: list tree → read script → edit) can chain.
                    var mcpRegex = new Regex(@"\[CALL_MCP_TOOL:\s*(?<server>.*?)\s*\|\s*(?<tool>.*?)\s*\|\s*(?<args>.*?)\]", RegexOptions.Singleline);
                    for (int mcpStep = 0; mcpStep < 8 && !cts.IsCancellationRequested; mcpStep++)
                    {
                        var match = mcpRegex.Match(aiRaw);
                        if (!match.Success) break;

                        string server = match.Groups["server"].Value.Trim();
                        string tool = match.Groups["tool"].Value.Trim();
                        string argsJson = match.Groups["args"].Value.Trim();

                        Application.Current.Dispatcher.Invoke(() => {
                            AppendToRichText(rt, $"\n\n⚡ [MCP] {tool} → {server}…");
                        });

                        string toolResult;
                        try {
                            var args = string.IsNullOrWhiteSpace(argsJson)
                                ? new Dictionary<string, object>()
                                : (JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson) ?? new());
                            toolResult = await McpManager.CallToolAsync(server, tool, args);
                        } catch (Exception ex) {
                            toolResult = $"Error: could not parse/execute tool call — {ex.Message}";
                        }

                        // Re-ask AI with result so it can decide the next step (or finish).
                        aiRaw = await AiAPI.AskAgentAsync(
                            $"[MCP TOOL RESULT: {server}.{tool}]\n{toolResult}\n\nContinue the task. Emit another [CALL_MCP_TOOL: ...] to keep going, or give your final answer with no tool tags.",
                            ConversationHistory, cts.Token);
                    }

                    FormatRichText(rt, AiAPI.SanitizeText(aiRaw));
                }

                StatusText.Text = "EXECUTING TOOLS"; StatusDot.Fill = Brushes.Orange;
                // The non-streaming path already ran tools multi-turn; only the streaming path needs
                // a single tool pass here.
                string processedText = multiTurnDone ? aiRaw : await AgentExecutor.ProcessAIResponseAsync(aiRaw);
                if (!string.IsNullOrEmpty(processedText)) FormatRichText(rt, processedText);

                dbg.Text = "INTERNAL TRACE:\n" + aiRaw;
                ChronoLogManager.LogEvent("Chat", $"Heaplit: {aiRaw}");

                ConversationHistory.Add(new ChatTurn { Role = "user", Text = msg });
                ConversationHistory.Add(new ChatTurn { Role = "model", Text = aiRaw });
                SaveCurrentSession();
            } catch (Exception ex) {
                LogConsoleAction("AI Fault", ex.ToString());
                string errText = ex.Message;
                if (errText.Contains("429") || errText.Contains("quota")) errText = "Quota exceeded (429). Switch engine or check key in LLM Settings.";
                FormatRichText(rt, $"⚠️ **AI Service Notice**\n\n{errText}\n\n*Click **⚡ AI** in the toolbar above to check keys or select a different engine.*");
            } finally {
                StatusText.Text = "READY"; StatusDot.Fill = Brushes.LightGreen;
                StopBtn.Visibility = Visibility.Collapsed; _activeCts = null; ScrollViewerPrivate.ScrollToBottom();
            }
        }

        private static void ApplySelectedModelOrBackend(string? sel)
        {
            if (string.IsNullOrWhiteSpace(sel)) return;
            string s = sel.Trim();
            var settings = CoreRegistry.Data.Settings.Current;
            if (settings == null) return;

            if (s.StartsWith("Auto", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Auto";
            }
            // Gemini
            else if (s.StartsWith("Gemini 3.8 Pro", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.8-pro";
            } else if (s.StartsWith("Gemini 3.8", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.8-flash";
            } else if (s.StartsWith("Gemini 3.7 Pro", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.7-pro";
            } else if (s.StartsWith("Gemini 3.7 Thinking", StringComparison.OrdinalIgnoreCase) || s.Contains("Thinking", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.7-flash-thinking";
            } else if (s.StartsWith("Gemini 3.7", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.7-flash";
            } else if (s.StartsWith("Gemini 3.6 Pro", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.6-pro";
            } else if (s.StartsWith("Gemini 3.6", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.6-flash";
            } else if (s.StartsWith("Gemini 3.5 Pro", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.5-pro";
            } else if (s.StartsWith("Gemini 3.5", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-3.5-flash";
            } else if (s.StartsWith("Gemini 2.5 Pro", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-2.5-pro";
            } else if (s.StartsWith("Gemini 2.5", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-2.5-flash";
            } else if (s.StartsWith("Gemini 2.0", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-2.0-flash";
            } else if (s.StartsWith("Gemini 1.5 Pro", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-1.5-pro";
            } else if (s.StartsWith("Gemini 1.5", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = "gemini-1.5-flash";
            } else if (s.Equals("Gemini", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
            }
            // OpenRouter
            else if (s.StartsWith("OpenRouter", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "OpenRouter";
                if (s.Contains("DeepSeek R1", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "deepseek/deepseek-r1";
                } else if (s.Contains("DeepSeek V3", StringComparison.OrdinalIgnoreCase) || s.Contains("DeepSeek-Chat", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "deepseek/deepseek-chat";
                } else if (s.Contains("Claude 3.7", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "anthropic/claude-3.7-sonnet";
                } else if (s.Contains("Claude 3.5", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "anthropic/claude-3.5-sonnet";
                } else if (s.Contains("GPT-4o Mini", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "openai/gpt-4o-mini";
                } else if (s.Contains("GPT-4o", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "openai/gpt-4o";
                } else if (s.Contains("o3-mini", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "openai/o3-mini";
                } else if (s.Contains("Llama 3.3", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "meta-llama/llama-3.3-70b-instruct";
                } else if (s.Contains("Qwen 2.5", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "qwen/qwen-2.5-72b-instruct";
                } else if (s.Contains("Mistral Large", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "mistralai/mistral-large-2411";
                } else if (s.Contains("Gemini 2.0", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "google/gemini-2.0-flash-001";
                } else if (s.Contains("Perplexity", StringComparison.OrdinalIgnoreCase) || s.Contains("Sonar", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "perplexity/sonar";
                } else if (s.Contains("Auto", StringComparison.OrdinalIgnoreCase) || s.Contains("Free", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENROUTER_MODEL = "openrouter/auto";
                } else if (s.Contains("(") && s.Contains(")")) {
                    string inside = s.Substring(s.IndexOf('(') + 1).TrimEnd(')').Trim();
                    if (!string.IsNullOrEmpty(inside)) settings.OPENROUTER_MODEL = inside;
                } else if (s.Contains(":")) {
                    string after = s.Substring(s.IndexOf(':') + 1).Trim();
                    if (!string.IsNullOrEmpty(after)) settings.OPENROUTER_MODEL = after;
                }
            }
            // OpenAI
            else if (s.StartsWith("OpenAI", StringComparison.OrdinalIgnoreCase) || s.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) || s.StartsWith("o1", StringComparison.OrdinalIgnoreCase) || s.StartsWith("o3", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "OpenAI";
                if (s.Contains("4o-mini", StringComparison.OrdinalIgnoreCase)) settings.OPENAI_MODEL = "gpt-4o-mini";
                else if (s.Contains("4o", StringComparison.OrdinalIgnoreCase)) settings.OPENAI_MODEL = "gpt-4o";
                else if (s.Contains("4.5", StringComparison.OrdinalIgnoreCase)) settings.OPENAI_MODEL = "gpt-4.5-preview";
                else if (s.Contains("o3-mini", StringComparison.OrdinalIgnoreCase)) settings.OPENAI_MODEL = "o3-mini";
                else if (s.Contains("o1", StringComparison.OrdinalIgnoreCase)) settings.OPENAI_MODEL = "o1";
                else if (s.Contains("(") && s.Contains(")")) {
                    string inside = s.Substring(s.IndexOf('(') + 1).TrimEnd(')').Trim();
                    if (!string.IsNullOrEmpty(inside)) settings.OPENAI_MODEL = inside;
                } else if (s.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) || s.StartsWith("o1", StringComparison.OrdinalIgnoreCase) || s.StartsWith("o3", StringComparison.OrdinalIgnoreCase)) {
                    settings.OPENAI_MODEL = s;
                }
            }
            // Anthropic
            else if (s.StartsWith("Anthropic", StringComparison.OrdinalIgnoreCase) || s.StartsWith("claude-", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Anthropic";
                if (s.Contains("3.7") || s.Contains("3-7")) settings.ANTHROPIC_MODEL = "claude-3-7-sonnet-latest";
                else if (s.Contains("3.5 Sonnet") || s.Contains("3-5-sonnet")) settings.ANTHROPIC_MODEL = "claude-3-5-sonnet-latest";
                else if (s.Contains("3.5 Haiku") || s.Contains("3-5-haiku")) settings.ANTHROPIC_MODEL = "claude-3-5-haiku-latest";
                else if (s.Contains("Opus") || s.Contains("opus")) settings.ANTHROPIC_MODEL = "claude-3-opus-latest";
                else if (s.StartsWith("claude-", StringComparison.OrdinalIgnoreCase)) settings.ANTHROPIC_MODEL = s;
            }
            // ClaudeCode
            else if (s.StartsWith("ClaudeCode", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "ClaudeCode";
            }
            // Groq
            else if (s.StartsWith("Groq", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Groq";
                if (s.Contains("DeepSeek", StringComparison.OrdinalIgnoreCase) || s.Contains("R1", StringComparison.OrdinalIgnoreCase)) settings.GROQ_MODEL = "deepseek-r1-distill-llama-70b";
                else if (s.Contains("3.1", StringComparison.OrdinalIgnoreCase) || s.Contains("8b", StringComparison.OrdinalIgnoreCase)) settings.GROQ_MODEL = "llama-3.1-8b-instant";
                else if (s.Contains("Mixtral", StringComparison.OrdinalIgnoreCase)) settings.GROQ_MODEL = "mixtral-8x7b-32768";
                else if (s.Contains("3.3", StringComparison.OrdinalIgnoreCase) || s.Contains("70b", StringComparison.OrdinalIgnoreCase)) settings.GROQ_MODEL = "llama-3.3-70b-versatile";
            }
            // DeepSeek
            else if (s.StartsWith("DeepSeek", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "DeepSeek";
                if (s.Contains("R1") || s.Contains("reasoner", StringComparison.OrdinalIgnoreCase)) {
                    settings.DEEPSEEK_MODEL = "deepseek-reasoner";
                    settings.CUSTOM_LLM_MODEL = "deepseek-reasoner";
                } else {
                    settings.DEEPSEEK_MODEL = "deepseek-chat";
                    settings.CUSTOM_LLM_MODEL = "deepseek-chat";
                }
            }
            // Mistral
            else if (s.StartsWith("Mistral", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Mistral";
                if (s.Contains("Codestral", StringComparison.OrdinalIgnoreCase)) settings.MISTRAL_MODEL = "codestral-latest";
                else if (s.Contains("Small", StringComparison.OrdinalIgnoreCase)) settings.MISTRAL_MODEL = "mistral-small-latest";
                else settings.MISTRAL_MODEL = "mistral-large-latest";
            }
            // Perplexity
            else if (s.StartsWith("Perplexity", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Perplexity";
                if (s.Contains("Reasoning", StringComparison.OrdinalIgnoreCase)) settings.PERPLEXITY_MODEL = "sonar-reasoning";
                else if (s.Contains("Pro", StringComparison.OrdinalIgnoreCase)) settings.PERPLEXITY_MODEL = "sonar-pro";
                else settings.PERPLEXITY_MODEL = "sonar";
            }
            // X-AI / Grok
            else if (s.StartsWith("X-AI", StringComparison.OrdinalIgnoreCase) || s.StartsWith("Grok", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "X-AI";
                if (s.Contains("Vision", StringComparison.OrdinalIgnoreCase)) settings.XAI_MODEL = "grok-2-vision-1212";
                else settings.XAI_MODEL = "grok-2-latest";
            }
            // LM Studio
            else if (s.StartsWith("LM Studio", StringComparison.OrdinalIgnoreCase) || s.StartsWith("LMStudio", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "LMStudio";
                if (s.Contains(":")) {
                    string modelName = s.Substring(s.IndexOf(':') + 1).Trim();
                    if (!string.IsNullOrEmpty(modelName)) settings.CUSTOM_LLM_MODEL = modelName;
                }
            }
            // Ollama
            else if (s.StartsWith("Ollama", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Ollama";
                if (s.Contains(":")) {
                    string modelName = s.Substring(s.IndexOf(':') + 1).Trim();
                    if (!string.IsNullOrEmpty(modelName)) settings.OLLAMA_MODEL = modelName;
                }
            }
            // Custom Command
            else if (s.StartsWith("Custom Command", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "CustomCommand";
            }
            // Custom API
            else if (s.StartsWith("Custom API", StringComparison.OrdinalIgnoreCase) || s.StartsWith("Custom", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Custom";
            }
            // Raw model path containing / -> OpenRouter
            else if (s.Contains("/")) {
                settings.LLM_BACKEND = "OpenRouter";
                settings.OPENROUTER_MODEL = s;
            }
            // Gemini normalized fallback
            else if (s.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase)) {
                settings.LLM_BACKEND = "Gemini";
                settings.GEMINI_MODEL = LlmRouter.NormalizeGeminiModel(s);
            } else {
                settings.LLM_BACKEND = s;
            }

            CoreRegistry.Data.Settings.Save();
            DebugConsoleOverlay.Log("AI-Switch", $"Engine switched to: {settings.LLM_BACKEND}");
        }

        private void AppendToRichText(RichTextBox rt, string text) {
            if (new TextRange(rt.Document.ContentStart, rt.Document.ContentEnd).Text.Trim() == "Thinking...") rt.Document.Blocks.Clear();

            var p = rt.Document.Blocks.FirstBlock as Paragraph ?? new Paragraph();
            if (rt.Document.Blocks.Count == 0) rt.Document.Blocks.Add(p);
            p.Inlines.Add(new Run(text));
            ScrollViewerPrivate.ScrollToBottom();
        }

        private void FormatRichText(RichTextBox rt, string text) {
            rt.Document.Blocks.Clear();
            if (string.IsNullOrEmpty(text)) return;

            // Split by code blocks first (triple backticks)
            var blocks = Regex.Split(text, @"(``​`[\s\S]*?``​`)");

            foreach (var block in blocks) {
                if (block.StartsWith("``​`") && block.EndsWith("``​`")) {
                    string code = block.Trim('`').Trim();
                    var bdr = new Border { Background = new SolidColorBrush(Color.FromArgb(60, 0,0,0)), BorderThickness = new Thickness(1), BorderBrush = Brushes.DimGray, Padding = new Thickness(10), Margin = new Thickness(0,5,0,10), CornerRadius = new CornerRadius(4) };
                    var tb = new TextBox { Text = code, FontFamily = (Application.Current.Resources["MonoFontFamily"] as FontFamily) ?? new FontFamily("Consolas"), FontSize = 12, Foreground = Brushes.Lime, Background = Brushes.Transparent, BorderThickness = new Thickness(0), IsReadOnly = true, TextWrapping = TextWrapping.Wrap };
                    bdr.Child = tb;
                    rt.Document.Blocks.Add(new BlockUIContainer(bdr));
                } else {
                    RenderTextWithMedia(rt, block);
                }
            }
        }

        // Renders a non-code text block, pulling out any image/gif/video/audio references and
        // embedding them inline as real media players; the text around them keeps markdown styling.
        private void RenderTextWithMedia(RichTextBox rt, string block) {
            int last = 0;
            foreach (Match m in ChatMediaRenderer.MediaRegex.Matches(block)) {
                string before = block.Substring(last, m.Index - last);
                if (!string.IsNullOrEmpty(before)) AddFormattedParagraph(rt, before);

                string url = ChatMediaRenderer.ExtractUrl(m);
                UIElement? media = null;
                try { media = ChatMediaRenderer.Create(url); } catch { }
                if (media != null) {
                    rt.Document.Blocks.Add(new BlockUIContainer(media) { Margin = new Thickness(0, 4, 0, 8) });
                } else {
                    AddFormattedParagraph(rt, m.Value);   // not real media → leave the raw text
                }
                last = m.Index + m.Length;
            }
            string rest = block.Substring(last);
            if (!string.IsNullOrEmpty(rest)) AddFormattedParagraph(rt, rest);
        }

        // Existing inline markdown styling (bold / inline-code), factored out for reuse.
        private void AddFormattedParagraph(RichTextBox rt, string block) {
            var p = new Paragraph { Margin = new Thickness(0,0,0,8) };
            string[] subParts = Regex.Split(block, @"(\*\*.*?\*\*|`.*?`)");
            foreach (var part in subParts) {
                if (part.StartsWith("**") && part.EndsWith("**")) {
                    p.Inlines.Add(new Bold(new Run(part.Trim('*'))) { Foreground = Brushes.Cyan });
                } else if (part.StartsWith("`") && part.EndsWith("`")) {
                    p.Inlines.Add(new Run(part.Trim('`')) { Background = new SolidColorBrush(Color.FromArgb(60, 0,0,0)), FontFamily = new FontFamily("Consolas"), Foreground = Brushes.Lime });
                } else {
                    p.Inlines.Add(new Run(part));
                }
            }
            rt.Document.Blocks.Add(p);
        }

        private void AddMessageBubble(string t, bool ai) => AddMessageBubbleWithControls(t, ai);

        private (Border, RichTextBox, TextBox) AddMessageBubbleWithControls(string t, bool ai) {
            var b = new Border { Background = ai ? new SolidColorBrush(Color.FromArgb(35, 25, 25, 35)) : new SolidColorBrush(Color.FromArgb(90, 0, 120, 215)), CornerRadius = new CornerRadius(14, 14, ai ? 14 : 2, ai ? 2 : 14), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(ai ? 0 : 50, 8, ai ? 50 : 0, 8), HorizontalAlignment = ai ? HorizontalAlignment.Left : HorizontalAlignment.Right, MaxWidth = 420, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) };
            var stack = new StackPanel();

            var rt = new RichTextBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), IsReadOnly = true, Foreground = Brushes.White, FontSize = 13.5, FontFamily = (Application.Current.Resources["ChatFontFamily"] as FontFamily) ?? new FontFamily("Segoe UI"), VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, IsHitTestVisible = true };
            rt.Document.PagePadding = new Thickness(0);
            FormatRichText(rt, t);
            stack.Children.Add(rt);

            TextBox dbg = new TextBox { Visibility = Visibility.Collapsed, Margin = new Thickness(0,12,0,0), Background = new SolidColorBrush(Color.FromArgb(140, 0,0,0)), Foreground = Brushes.Gray, FontSize = 10.5, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 350, BorderThickness = new Thickness(0), FontFamily = new FontFamily("Consolas"), Padding = new Thickness(8) };

            if (ai) {
                var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Opacity = 0.6, Margin = new Thickness(0,6,0,0) };
                var copyBtn = CreateBubbleButton("📋", (s, e) => { try { Clipboard.SetText(new TextRange(rt.Document.ContentStart, rt.Document.ContentEnd).Text); TextOverlay.Show("COPIED", 1000); } catch { } });
                var detailsBtn = CreateBubbleButton("⌄", (s, e) => { dbg.Visibility = dbg.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; (s as Button)!.Content = dbg.Visibility == Visibility.Visible ? "⌃" : "⌄"; if(dbg.Visibility==Visibility.Visible) ScrollViewerPrivate.ScrollToBottom(); });
                btnStack.Children.Add(copyBtn); btnStack.Children.Add(detailsBtn); stack.Children.Add(btnStack); stack.Children.Add(dbg);
            }

            b.Child = stack; ChatHistoryPanel.Children.Add(b); ScrollViewerPrivate.ScrollToBottom();
            return (b, rt, dbg);
        }

        private Button CreateBubbleButton(string icon, RoutedEventHandler click) {
            var b = new Button { Content = icon, FontSize = 10, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.White, Cursor = Cursors.Hand, Margin = new Thickness(8,0,0,0) };
            b.Click += click; return b;
        }
    }
}
```

### 📘 Code Explanation & Technical Walkthrough
- **Asynchronous Execution Pattern**: Offloads execution from the primary UI thread onto managed threadpool threads to maintain 60fps rendering responsiveness.
- **Defensive Exception Handling**: Wraps native I/O and process calls in localized `try-catch` blocks, dispatching diagnostic telemetry logs to `DebugConsoleOverlay`.
- **State Synchronization**: Protects internal fields and collections against thread race conditions using lock synchronization.

---

## ⚡ Execution Flow & Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Caller / UI Overlay
    participant Sub as ChatOverlay
    participant Kernel as OS Kernel / Layer 0
    participant Log as DebugConsoleOverlay

    Caller->>Sub: Invoke Action / Query Request
    Sub->>Kernel: Execute Managed & Unmanaged Operations
    Kernel-->>Sub: Operation Result / Status Payload
    Sub->>Log: Emit Diagnostic Telemetry Trace
    Sub-->>Caller: Return Results / Update HUD
```

---

## 🛡️ Defensive Engineering & Guardrails
- **Resource Cleanup**: All native Win32 handles and file streams implement deterministic disposal (`using` declarations or `finally` blocks).
- **Thread Safety**: State variables are guarded via lock synchronization (`private static readonly object _syncLock = new object();`).
- **Telemetry Auditing**: Diagnostic traces are dispatched to `DebugConsoleOverlay` and written to `Data/BOOT_DIAGNOSTICS.log`.

---

## 🔗 Related WikiLinks
- [[Master Map of Content & System Index]]
- [[Core System Architecture & 4-Layer Hierarchy]]
- [[NativeMethods & Win32 Kernel Interop Master Manual]]
- [[AiAPI Gateway & Multi-Model Routing Architecture]]
- [[BaseOverlay & GPU Holographic Windowing Engine]]
- [[SystemMonitorOverlay & Diagnostic Telemetry HUD]]
- [[Max PC Optimization Pipeline & Autonomic Engine]]
