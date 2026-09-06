// Developer: heaplyn
// Date: 2026-08-20
// Summary: Interactive Glassmorphic Discord Message Scraper and Exporter Overlay.
//          Allows users to configure bot credentials, load active guilds/channels/DMs, 
//          preview messages, and export logs directly to markdown files.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class DiscordScraperOverlay : BaseOverlay
    {
        private static DiscordScraperOverlay? _instance;

        private readonly TextBox _tokenInput;
        private readonly ComboBox _guildComboBox;
        private readonly ListBox _channelListBox;
        private readonly ListBox _messagesListBox;
        private readonly TextBlock _statusLabel;
        private readonly Button _exportBtn;
        private readonly Button _connectBtn;

        private List<DiscordGuildInfo> _guilds = new();
        private List<DiscordChannelInfo> _channels = new();
        private List<DiscordMessageInfo> _activeMessages = new();

        public static void Open()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null || !_instance.IsLoaded)
                {
                    _instance = new DiscordScraperOverlay();
                    _instance.Closed += (s, e) => _instance = null;
                }
                _instance.Show();
                _instance.BringToFront();
            });
        }

        private DiscordScraperOverlay() : base("💬 DISCORD MESSAGE LOGGER & EXPORTER", width: 820, height: 550)
        {
            var mainGrid = new Grid { Margin = new Thickness(12) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Token row
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Guild selection
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Lists panels
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status & Export

            // --- Row 0: Token Input ---
            var tokenGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            tokenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tokenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tokenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tokenLabel = CreateLabel("BOT TOKEN:", 11, true);
            BaseOverlay.SetLabelForeground(tokenLabel, Brushes.Cyan);
            tokenLabel.Margin = new Thickness(0, 0, 10, 0);
            tokenLabel.VerticalAlignment = VerticalAlignment.Center;
            tokenGrid.Children.Add(tokenLabel);

            _tokenInput = new TextBox
            {
                Height = 26,
                FontSize = 11,
                Padding = new Thickness(6, 3, 6, 3),
                VerticalContentAlignment = VerticalAlignment.Center,
                Text = SettingsManager.Current.DISCORD_BOT_TOKEN
            };
            _tokenInput.SetResourceReference(TextBox.BackgroundProperty, "WindowBackgroundBrush");
            _tokenInput.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _tokenInput.SetResourceReference(TextBox.BorderBrushProperty, "WindowBorderBrush");
            _tokenInput.SetResourceReference(TextBox.CaretBrushProperty, "AccentCaretBrush");
            Grid.SetColumn(_tokenInput, 1);
            tokenGrid.Children.Add(_tokenInput);

            _connectBtn = CreateStyledButton("CONNECT / SAVE", (s, e) => SaveAndConnect(), isPrimary: true, fontSize: 10);
            _connectBtn.Margin = new Thickness(10, 0, 0, 0);
            Grid.SetColumn(_connectBtn, 2);
            tokenGrid.Children.Add(_connectBtn);

            Grid.SetRow(tokenGrid, 0);
            mainGrid.Children.Add(tokenGrid);

            // --- Row 1: Guild Selector ---
            var guildGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            guildGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            guildGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var guildLabel = CreateLabel("SELECT SERVER:", 11, true);
            BaseOverlay.SetLabelForeground(guildLabel, Brushes.Cyan);
            guildLabel.Margin = new Thickness(0, 0, 10, 0);
            guildLabel.VerticalAlignment = VerticalAlignment.Center;
            guildGrid.Children.Add(guildLabel);

            _guildComboBox = new ComboBox { Height = 26, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
            _guildComboBox.SelectionChanged += GuildComboBox_SelectionChanged;
            Grid.SetColumn(_guildComboBox, 1);
            guildGrid.Children.Add(_guildComboBox);

            Grid.SetRow(guildGrid, 1);
            mainGrid.Children.Add(guildGrid);

            // --- Row 2: Columns Panel (Channels & Messages) ---
            var columnsGrid = new Grid();
            columnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            columnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left column: Channels/DMs list
            var leftStack = new Grid { Margin = new Thickness(0, 0, 10, 0) };
            leftStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var chanLabel = CreateLabel("CHANNELS / DIRECT MESSAGES:", 10, true);
            BaseOverlay.SetLabelForeground(chanLabel, Brushes.Gray);
            leftStack.Children.Add(chanLabel);

            _channelListBox = new ListBox
            {
                Background = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 0)
            };
            _channelListBox.SelectionChanged += ChannelListBox_SelectionChanged;
            Grid.SetRow(_channelListBox, 1);
            leftStack.Children.Add(_channelListBox);
            columnsGrid.Children.Add(leftStack);

            // Right column: Message log list
            var rightStack = new Grid();
            rightStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var msgLabel = CreateLabel("PREVIEW MESSAGES (RECENT 50):", 10, true);
            BaseOverlay.SetLabelForeground(msgLabel, Brushes.Gray);
            rightStack.Children.Add(msgLabel);

            _messagesListBox = new ListBox
            {
                Background = new SolidColorBrush(Color.FromArgb(5, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(_messagesListBox, 1);
            rightStack.Children.Add(_messagesListBox);

            Grid.SetColumn(rightStack, 1);
            columnsGrid.Children.Add(rightStack);

            Grid.SetRow(columnsGrid, 2);
            mainGrid.Children.Add(columnsGrid);

            // --- Row 3: Status & Bottom Actions ---
            var bottomGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusLabel = new TextBlock
            {
                Text = "Ready. Configure token and click connect.",
                Foreground = Brushes.Gray,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            bottomGrid.Children.Add(_statusLabel);

            _exportBtn = CreateStyledButton("📥 EXPORT MESSAGES TO MARKDOWN FILE", (s, e) => ExportMessages(), isPrimary: true, fontSize: 11);
            _exportBtn.IsEnabled = false;
            Grid.SetColumn(_exportBtn, 1);
            bottomGrid.Children.Add(_exportBtn);

            Grid.SetRow(bottomGrid, 3);
            mainGrid.Children.Add(bottomGrid);

            this.UserContent = mainGrid;

            if (DiscordScraperManager.HasToken)
            {
                SaveAndConnect();
            }
        }

        private async void SaveAndConnect()
        {
            string token = _tokenInput.Text.Trim();
            if (string.IsNullOrEmpty(token))
            {
                _statusLabel.Text = "⚠️ Bot token is empty.";
                _statusLabel.Foreground = Brushes.Tomato;
                return;
            }

            DiscordScraperManager.SaveBotToken(token);
            _statusLabel.Text = "Connecting to Discord...";
            _statusLabel.Foreground = Brushes.Cyan;
            _connectBtn.IsEnabled = false;

            try
            {
                _guilds = await DiscordScraperManager.GetGuildsAsync();
                _guildComboBox.Items.Clear();

                _guildComboBox.Items.Add("💬 Direct Messages (DMs)");
                foreach (var g in _guilds)
                {
                    _guildComboBox.Items.Add($"📂 {g.Name}");
                }

                _guildComboBox.SelectedIndex = 0;
                _statusLabel.Text = $"Connected! Loaded {_guilds.Count} guilds.";
                _statusLabel.Foreground = Brushes.Lime;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Connection failed: {ex.Message}";
                _statusLabel.Foreground = Brushes.Tomato;
            }
            finally
            {
                _connectBtn.IsEnabled = true;
            }
        }

        private async void GuildComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = _guildComboBox.SelectedIndex;
            if (index < 0) return;

            _channelListBox.Items.Clear();
            _messagesListBox.ItemsSource = null;
            _exportBtn.IsEnabled = false;

            _statusLabel.Text = "Loading channels...";
            _statusLabel.Foreground = Brushes.Cyan;

            try
            {
                if (index == 0) // DMs
                {
                    _channels = await DiscordScraperManager.GetDMsAsync();
                    foreach (var c in _channels)
                    {
                        _channelListBox.Items.Add($"👤 {c.Name}");
                    }
                    _statusLabel.Text = $"Loaded {_channels.Count} DM channels.";
                    _statusLabel.Foreground = Brushes.Lime;
                }
                else
                {
                    var guild = _guilds[index - 1];
                    _channels = await DiscordScraperManager.GetChannelsAsync(guild.Id);
                    foreach (var c in _channels)
                    {
                        _channelListBox.Items.Add($"# {c.Name}");
                    }
                    _statusLabel.Text = $"Loaded {_channels.Count} text channels.";
                    _statusLabel.Foreground = Brushes.Lime;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Failed to load: {ex.Message}";
                _statusLabel.Foreground = Brushes.Tomato;
            }
        }

        private async void ChannelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = _channelListBox.SelectedIndex;
            if (index < 0 || index >= _channels.Count) return;

            var channel = _channels[index];
            _statusLabel.Text = $"Loading messages for '{channel.Name}'...";
            _statusLabel.Foreground = Brushes.Cyan;
            _exportBtn.IsEnabled = false;

            try
            {
                _activeMessages = await DiscordScraperManager.GetRecentMessagesAsync(channel.Id, 50);
                
                var displayList = _activeMessages.Select(m => {
                    string cleanTime = m.Timestamp;
                    if (DateTime.TryParse(m.Timestamp, out var dt)) cleanTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    return $"[{cleanTime}] {m.Author}: {m.Content}";
                }).ToList();

                _messagesListBox.ItemsSource = displayList;
                _statusLabel.Text = $"Loaded {displayList.Count} messages.";
                _statusLabel.Foreground = Brushes.Lime;
                _exportBtn.IsEnabled = _activeMessages.Count > 0;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Failed to load messages: {ex.Message}";
                _statusLabel.Foreground = Brushes.Tomato;
                _messagesListBox.ItemsSource = null;
            }
        }

        private async void ExportMessages()
        {
            int index = _channelListBox.SelectedIndex;
            if (index < 0 || index >= _channels.Count) return;

            var channel = _channels[index];
            _statusLabel.Text = "Exporting messages...";
            _statusLabel.Foreground = Brushes.Cyan;

            try
            {
                string path = await DiscordScraperManager.ExportChannelMessagesToFileAsync(channel.Id, channel.Name, 100);
                _statusLabel.Text = $"Exported successfully to: {Path.GetFileName(path)}";
                _statusLabel.Foreground = Brushes.Lime;
                
                TextOverlay.Show($"📝 Chat logs saved to Downloads folder!", 3000);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Export failed: {ex.Message}";
                _statusLabel.Foreground = Brushes.Tomato;
            }
        }
    }
}
