// Developer: heaplyn
// Date: 2026-09-03
// Summary: Glassmorphic LLM Settings & Installer Overlay.
//          Added: Gemini 3.5-3.8 models, Custom Command / Script process runner with file picker,
//                 and dedicated configuration panels for all AI backends.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace HeaplitLauncher
{
    public class LlmSettingsOverlay : BaseOverlay
    {
        private static LlmSettingsOverlay? _instance;

        private ComboBox _backendCombo = null!;
        private StackPanel _geminiPanel = null!;
        private ComboBox _geminiModelCombo = null!;
        private StackPanel _openAiPanel = null!;
        private ComboBox _openAiModelCombo = null!;
        private StackPanel _anthropicPanel = null!;
        private ComboBox _anthropicModelCombo = null!;
        private StackPanel _deepSeekPanel = null!;
        private ComboBox _deepSeekModelCombo = null!;
        private StackPanel _groqPanel = null!;
        private ComboBox _groqModelCombo = null!;
        private StackPanel _openRouterPanel = null!;
        private ComboBox _openRouterModelCombo = null!;
        private StackPanel _mistralPanel = null!;
        private ComboBox _mistralModelCombo = null!;
        private StackPanel _perplexityPanel = null!;
        private ComboBox _perplexityModelCombo = null!;
        private StackPanel _xaiPanel = null!;
        private ComboBox _xaiModelCombo = null!;
        private StackPanel _lmStudioPanel = null!;
        private StackPanel _ollamaPanel = null!;
        private StackPanel _customApiPanel = null!;
        private StackPanel _customCmdPanel = null!;
        private TextBlock _statusText = null!;

        public LlmSettingsOverlay()
            : base("LLM ENGINE & INSTALLER STUDIO", width: 540, height: 780)
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, CanContentScroll = false };
            scroll.PreviewMouseWheel += (s, e) => {
                double scrollAmount = (e.Delta / 3.0 > 0 ? Math.Max(28, e.Delta / 3.0) : Math.Min(-28, e.Delta / 3.0));
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset - scrollAmount);
                e.Handled = true;
            };
            var root = new StackPanel { Margin = new Thickness(14) };
            scroll.Content = root;

            var set = CoreRegistry.Data.Settings.Current;

            // ── Backend Selector ──────────────────────────────────────────────────────
            root.Children.Add(CreateHeader("🤖 Active LLM Engine"));

            _backendCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(8, 6, 8, 6), FontSize = 13 };
            var allBackends = new[] {
                "Auto", "Gemini", "OpenAI", "Anthropic", "ClaudeCode", "Groq",
                "OpenRouter", "DeepSeek", "Mistral", "Perplexity", "X-AI",
                "LM Studio", "Ollama", "Custom API", "Custom Command (CLI/Script)"
            };
            foreach (var b in allBackends) _backendCombo.Items.Add(b);

            root.Children.Add(_backendCombo);

            // ── Panels Initialization ──────────────────────────────────────────────────

            // 1. Gemini
            _geminiPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _geminiPanel.Children.Add(CreateLabel("Google Gemini API Key:"));
            var geminiKey = CreateTextBox(set?.GOOGLE_AI_KEY);
            geminiKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.GOOGLE_AI_KEY = geminiKey.Text.Trim(); };
            _geminiPanel.Children.Add(geminiKey);
            _geminiPanel.Children.Add(new TextBlock {
                Text = "Create a free key at AI Studio (keys start with 'AIza' or 'AQ.'). You can also connect a Google Account in Accounts tab.",
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4)
            });
            var getGeminiKeyBtn = new Button { Content = "🔑 Get a free Gemini API key", Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Left };
            getGeminiKeyBtn.Click += (s, e) => ApiKeyPortals.Open("Gemini");
            _geminiPanel.Children.Add(getGeminiKeyBtn);

            _geminiPanel.Children.Add(CreateLabel("Gemini Model Target:"));
            var geminiModels = new[] {
                "gemini-3.8-flash", "gemini-3.8-pro",
                "gemini-3.7-flash", "gemini-3.7-pro", "gemini-3.7-flash-thinking",
                "gemini-3.6-flash", "gemini-3.6-pro",
                "gemini-3.5-flash", "gemini-3.5-pro",
                "gemini-2.5-flash", "gemini-2.5-pro",
                "gemini-2.0-flash", "gemini-1.5-flash", "gemini-1.5-pro"
            };
            _geminiModelCombo = CreateEditableComboBox(geminiModels, set?.GEMINI_MODEL);
            _geminiModelCombo.SelectionChanged += (s, e) => {
                var selected = _geminiModelCombo.SelectedItem?.ToString() ?? _geminiModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.GEMINI_MODEL = selected;
            };
            _geminiPanel.Children.Add(_geminiModelCombo);
            root.Children.Add(_geminiPanel);

            // 2. OpenAI
            _openAiPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _openAiPanel.Children.Add(CreateLabel("OpenAI API Key:"));
            var oaiKey = CreateTextBox(set?.OPENAI_KEY);
            oaiKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.OPENAI_KEY = oaiKey.Text.Trim(); };
            _openAiPanel.Children.Add(oaiKey);
            _openAiPanel.Children.Add(CreateLabel("Base Endpoint URL:"));
            var oaiUrl = CreateTextBox(set?.OPENAI_BASE_URL);
            oaiUrl.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.OPENAI_BASE_URL = oaiUrl.Text.Trim(); };
            _openAiPanel.Children.Add(oaiUrl);
            _openAiPanel.Children.Add(CreateLabel("OpenAI Model:"));
            _openAiModelCombo = CreateEditableComboBox(new[] { "gpt-4o", "gpt-4o-mini", "o1", "o3-mini", "gpt-4.5-preview" }, set?.OPENAI_MODEL);
            _openAiModelCombo.SelectionChanged += (s, e) => {
                var selected = _openAiModelCombo.SelectedItem?.ToString() ?? _openAiModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.OPENAI_MODEL = selected;
            };
            _openAiPanel.Children.Add(_openAiModelCombo);
            root.Children.Add(_openAiPanel);

            // 3. Anthropic
            _anthropicPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _anthropicPanel.Children.Add(CreateLabel("Anthropic API Key:"));
            var antKey = CreateTextBox(set?.ANTHROPIC_KEY);
            antKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.ANTHROPIC_KEY = antKey.Text.Trim(); };
            _anthropicPanel.Children.Add(antKey);
            _anthropicPanel.Children.Add(CreateLabel("Anthropic Model:"));
            _anthropicModelCombo = CreateEditableComboBox(new[] { "claude-3-7-sonnet-latest", "claude-3-5-sonnet-latest", "claude-3-5-haiku-latest", "claude-3-opus-latest" }, set?.ANTHROPIC_MODEL);
            _anthropicModelCombo.SelectionChanged += (s, e) => {
                var selected = _anthropicModelCombo.SelectedItem?.ToString() ?? _anthropicModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.ANTHROPIC_MODEL = selected;
            };
            _anthropicPanel.Children.Add(_anthropicModelCombo);
            root.Children.Add(_anthropicPanel);

            // 4. Groq
            _groqPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _groqPanel.Children.Add(CreateLabel("Groq API Key:"));
            var groqKey = CreateTextBox(set?.GROQ_KEY);
            groqKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.GROQ_KEY = groqKey.Text.Trim(); };
            _groqPanel.Children.Add(groqKey);
            _groqPanel.Children.Add(CreateLabel("Groq Model:"));
            _groqModelCombo = CreateEditableComboBox(new[] { "llama-3.3-70b-versatile", "deepseek-r1-distill-llama-70b", "llama-3.1-8b-instant", "mixtral-8x7b-32768" }, set?.GROQ_MODEL);
            _groqModelCombo.SelectionChanged += (s, e) => {
                var selected = _groqModelCombo.SelectedItem?.ToString() ?? _groqModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.GROQ_MODEL = selected;
            };
            _groqPanel.Children.Add(_groqModelCombo);
            root.Children.Add(_groqPanel);

            // 5. OpenRouter
            _openRouterPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _openRouterPanel.Children.Add(CreateLabel("OpenRouter API Key (400+ Models with 1 Key):"));
            var orKey = CreateTextBox(set?.OPENROUTER_KEY);
            orKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.OPENROUTER_KEY = orKey.Text.Trim(); };
            _openRouterPanel.Children.Add(orKey);
            var getOrKeyBtn = new Button { Content = "🔑 Get an OpenRouter API key", Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Left };
            getOrKeyBtn.Click += (s, e) => ApiKeyPortals.Open("OpenRouter");
            _openRouterPanel.Children.Add(getOrKeyBtn);

            _openRouterPanel.Children.Add(CreateLabel("OpenRouter Model Target:"));
            var openRouterPopularModels = new[] {
                "openrouter/auto",
                "anthropic/claude-3.7-sonnet",
                "anthropic/claude-3.5-sonnet",
                "anthropic/claude-3.5-haiku",
                "deepseek/deepseek-r1",
                "deepseek/deepseek-chat",
                "openai/gpt-4o",
                "openai/gpt-4o-mini",
                "openai/o3-mini",
                "openai/o1",
                "meta-llama/llama-3.3-70b-instruct",
                "qwen/qwen-2.5-72b-instruct",
                "google/gemini-2.0-flash-001",
                "mistralai/mistral-large-2411",
                "perplexity/sonar"
            };
            _openRouterModelCombo = CreateEditableComboBox(openRouterPopularModels, set?.OPENROUTER_MODEL);
            _openRouterModelCombo.SelectionChanged += (s, e) => {
                var selected = _openRouterModelCombo.SelectedItem?.ToString() ?? _openRouterModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.OPENROUTER_MODEL = selected;
            };
            _openRouterPanel.Children.Add(_openRouterModelCombo);

            var discoverOrBtn = CreateStyledButton("🔍 Fetch Live OpenRouter Models", async (s, e) => {
                _statusText.Text = "Fetching OpenRouter catalog...";
                _statusText.Foreground = Brushes.Cyan;
                var models = await ModelDiscoveryService.SearchOpenRouterAsync("");
                if (models.Count > 0) {
                    foreach (var m in models) {
                        if (!_openRouterModelCombo.Items.Contains(m.Id)) _openRouterModelCombo.Items.Add(m.Id);
                    }
                    _statusText.Text = $"Loaded {models.Count} OpenRouter models into dropdown.";
                    _statusText.Foreground = Brushes.LightGreen;
                } else {
                    _statusText.Text = "Could not fetch OpenRouter models. Check internet connection.";
                    _statusText.Foreground = Brushes.Orange;
                }
            });
            _openRouterPanel.Children.Add(discoverOrBtn);
            root.Children.Add(_openRouterPanel);

            // 6. DeepSeek
            _deepSeekPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _deepSeekPanel.Children.Add(CreateLabel("DeepSeek API Key:"));
            var dsKey = CreateTextBox(!string.IsNullOrEmpty(set?.DEEPSEEK_KEY) ? set?.DEEPSEEK_KEY : set?.CUSTOM_LLM_KEY);
            dsKey.TextChanged += (s, e) => {
                if (CoreRegistry.Data.Settings.Current != null) {
                    CoreRegistry.Data.Settings.Current.DEEPSEEK_KEY = dsKey.Text.Trim();
                    CoreRegistry.Data.Settings.Current.CUSTOM_LLM_KEY = dsKey.Text.Trim();
                }
            };
            _deepSeekPanel.Children.Add(dsKey);
            _deepSeekPanel.Children.Add(CreateLabel("DeepSeek Model:"));
            _deepSeekModelCombo = CreateEditableComboBox(new[] { "deepseek-chat", "deepseek-reasoner" }, !string.IsNullOrEmpty(set?.DEEPSEEK_MODEL) ? set?.DEEPSEEK_MODEL : set?.CUSTOM_LLM_MODEL);
            _deepSeekModelCombo.SelectionChanged += (s, e) => {
                var selected = _deepSeekModelCombo.SelectedItem?.ToString() ?? _deepSeekModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) {
                    CoreRegistry.Data.Settings.Current.DEEPSEEK_MODEL = selected;
                    CoreRegistry.Data.Settings.Current.CUSTOM_LLM_MODEL = selected;
                }
            };
            _deepSeekPanel.Children.Add(_deepSeekModelCombo);
            root.Children.Add(_deepSeekPanel);

            // 7. Mistral
            _mistralPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _mistralPanel.Children.Add(CreateLabel("Mistral API Key:"));
            var misKey = CreateTextBox(set?.MISTRAL_KEY);
            misKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.MISTRAL_KEY = misKey.Text.Trim(); };
            _mistralPanel.Children.Add(misKey);
            _mistralPanel.Children.Add(CreateLabel("Mistral Model:"));
            _mistralModelCombo = CreateEditableComboBox(new[] { "mistral-large-latest", "codestral-latest", "mistral-small-latest" }, set?.MISTRAL_MODEL);
            _mistralModelCombo.SelectionChanged += (s, e) => {
                var selected = _mistralModelCombo.SelectedItem?.ToString() ?? _mistralModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.MISTRAL_MODEL = selected;
            };
            _mistralPanel.Children.Add(_mistralModelCombo);
            root.Children.Add(_mistralPanel);

            // 8. Perplexity
            _perplexityPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _perplexityPanel.Children.Add(CreateLabel("Perplexity API Key:"));
            var perpKey = CreateTextBox(set?.PERPLEXITY_KEY);
            perpKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.PERPLEXITY_KEY = perpKey.Text.Trim(); };
            _perplexityPanel.Children.Add(perpKey);
            _perplexityPanel.Children.Add(CreateLabel("Perplexity Model:"));
            _perplexityModelCombo = CreateEditableComboBox(new[] { "sonar", "sonar-pro", "sonar-reasoning" }, set?.PERPLEXITY_MODEL);
            _perplexityModelCombo.SelectionChanged += (s, e) => {
                var selected = _perplexityModelCombo.SelectedItem?.ToString() ?? _perplexityModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.PERPLEXITY_MODEL = selected;
            };
            _perplexityPanel.Children.Add(_perplexityModelCombo);
            root.Children.Add(_perplexityPanel);

            // 9. X-AI (Grok)
            _xaiPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _xaiPanel.Children.Add(CreateLabel("x.AI API Key:"));
            var xaiKey = CreateTextBox(!string.IsNullOrEmpty(set?.XAI_KEY) ? set?.XAI_KEY : set?.CUSTOM_LLM_KEY);
            xaiKey.TextChanged += (s, e) => {
                if (CoreRegistry.Data.Settings.Current != null) {
                    CoreRegistry.Data.Settings.Current.XAI_KEY = xaiKey.Text.Trim();
                    CoreRegistry.Data.Settings.Current.CUSTOM_LLM_KEY = xaiKey.Text.Trim();
                }
            };
            _xaiPanel.Children.Add(xaiKey);
            _xaiPanel.Children.Add(CreateLabel("Grok Model:"));
            _xaiModelCombo = CreateEditableComboBox(new[] { "grok-2-latest", "grok-beta" }, set?.XAI_MODEL);
            _xaiModelCombo.SelectionChanged += (s, e) => {
                var selected = _xaiModelCombo.SelectedItem?.ToString() ?? _xaiModelCombo.Text;
                if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.XAI_MODEL = selected;
            };
            _xaiPanel.Children.Add(_xaiModelCombo);
            root.Children.Add(_xaiPanel);

            // 10. LM Studio
            _lmStudioPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _lmStudioPanel.Children.Add(CreateLabel("LM Studio Endpoint URL:"));
            var lmsEndpoint = CreateTextBox(set?.LM_STUDIO_ENDPOINT);
            lmsEndpoint.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.LM_STUDIO_ENDPOINT = lmsEndpoint.Text.Trim(); };
            _lmStudioPanel.Children.Add(lmsEndpoint);
            var lmsDiscoverBtn = CreateStyledButton("🔍 Discover LM Studio Models", async (s, e) => {
                var models = await LlmRouter.GetLmStudioModelsAsync();
                _statusText.Text = models.Count > 0 ? $"Found {models.Count} models: {string.Join(", ", models.Take(5))}" : "No models detected on LM Studio endpoint.";
                _statusText.Foreground = models.Count > 0 ? Brushes.LightGreen : Brushes.Orange;
            });
            _lmStudioPanel.Children.Add(lmsDiscoverBtn);
            root.Children.Add(_lmStudioPanel);

            // 11. Ollama
            _ollamaPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _ollamaPanel.Children.Add(CreateLabel("Ollama Endpoint:"));
            var ollamaEndpoint = CreateTextBox(set?.OLLAMA_ENDPOINT);
            ollamaEndpoint.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.OLLAMA_ENDPOINT = ollamaEndpoint.Text.Trim(); };
            _ollamaPanel.Children.Add(ollamaEndpoint);
            _ollamaPanel.Children.Add(CreateLabel("Local Model:"));
            var ollamaModel = CreateTextBox(set?.OLLAMA_MODEL);
            ollamaModel.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.OLLAMA_MODEL = ollamaModel.Text.Trim(); };
            _ollamaPanel.Children.Add(ollamaModel);

            var discoverBtn = CreateStyledButton("🔍 Discover Local Servers", async (s, e) => {
                if (CoreRegistry.Intelligence.Llm != null) {
                    string res = await CoreRegistry.Intelligence.Llm.DiscoverAiServersAsync();
                    ContentPreviewOverlay.Show("Discovery Result", res, "markdown");
                }
            });
            _ollamaPanel.Children.Add(discoverBtn);
            root.Children.Add(_ollamaPanel);

            // 12. Custom OpenAI Endpoint
            _customApiPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _customApiPanel.Children.Add(CreateLabel("Custom Endpoint Base URL (OpenAI-Compatible):"));
            var customEndpoint = CreateTextBox(set?.CUSTOM_LLM_ENDPOINT);
            customEndpoint.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.CUSTOM_LLM_ENDPOINT = customEndpoint.Text.Trim(); };
            _customApiPanel.Children.Add(customEndpoint);
            _customApiPanel.Children.Add(CreateLabel("Custom API Key (Optional):"));
            var customKey = CreateTextBox(set?.CUSTOM_LLM_KEY);
            customKey.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.CUSTOM_LLM_KEY = customKey.Text.Trim(); };
            _customApiPanel.Children.Add(customKey);
            _customApiPanel.Children.Add(CreateLabel("Custom Model Name:"));
            var customModel = CreateTextBox(set?.CUSTOM_LLM_MODEL);
            customModel.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.CUSTOM_LLM_MODEL = customModel.Text.Trim(); };
            _customApiPanel.Children.Add(customModel);
            root.Children.Add(_customApiPanel);

            // 13. Custom Command / CLI Script Runner (MCP / Executable / Script Engine)
            _customCmdPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _customCmdPanel.Children.Add(CreateLabel("Custom Command / Executable / Script File:"));
            
            var cmdGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            cmdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cmdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var cmdFileBox = CreateTextBox(set?.CUSTOM_CMD_RUNNER_PATH);
            cmdFileBox.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.CUSTOM_CMD_RUNNER_PATH = cmdFileBox.Text.Trim(); };
            Grid.SetColumn(cmdFileBox, 0); cmdGrid.Children.Add(cmdFileBox);

            var browseCmdBtn = new Button { Content = "📁 Browse File", Margin = new Thickness(6, 0, 0, 6), Padding = new Thickness(10, 4, 10, 4), Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Foreground = Brushes.White };
            browseCmdBtn.Click += (s, e) => {
                var dlg = new OpenFileDialog { Filter = "Executable & Script Files (*.exe;*.ps1;*.bat;*.cmd;*.py;*.sh)|*.exe;*.ps1;*.bat;*.cmd;*.py;*.sh|All Files (*.*)|*.*" };
                if (dlg.ShowDialog() == true) {
                    cmdFileBox.Text = dlg.FileName;
                    if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.CUSTOM_CMD_RUNNER_PATH = dlg.FileName;
                }
            };
            Grid.SetColumn(browseCmdBtn, 1); cmdGrid.Children.Add(browseCmdBtn);
            _customCmdPanel.Children.Add(cmdGrid);

            _customCmdPanel.Children.Add(CreateLabel("Arguments & Template ({prompt}, {model}, {system}, or empty for stdin):"));
            var cmdArgsBox = CreateTextBox(set?.CUSTOM_CMD_RUNNER_ARGS);
            cmdArgsBox.TextChanged += (s, e) => { if (CoreRegistry.Data.Settings.Current != null) CoreRegistry.Data.Settings.Current.CUSTOM_CMD_RUNNER_ARGS = cmdArgsBox.Text.Trim(); };
            _customCmdPanel.Children.Add(cmdArgsBox);

            _customCmdPanel.Children.Add(CreateLabel("Execution Environment:"));
            var cmdTypeCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 8), FontSize = 12 };
            foreach (var t in new[] { "Auto", "PowerShell", "Process", "Python", "Cmd" }) cmdTypeCombo.Items.Add(t);
            cmdTypeCombo.SelectedItem = set?.CUSTOM_CMD_RUNNER_TYPE ?? "Auto";
            cmdTypeCombo.SelectionChanged += (s, e) => {
                if (CoreRegistry.Data.Settings.Current != null && cmdTypeCombo.SelectedItem != null)
                    CoreRegistry.Data.Settings.Current.CUSTOM_CMD_RUNNER_TYPE = cmdTypeCombo.SelectedItem.ToString()!;
            };
            _customCmdPanel.Children.Add(cmdTypeCombo);

            _customCmdPanel.Children.Add(new TextBlock {
                Text = "Tip: If arguments do not contain '{prompt}', Heaplit automatically pipes the full context to StandardInput (like Claude CLI / headless runners).",
                Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6)
            });
            root.Children.Add(_customCmdPanel);

            // ── Global Actions ───────────────────────────────────────────────────────
            root.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
            var testBtn = CreateStyledButton("⚡ Test Backend", async (s, e) => {
                try {
                    _statusText.Text = "⏳ Testing active backend...";
                    _statusText.Foreground = Brushes.Yellow;
                    string res = await LlmRouter.AskAsync("Reply with exactly: ONLINE.");
                    _statusText.Text = "✅ " + res;
                    _statusText.Foreground = Brushes.LightGreen;
                } catch (Exception ex) { 
                    _statusText.Text = "❌ " + ex.Message; 
                    _statusText.Foreground = Brushes.Tomato; 
                }
            });
            root.Children.Add(testBtn);

            _statusText = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 5), Foreground = Brushes.Gray };
            root.Children.Add(_statusText);

            var saveBtn = CreateStyledButton("💾 Save & Close", (s, e) => {
                if (_backendCombo?.SelectedItem != null && CoreRegistry.Data.Settings.Current != null) 
                {
                    string rawSel = _backendCombo.SelectedItem.ToString()!;
                    string cleanBackend = rawSel switch {
                        "LM Studio" => "LMStudio",
                        "Custom API" => "Custom",
                        "Custom Command (CLI/Script)" => "CustomCommand",
                        _ => rawSel
                    };
                    CoreRegistry.Data.Settings.Current.LLM_BACKEND = cleanBackend;
                }
                
                CoreRegistry.Data.Settings.Save();
                this.FadeOutAndClose();
            }, isPrimary: true);
            root.Children.Add(saveBtn);

            this.UserContent = scroll;

            _backendCombo.SelectionChanged += (s, e) => UpdatePanelVisibility();
            
            string currentBackend = set?.LLM_BACKEND ?? "Gemini";
            foreach (var item in _backendCombo.Items) {
                string it = item?.ToString() ?? "";
                if (it.Equals(currentBackend, StringComparison.OrdinalIgnoreCase) ||
                    (currentBackend == "LMStudio" && it == "LM Studio") ||
                    (currentBackend == "Custom" && it == "Custom API") ||
                    (currentBackend == "CustomCommand" && it.StartsWith("Custom Command")))
                {
                    _backendCombo.SelectedItem = item;
                    break;
                }
            }
            
            if (_backendCombo.SelectedItem == null) 
                _backendCombo.SelectedIndex = 0;

            UpdatePanelVisibility();
        }

        private void UpdatePanelVisibility()
        {
            if (_backendCombo?.SelectedItem == null) return;
            string sel = _backendCombo.SelectedItem.ToString()!;

            if (_geminiPanel != null) _geminiPanel.Visibility = sel == "Gemini" ? Visibility.Visible : Visibility.Collapsed;
            if (_openAiPanel != null) _openAiPanel.Visibility = sel == "OpenAI" ? Visibility.Visible : Visibility.Collapsed;
            if (_anthropicPanel != null) _anthropicPanel.Visibility = sel == "Anthropic" ? Visibility.Visible : Visibility.Collapsed;
            if (_groqPanel != null) _groqPanel.Visibility = sel == "Groq" ? Visibility.Visible : Visibility.Collapsed;
            if (_openRouterPanel != null) _openRouterPanel.Visibility = sel == "OpenRouter" ? Visibility.Visible : Visibility.Collapsed;
            if (_deepSeekPanel != null) _deepSeekPanel.Visibility = sel == "DeepSeek" ? Visibility.Visible : Visibility.Collapsed;
            if (_mistralPanel != null) _mistralPanel.Visibility = sel == "Mistral" ? Visibility.Visible : Visibility.Collapsed;
            if (_perplexityPanel != null) _perplexityPanel.Visibility = sel == "Perplexity" ? Visibility.Visible : Visibility.Collapsed;
            if (_xaiPanel != null) _xaiPanel.Visibility = (sel == "X-AI" || sel == "Grok") ? Visibility.Visible : Visibility.Collapsed;
            if (_lmStudioPanel != null) _lmStudioPanel.Visibility = sel.Contains("LM Studio") ? Visibility.Visible : Visibility.Collapsed;
            if (_ollamaPanel != null) _ollamaPanel.Visibility = (sel == "Ollama" || sel == "Auto") ? Visibility.Visible : Visibility.Collapsed;
            if (_customApiPanel != null) _customApiPanel.Visibility = sel.Contains("Custom API") ? Visibility.Visible : Visibility.Collapsed;
            if (_customCmdPanel != null) _customCmdPanel.Visibility = sel.Contains("Custom Command") ? Visibility.Visible : Visibility.Collapsed;
        }

        private new static TextBlock CreateHeader(string t) => new TextBlock { Text = t, FontSize = 13, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 4), Foreground = Brushes.White };
        private static TextBlock CreateLabel(string t) => new TextBlock { Text = t, FontSize = 11, Margin = new Thickness(0, 4, 0, 2), Foreground = Brushes.LightGray };
        private static TextBox CreateTextBox(string? t) => new TextBox { Text = t ?? "", Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(6, 4, 6, 4), FontSize = 12, Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), Foreground = Brushes.White, BorderBrush = Brushes.DimGray };
        private static ComboBox CreateEditableComboBox(string[] items, string? current) { 
            var cb = new ComboBox { IsEditable = true, Margin = new Thickness(0, 0, 0, 8), FontSize = 12, VerticalContentAlignment = VerticalAlignment.Center };
            foreach (var x in items) cb.Items.Add(x); 
            if (!string.IsNullOrEmpty(current)) cb.Text = current; 
            return cb; 
        }

        public static void ShowOverlay() {
            Application.Current.Dispatcher.Invoke(() => {
                if (_instance == null || !_instance.IsLoaded) _instance = new LlmSettingsOverlay();
                _instance.Show(); 
                _instance.BringToFront();
            });
        }
    }
}