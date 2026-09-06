// Developer: heaplyn
// Date: 2026-08-09
// Summary: Interactive Glassmorphic Music Player & Playlist Manager GUI featuring custom song folders, track organization, media playback controls, file import, and online URL stream link adding.

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class MusicPlaylistOverlay : BaseOverlay
    {
        private static MusicPlaylistOverlay? _instance;
        public static MusicPlaylistOverlay? Instance => _instance;

        private MusicLibraryData _library;
        private MusicFolder? _activeFolder;
        private MusicTrack? _currentTrack;

        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly DispatcherTimer _playTimer = new DispatcherTimer();

        private readonly ComboBox _folderComboBox;
        private readonly TextBox _searchBox;
        private string _searchQuery = string.Empty;
        private readonly StackPanel _tracksPanel;
        private readonly TextBlock _nowPlayingTitle;
        private readonly TextBlock _nowPlayingArtist;
        private readonly Slider _positionSlider;
        private readonly TextBlock _timeLabel;
        private readonly Button _playPauseBtn;

        private bool _isShuffle = false;
        private Button? _shuffleBtn;
        private Slider? _volumeSlider;

        private bool _isPlaying = false;
        private bool _isDraggingSlider = false;

        private enum LoopMode
        {
            Off,
            Track,
            Folder
        }
        private LoopMode _loopMode = LoopMode.Folder;
        private Button? _loopBtn;
        private bool _trackAdvanceTriggered = false;

        // Downloads queue UI controls
        private Button? _downloadsToggleBtn;
        private Grid? _downloadsDrawerGrid;
        private StackPanel? _downloadsQueuePanel;
        private bool _isDownloadsDrawerOpen = false;

        // Mass Action UI controls
        private Border _massActionBar;
        private TextBlock _massActionTitle;
        private ComboBox _massDestinationComboBox;
        private readonly HashSet<string> _selectedTrackIds = new HashSet<string>();

        public class DownloadQueueItem
        {
            public string Url { get; set; } = string.Empty;
            public string Status { get; set; } = "Pending";
            public string Title { get; set; } = "Locating track...";
            public Border? BorderElement { get; set; }
            public TextBlock? StatusTextBlock { get; set; }
            public TextBlock? TitleTextBlock { get; set; }
            public Button? CancelButton { get; set; }
            public System.Threading.CancellationTokenSource Cts { get; set; } = new();
        }

        private readonly List<DownloadQueueItem> _downloadQueue = new List<DownloadQueueItem>();

        public static void OpenPlayer()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null)
                {
                    _instance = new MusicPlaylistOverlay();
                }

                _instance.Show();
            });
        }

        public static void DownloadTrackFromUrl(string url)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OpenPlayer();
                if (string.IsNullOrWhiteSpace(url))
                {
                    InputPromptOverlay.Show("Enter Audio / YouTube / Soundcloud Link to Download MP3:", (input) =>
                    {
                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            _instance?.ExecuteDownloadProcess(input.Trim());
                        }
                    });
                }
                else
                {
                    // Only start download if the URL is not empty after trimming
                    string trimmed = url.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        _instance?.ExecuteDownloadProcess(trimmed);
                    }
                }
            });
        }

        private System.Diagnostics.Process? _streamProcess = null;

        public class MusicPlayerState
        {
            public double Volume { get; set; } = 0.5;
            public bool IsShuffle { get; set; } = false;
            public string LoopMode { get; set; } = "Folder";
        }

        private MusicPlaylistOverlay()
            : base("🎵 HEAPLIT MUSIC PLAYER & PLAYLIST ORGANIZER", width: 680, height: 520)
        {
            // Load persistent state
            var state = PersistentStateManager.LoadState<MusicPlayerState>("MusicPlayer");
            _isShuffle = state?.IsShuffle ?? false;
            _loopMode = Enum.TryParse<LoopMode>(state?.LoopMode, out var lm) ? lm : LoopMode.Folder;
            _mediaPlayer.Volume = state?.Volume ?? 0.5;

            this.AllowDrop = true;
            this.DragOver += (s, e) => {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
                else e.Effects = DragDropEffects.None;
                e.Handled = true;
            };
            this.Drop += (s, e) => {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    AddFilesFromPaths(files);
                }
            };

            _library = MusicPlaylistManager.LoadLibrary();
            _activeFolder = _library.Folders.FirstOrDefault(f => f.Id == _library.LastActiveFolderId) 
                            ?? _library.Folders.FirstOrDefault();

            this.Closed += (s, e) =>
            {
                // Save state
                PersistentStateManager.SaveState("MusicPlayer", new MusicPlayerState {
                    Volume = _mediaPlayer.Volume,
                    IsShuffle = _isShuffle,
                    LoopMode = _loopMode.ToString()
                });

                // Ensure library state is persisted on close
                if (_library != null)
                {
                    MusicPlaylistManager.SaveLibrary(_library);
                }

                _playTimer.Stop();
                _mediaPlayer.Close();
                try
                {
                    ChromeStreamTracker.KillIfRunning();
                    ChromeRemoteControl.Shutdown();
                    if (_streamProcess != null)
                    {
                        _streamProcess.Kill(entireProcessTree: true);
                    }
                }
                catch { }
                _instance = null;
            };

            this.IsVisibleChanged += (s, e) =>
            {
                // Pause/resume timer based on visibility
                if (this.IsVisible)
                    _playTimer.Start();
                else
                    _playTimer.Stop();
            };

            // Setup Playhead Position Timer
            _playTimer.Interval = TimeSpan.FromMilliseconds(500);
            _playTimer.Tick += PlayTimer_Tick;

            _mediaPlayer.MediaEnded += (s, e) =>
            {
                if (_loopMode == LoopMode.Track)
                {
                    if (_currentTrack != null) PlayTrack(_currentTrack);
                }
                else if (_loopMode == LoopMode.Folder)
                {
                    PlayNextTrack();
                }
                else
                {
                    if (_activeFolder != null && _currentTrack != null)
                    {
                        int index = _activeFolder.Tracks.FindIndex(t => t.Id == _currentTrack.Id);
                        if (index >= 0 && index < _activeFolder.Tracks.Count - 1)
                        {
                            PlayTrack(_activeFolder.Tracks[index + 1]);
                        }
                        else
                        {
                            _mediaPlayer.Stop();
                            _isPlaying = false;
                            if (_playPauseBtn != null) _playPauseBtn.Content = "▶️ Play";
                        }
                    }
                }
            };
            _mediaPlayer.MediaFailed += (s, e) =>
            {
                if (_currentTrack != null && File.Exists(_currentTrack.PathOrUrl))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = Path.GetFullPath(_currentTrack.PathOrUrl),
                            UseShellExecute = true
                        });
                        TextOverlay.Show($"🎵 Playing in Windows Media Player:\n{_currentTrack.Title}", 3000);
                    }
                    catch
                    {
                        TextOverlay.Show($"⚠️ Media Playback Error: {e.ErrorException?.Message ?? "Invalid format"}", 3500);
                    }
                }
            };

            var mainGrid = new Grid { Margin = new Thickness(10) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                     // Folder Selection Bar & Add Buttons
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                     // Mass Action Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Track List View
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                     // Player Controls & Progress Bar

            // 1. Top Folder Bar
            var topBarBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var folderLabel = new TextBlock
            {
                Text = "📁 Folder: ",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 6, 0)
            };
            folderLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            Grid.SetColumn(folderLabel, 0);
            topGrid.Children.Add(folderLabel);

            _folderComboBox = new ComboBox
            {
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI"),
                Padding = new Thickness(6, 4, 6, 4)
            };
            RefreshFoldersDropDown();
            _folderComboBox.SelectionChanged += FolderComboBox_SelectionChanged;
            Grid.SetColumn(_folderComboBox, 1);
            topGrid.Children.Add(_folderComboBox);

            _searchBox = new TextBox
            {
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 2, 5, 2),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                Tag = "Search songs..."
            };
            _searchBox.TextChanged += (s, e) => {
                _searchQuery = _searchBox.Text.ToLower();
                RenderTracksList();
            };
            Grid.SetColumn(_searchBox, 2);
            topGrid.Children.Add(_searchBox);

            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };

            var newFolderBtn = CreateButton("➕ New Folder", (s, e) => CreateNewFolderPrompt());
            var addFileBtn = CreateButton("🎵 Add Files", (s, e) => BrowseAndAddFile());
            var addUrlBtn = CreateButton("🔗 Add Link", (s, e) => AddUrlStreamPrompt());
            var searchWebBtn = CreateButton("🔍 Search Web", (s, e) => ShowWebSearchPrompt());
            var importPlaylistBtn = CreateButton("📋 Import Playlist", (s, e) => ShowPlaylistImportPrompt());
            var clearFolderBtn = CreateButton("🧹 Clear", (s, e) => ClearFolderConfirm());
            _downloadsToggleBtn = CreateButton("📥 Downloads (0)", (s, e) => ToggleDownloadsDrawer());

            buttonStack.Children.Add(newFolderBtn);
            buttonStack.Children.Add(addFileBtn);
            buttonStack.Children.Add(addUrlBtn);
            buttonStack.Children.Add(searchWebBtn);
            buttonStack.Children.Add(importPlaylistBtn);
            buttonStack.Children.Add(clearFolderBtn);
            buttonStack.Children.Add(_downloadsToggleBtn);

            Grid.SetColumn(buttonStack, 3);
            topGrid.Children.Add(buttonStack);

            topBarBorder.Child = topGrid;
            Grid.SetRow(topBarBorder, 0);
            mainGrid.Children.Add(topBarBorder);

            // 1.5 Mass Action Bar Setup
            _massActionBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed
            };
            _massActionBar.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush");

            var massGrid = new Grid();
            massGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            massGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            massGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            massGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            massGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _massActionTitle = new TextBlock
            {
                Text = "Selected: 0 tracks",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            _massActionTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            Grid.SetColumn(_massActionTitle, 0);
            massGrid.Children.Add(_massActionTitle);

            _massDestinationComboBox = new ComboBox
            {
                Width = 150,
                Margin = new Thickness(8, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
            Grid.SetColumn(_massDestinationComboBox, 1);
            massGrid.Children.Add(_massDestinationComboBox);

            var massMoveBtn = CreateButton("📦 Move Selected", (s, e) => MassMoveTracks());
            Grid.SetColumn(massMoveBtn, 2);
            massGrid.Children.Add(massMoveBtn);

            var massDeleteBtn = CreateButton("❌ Delete Selected", (s, e) => MassDeleteTracks());
            Grid.SetColumn(massDeleteBtn, 3);
            massGrid.Children.Add(massDeleteBtn);

            var massCancelBtn = CreateButton("Cancel", (s, e) =>
            {
                _selectedTrackIds.Clear();
                RenderTracksList();
                UpdateMassActionBarVisibility();
            });
            Grid.SetColumn(massCancelBtn, 4);
            massGrid.Children.Add(massCancelBtn);

            _massActionBar.Child = massGrid;
            Grid.SetRow(_massActionBar, 1);
            mainGrid.Children.Add(_massActionBar);

            // 2. Center Track List Grid (splits track list and downloads drawer side-by-side)
            var centerGrid = new Grid();
            centerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Track List
            centerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                     // Downloads Sidebar

            var listScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _tracksPanel = new StackPanel();
            listScrollViewer.Content = _tracksPanel;
            Grid.SetColumn(listScrollViewer, 0);
            centerGrid.Children.Add(listScrollViewer);

            // Construct Downloads Sidebar Drawer
            _downloadsDrawerGrid = new Grid
            {
                Width = 0,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(10, 0, 0, 10)
            };
            _downloadsDrawerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            _downloadsDrawerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scroll list

            var dlHeaderBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                Padding = new Thickness(6),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 6)
            };
            var dlTitle = new TextBlock
            {
                Text = "📥 DOWNLOAD QUEUE",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            dlTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            dlHeaderBorder.Child = dlTitle;
            Grid.SetRow(dlHeaderBorder, 0);
            _downloadsDrawerGrid.Children.Add(dlHeaderBorder);

            var dlScrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _downloadsQueuePanel = new StackPanel();
            dlScrollViewer.Content = _downloadsQueuePanel;
            Grid.SetRow(dlScrollViewer, 1);
            _downloadsDrawerGrid.Children.Add(dlScrollViewer);

            Grid.SetColumn(_downloadsDrawerGrid, 1);
            centerGrid.Children.Add(_downloadsDrawerGrid);

            Grid.SetRow(centerGrid, 2);
            mainGrid.Children.Add(centerGrid);

            // 3. Player Bar (Now Playing + Controls + Progress Slider)
            var playerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Padding = new Thickness(12, 10, 12, 10),
                CornerRadius = new CornerRadius(8)
            };

            var playerStack = new StackPanel();

            // Track info labels
            _nowPlayingTitle = new TextBlock
            {
                Text = "No track selected",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _nowPlayingTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            playerStack.Children.Add(_nowPlayingTitle);

            _nowPlayingArtist = new TextBlock
            {
                Text = "Select a song from your playlist folders to play",
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 6)
            };
            _nowPlayingArtist.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            playerStack.Children.Add(_nowPlayingArtist);

            // Slider & Time
            var sliderGrid = new Grid();
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _positionSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                IsMoveToPointEnabled = true
            };
            _positionSlider.PreviewMouseDown += (s, e) => _isDraggingSlider = true;
            _positionSlider.PreviewMouseUp += async (s, e) =>
            {
                _isDraggingSlider = false;
                if (_currentTrack == null) return;

                if (_currentTrack.IsStreamUrl || _currentTrack.PathOrUrl.StartsWith("http://") || _currentTrack.PathOrUrl.StartsWith("https://"))
                {
                    double targetSec = _positionSlider.Value;
                    await ChromeRemoteControl.SeekAsync(_currentTrack.PathOrUrl, targetSec);
                }
                else if (_mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    double targetSec = (_positionSlider.Value / 100.0) * _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    _mediaPlayer.Position = TimeSpan.FromSeconds(targetSec);
                }
            };
            Grid.SetColumn(_positionSlider, 0);
            sliderGrid.Children.Add(_positionSlider);

            _timeLabel = new TextBlock
            {
                Text = "00:00 / 00:00",
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _timeLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            Grid.SetColumn(_timeLabel, 1);
            sliderGrid.Children.Add(_timeLabel);

            playerStack.Children.Add(sliderGrid);

            // Buttons Bar (Prev, Play/Pause, Next, Loop, Shuffle, Volume)
            var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };

            var prevBtn = CreateButton("⏮️ Prev", (s, e) => PlayPrevTrack());
            _playPauseBtn = CreateButton("▶️ Play", (s, e) => TogglePlayPause());
            var nextBtn = CreateButton("⏭️ Next", (s, e) => PlayNextTrack());
            _loopBtn = CreateButton("🔁 Folder", (s, e) => ToggleLoopMode());
            _shuffleBtn = CreateButton("🔀 Shuffle: Off", (s, e) => ToggleShuffle());

            if (controlsPanel != null) {
                controlsPanel.Children.Add(prevBtn);
                controlsPanel.Children.Add(_playPauseBtn);
                controlsPanel.Children.Add(nextBtn);
                controlsPanel.Children.Add(_loopBtn);
                controlsPanel.Children.Add(_shuffleBtn);
            }

            var volStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(15, 0, 0, 0) };
            volStack.Children.Add(new TextBlock { Text = "🔊", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,4,0), Foreground = Brushes.White });
            _volumeSlider = new Slider
            {
                Width = 80,
                Minimum = 0,
                Maximum = 1,
                Value = _mediaPlayer.Volume,
                VerticalAlignment = VerticalAlignment.Center
            };
            _volumeSlider.ValueChanged += (s, e) =>
            {
                if (_mediaPlayer != null)
                    _mediaPlayer.Volume = _volumeSlider.Value;
            };
            volStack.Children.Add(_volumeSlider);
            controlsPanel.Children.Add(volStack);

            playerStack.Children.Add(controlsPanel);

            playerBorder.Child = playerStack;
            Grid.SetRow(playerBorder, 3);
            mainGrid.Children.Add(playerBorder);

            this.UserContent = mainGrid;

            RenderTracksList();
        }

        private void RefreshFoldersDropDown()
        {
            _folderComboBox.Items.Clear();
            foreach (var f in _library.Folders)
            {
                _folderComboBox.Items.Add(f.FolderName);
            }

            if (_activeFolder != null)
            {
                _folderComboBox.SelectedItem = _activeFolder.FolderName;
            }
        }

        private void FolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_folderComboBox.SelectedItem is string name)
            {
                var folder = _library.Folders.FirstOrDefault(f => f.FolderName == name);
                if (folder != null)
                {
                    _selectedTrackIds.Clear();
                    UpdateMassActionBarVisibility();
                    _activeFolder = folder;
                    _library.LastActiveFolderId = folder.Id;
                    MusicPlaylistManager.SaveLibrary(_library);
                    RenderTracksList();
                }
            }
        }

        private void RenderTracksList()
        {
            _tracksPanel.Children.Clear();

            if (_activeFolder == null) return;

            var tracksToRender = _activeFolder.Tracks;
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                tracksToRender = tracksToRender.Where(t =>
                    t.Title.ToLower().Contains(_searchQuery) ||
                    t.Artist.ToLower().Contains(_searchQuery) ||
                    t.PathOrUrl.ToLower().Contains(_searchQuery)).ToList();
            }

            if (tracksToRender.Count == 0)
            {
                var emptyLabel = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(_searchQuery)
                        ? "📂 This folder is empty. Click 'Add Files' to import music!"
                        : "🔍 No tracks found matching your search.",
                    FontSize = 13,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(12)
                };
                emptyLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                _tracksPanel.Children.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < tracksToRender.Count; i++)
            {
                var track = tracksToRender[i];

                var trackItemBorder = new Border
                {
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 4),
                    CornerRadius = new CornerRadius(4),
                    Background = (_currentTrack?.Id == track.Id)
                        ? new SolidColorBrush(Color.FromArgb(50, 128, 80, 230))
                        : new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                    Cursor = Cursors.Hand,
                    ToolTip = "Left-click to Play. Right-click for options (Rename, Move to Folder). Check box to select multiple."
                };

                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Column 0: CheckBox
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Column 1: Icon
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Column 2: Title Stack
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Column 3: Rename Button
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Column 4: Delete Button

                var selectCheckBox = new CheckBox
                {
                    IsChecked = _selectedTrackIds.Contains(track.Id),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand
                };
                selectCheckBox.Checked += (s, e) =>
                {
                    _selectedTrackIds.Add(track.Id);
                    UpdateMassActionBarVisibility();
                };
                selectCheckBox.Unchecked += (s, e) =>
                {
                    _selectedTrackIds.Remove(track.Id);
                    UpdateMassActionBarVisibility();
                };
                Grid.SetColumn(selectCheckBox, 0);
                itemGrid.Children.Add(selectCheckBox);

                var iconText = new TextBlock
                {
                    Text = track.IsStreamUrl ? "🔗 " : "🎵 ",
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconText, 1);
                itemGrid.Children.Add(iconText);

                var titleStack = new StackPanel();
                var titleBlock = new TextBlock
                {
                    Text = track.Title,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                };
                titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
                titleStack.Children.Add(titleBlock);

                var pathBlock = new TextBlock
                {
                    Text = track.PathOrUrl,
                    FontSize = 10,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                pathBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                titleStack.Children.Add(pathBlock);

                Grid.SetColumn(titleStack, 2);
                itemGrid.Children.Add(titleStack);

                var renameBtn = CreateButton("✏️", (s, e) =>
                {
                    e.Handled = true;
                    RenameTrackPrompt(track);
                });
                renameBtn.Padding = new Thickness(6, 2, 6, 2);
                Grid.SetColumn(renameBtn, 3);
                itemGrid.Children.Add(renameBtn);

                var deleteBtn = CreateButton("❌", (s, e) =>
                {
                    e.Handled = true;
                    _activeFolder.Tracks.Remove(track);
                    MusicPlaylistManager.SaveLibrary(_library);
                    RenderTracksList();
                });
                deleteBtn.Padding = new Thickness(6, 2, 6, 2);
                Grid.SetColumn(deleteBtn, 4);
                itemGrid.Children.Add(deleteBtn);

                trackItemBorder.Child = itemGrid;

                // Click track to play (checking if they clicked checkbox to prevent play triggering)
                trackItemBorder.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.OriginalSource is DependencyObject depObj)
                    {
                        var parent = depObj;
                        while (parent != null && parent != trackItemBorder)
                        {
                            if (parent is CheckBox) return;
                            parent = VisualTreeHelper.GetParent(parent);
                        }
                    }
                    PlayTrack(track);
                };

                // Context menu setup
                var contextMenu = new ContextMenu();
                var renameMenuItem = new MenuItem { Header = "✏️ Rename Track" };
                renameMenuItem.Click += (s, e) => RenameTrackPrompt(track);
                contextMenu.Items.Add(renameMenuItem);

                var moveMenuItem = new MenuItem { Header = "📦 Copy to Folder" };
                bool hasOtherFolders = false;
                foreach (var folder in _library.Folders)
                {
                    if (folder.Id == _activeFolder?.Id) continue;
                    hasOtherFolders = true;
                    var folderItem = new MenuItem { Header = folder.FolderName };
                    folderItem.Click += (s, e) => CopyTrackToFolder(track, folder); // CopyTrackToFolder
                    moveMenuItem.Items.Add(folderItem);
                }
                if (!hasOtherFolders)
                {
                    var noFolderItem = new MenuItem { Header = "(No other folders)", IsEnabled = false };
                    moveMenuItem.Items.Add(noFolderItem);
                }
                contextMenu.Items.Add(moveMenuItem);

                var deleteMenuItem = new MenuItem { Header = "❌ Delete Track" };
                deleteMenuItem.Click += (s, e) =>
                {
                    if (_activeFolder != null) {
                        _activeFolder.Tracks.Remove(track);
                        MusicPlaylistManager.SaveLibrary(_library);
                        RenderTracksList();
                    }
                };
                contextMenu.Items.Add(deleteMenuItem);

                trackItemBorder.ContextMenu = contextMenu;

                _tracksPanel.Children.Add(trackItemBorder);
            }
        }

        private void UpdateMassActionBarVisibility()
        {
            if (_massActionBar == null) return;

            if (_selectedTrackIds.Count > 0)
            {
                _massActionTitle.Text = $"Selected: {_selectedTrackIds.Count} tracks";
                
                // Populate destination folders dropdown
                _massDestinationComboBox.Items.Clear();
                foreach (var folder in _library.Folders)
                {
                    if (folder.Id != _activeFolder?.Id)
                    {
                        _massDestinationComboBox.Items.Add(folder.FolderName);
                    }
                }
                
                if (_massDestinationComboBox.Items.Count > 0)
                {
                    _massDestinationComboBox.SelectedIndex = 0;
                    _massDestinationComboBox.IsEnabled = true;
                }
                else
                {
                    _massDestinationComboBox.Items.Add("(No other folders)");
                    _massDestinationComboBox.SelectedIndex = 0;
                    _massDestinationComboBox.IsEnabled = false;
                }

                _massActionBar.Visibility = Visibility.Visible;
            }
            else
            {
                _massActionBar.Visibility = Visibility.Collapsed;
            }
        }

        private void MassMoveTracks()
        {
            if (_activeFolder == null || _selectedTrackIds.Count == 0) return;

            string? targetFolderName = _massDestinationComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(targetFolderName) || targetFolderName == "(No other folders)")
            {
                TextOverlay.Show("⚠️ Select a valid destination folder!", 2500);
                return;
            }

            var destFolder = _library.Folders.FirstOrDefault(f => f.FolderName == targetFolderName);
            if (destFolder == null) return;

            var tracksToMove = _activeFolder.Tracks.Where(t => _selectedTrackIds.Contains(t.Id)).ToList();
            if (tracksToMove.Count == 0) return;

            foreach (var track in tracksToMove)
            {
                _activeFolder.Tracks.Remove(track);
                if (!destFolder.Tracks.Any(t => t.PathOrUrl == track.PathOrUrl))
                {
                    destFolder.Tracks.Add(track);
                }
            }

            _selectedTrackIds.Clear();
            MusicPlaylistManager.SaveLibrary(_library);
            UpdateMassActionBarVisibility();
            RenderTracksList();
            TextOverlay.Show($"📦 Moved {tracksToMove.Count} tracks to '{destFolder.FolderName}'", 2500);
        }

        private void MassDeleteTracks()
        {
            if (_activeFolder == null || _selectedTrackIds.Count == 0) return;

            var result = MessageBox.Show($"Are you sure you want to delete these {_selectedTrackIds.Count} tracks from the folder?", 
                                         "Confirm Mass Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            _activeFolder.Tracks.RemoveAll(t => _selectedTrackIds.Contains(t.Id));

            _selectedTrackIds.Clear();
            MusicPlaylistManager.SaveLibrary(_library);
            UpdateMassActionBarVisibility();
            RenderTracksList();
            TextOverlay.Show("❌ Selected tracks deleted!", 2500);
        }

        private void RenameTrackPrompt(MusicTrack track)
        {
            InputPromptOverlay.Show($"Rename track '{track.Title}':", (newName) =>
            {
                if (string.IsNullOrWhiteSpace(newName)) return;

                newName = newName.Trim();
                bool wasPlayingCurrent = false;
                TimeSpan resumePosition = TimeSpan.Zero;
                
                // If it is the currently playing local song, release the lock on the file
                if (_currentTrack?.Id == track.Id && _isPlaying && !_currentTrack.IsStreamUrl)
                {
                    wasPlayingCurrent = true;
                    resumePosition = _mediaPlayer.Position;
                    try
                    {
                        _mediaPlayer.Stop();
                        _mediaPlayer.Close();
                    }
                    catch { }
                }

                // If it is a local file, rename the physical file on disk to sync with metadata name
                if (!track.IsStreamUrl && File.Exists(track.PathOrUrl))
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(track.PathOrUrl) ?? string.Empty;
                        string extension = Path.GetExtension(track.PathOrUrl);
                        
                        // Sanitize new name for file system safety
                        string sanitizedName = string.Join("_", newName.Split(Path.GetInvalidFileNameChars())).Trim();
                        if (string.IsNullOrEmpty(sanitizedName))
                        {
                            TextOverlay.Show("⚠️ Invalid track file name!", 3000);
                            return;
                        }
                        
                        string newPath = Path.Combine(directory, sanitizedName + extension);
                        if (newPath != track.PathOrUrl)
                        {
                            if (File.Exists(newPath))
                            {
                                TextOverlay.Show("⚠️ A file with that name already exists!", 3000);
                                if (wasPlayingCurrent)
                                {
                                    _mediaPlayer.Open(new Uri(Path.GetFullPath(track.PathOrUrl)));
                                    _mediaPlayer.Position = resumePosition;
                                    _mediaPlayer.Play();
                                }
                                return;
                            }
                            
                            File.Move(track.PathOrUrl, newPath);
                            track.PathOrUrl = newPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        TextOverlay.Show($"⚠️ Failed to rename local file: {ex.Message}", 3000);
                        if (wasPlayingCurrent)
                        {
                            _mediaPlayer.Open(new Uri(Path.GetFullPath(track.PathOrUrl)));
                            _mediaPlayer.Position = resumePosition;
                            _mediaPlayer.Play();
                        }
                        return;
                    }
                }

                track.Title = newName;
                MusicPlaylistManager.SaveLibrary(_library);
                RenderTracksList();

                if (_currentTrack?.Id == track.Id)
                {
                    _nowPlayingTitle.Text = track.Title;
                    if (wasPlayingCurrent)
                    {
                        try
                        {
                            _mediaPlayer.Open(new Uri(Path.GetFullPath(track.PathOrUrl)));
                            _mediaPlayer.Position = resumePosition;
                            _mediaPlayer.Play();
                        }
                        catch { }
                    }
                }
                
                TextOverlay.Show("Track renamed successfully!", 2000);
            });
        }

        private void MoveTrackToFolder(MusicTrack track, MusicFolder destinationFolder)
        {
            if (_activeFolder == null) return;
            
            _activeFolder.Tracks.Remove(track);
            if (!destinationFolder.Tracks.Any(t => t.PathOrUrl == track.PathOrUrl))
            {
                destinationFolder.Tracks.Add(track);
            }
            
            MusicPlaylistManager.SaveLibrary(_library);
            RenderTracksList();
            TextOverlay.Show($"Moved track to '{destinationFolder.FolderName}'", 2000);
        }
private void CopyTrackToFolder(MusicTrack track, MusicFolder destinationFolder)
        {
            if (_activeFolder == null) return;
            
            //_activeFolder.Tracks.Remove(track);
            if (!destinationFolder.Tracks.Any(t => t.PathOrUrl == track.PathOrUrl))
            {
                destinationFolder.Tracks.Add(track);
            }
            
            MusicPlaylistManager.SaveLibrary(_library);
            RenderTracksList();
            TextOverlay.Show($"Moved track to '{destinationFolder.FolderName}'", 2000);
        }

        private void PlayTrack(MusicTrack track)
        {
            try
            {
                _trackAdvanceTriggered = false;
                _currentTrack = track;
                _nowPlayingTitle.Text = track.Title;
                _nowPlayingArtist.Text = track.IsStreamUrl ? $"Stream Link: {track.PathOrUrl}" : $"Local File: {track.PathOrUrl}";

                // Stop local WPF MediaPlayer if currently playing local audio
                try
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Close();
                }
                catch { }

                // Kill existing web stream process via global tracker + local handle
                try
                {
                    ChromeStreamTracker.KillIfRunning();
                    ChromeRemoteControl.Shutdown();
                    if (_streamProcess != null && !_streamProcess.HasExited)
                    {
                        _streamProcess.Kill(entireProcessTree: true);
                    }
                    _streamProcess = null;
                }
                catch { }

                if (track.IsStreamUrl || track.PathOrUrl.StartsWith("http://") || track.PathOrUrl.StartsWith("https://"))
                {
                    // Enable slider and timer for web streams control via remote debugger
                    _positionSlider.Value = 0;
                    _positionSlider.IsEnabled = true;
                    _timeLabel.Text = "Live Web Stream (Connecting...)";
                    _playTimer.Start();

                    try
                    {
                        // Launch Chrome process using default Google user profile credentials
                        string chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
                        if (!File.Exists(chromePath))
                        {
                            chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
                        }

                        if (File.Exists(chromePath))
                        {
                            var psi = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = chromePath,
                                Arguments = $"--app=\"{track.PathOrUrl}\" --new-window --remote-debugging-port=9222 " +
                                            $"--autoplay-policy=no-user-gesture-required --no-user-gesture-required-for-autoplay " +
                                            $"--user-data-dir=\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Heaplit", "ChromeDebugProfile")}\"",
                                UseShellExecute = true
                            };
                            ChromeStreamTracker.MarkLaunchTime();
                            _streamProcess = System.Diagnostics.Process.Start(psi);
                            ChromeStreamTracker.Set(_streamProcess);
                            NativeMethods.FocusProcessInstance(_streamProcess);
                        }
                        else
                        {
                            // Fallback to Edge app window mode
                            var psi = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "msedge.exe",
                                Arguments = $"--app=\"{track.PathOrUrl}\" --new-window --remote-debugging-port=9222 " +
                                            $"--autoplay-policy=no-user-gesture-required --no-user-gesture-required-for-autoplay " +
                                            $"--user-data-dir=\"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Heaplit", "EdgeDebugProfile")}\"",
                                UseShellExecute = true
                            };
                            ChromeStreamTracker.MarkLaunchTime();
                            _streamProcess = System.Diagnostics.Process.Start(psi);
                            ChromeStreamTracker.Set(_streamProcess);
                        }
                    }
                    catch
                    {
                        // Fallback default browser process launch
                        var fallbackPsi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = track.PathOrUrl,
                            UseShellExecute = true
                        };
                        ChromeStreamTracker.MarkLaunchTime();
                        _streamProcess = System.Diagnostics.Process.Start(fallbackPsi);
                        ChromeStreamTracker.Set(_streamProcess);

                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            // Wait for the browser process and WebSocket debugging target to initialize
                            await System.Threading.Tasks.Task.Delay(2500);

                            try
                            {
                                // Query duration/position to establish CDP connection via ChromeRemoteControl
                                await ChromeRemoteControl.GetPositionAsync(track.PathOrUrl);

                                // Optional: Call your ChromeRemoteControl JS execution helper if available
                                // to force media play on all video and audio DOM nodes:
                                /*
                                string playScript = @"
                                    document.querySelectorAll('video, audio').forEach(m => {
                                        m.muted = false;
                                        m.play().catch(e => console.log('Autoplay trigger: ', e));
                                    });
                                ";
                                await ChromeRemoteControl.ExecuteScriptAsync(track.PathOrUrl, playScript);
                                */
                            }
                            catch { }
                        });
                    }

                    TextOverlay.Show($"🌐 Opening Web Stream (Remote Debugging Enabled):\n{track.Title}", 3000);
                    _isPlaying = true;
                    _playPauseBtn.Content = "⏸️ Pause";
                    RenderTracksList();
                    return;
                }

                // Play local MP3 file using WPF MediaPlayer
                if (!File.Exists(track.PathOrUrl))
                {
                    TextOverlay.Show($"⚠️ Media File Not Found:\n{Path.GetFileName(track.PathOrUrl)}", 3500);
                    return;
                }

                try
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Close();

                    Uri targetUri = new Uri(Path.GetFullPath(track.PathOrUrl), UriKind.Absolute);
                    _mediaPlayer.Open(targetUri);
                    _mediaPlayer.Play();
                    _isPlaying = true;
                    _playPauseBtn.Content = "⏸️ Pause";
                    
                    // Enable position slider and start playhead timer
                    _positionSlider.IsEnabled = true;
                    _playTimer.Start();
                }
                catch
                {
                    // Native Windows Shell Fallback: Launch track in default Windows Media Player app
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Path.GetFullPath(track.PathOrUrl),
                        UseShellExecute = true
                    });
                    TextOverlay.Show($"🎵 Playing in default media app:\n{track.Title}", 3000);
                }

                RenderTracksList();
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Playback failed: {ex.Message}", 3000);
            }
        }

        private void TogglePlayPause()
        {
            if (_currentTrack == null)
            {
                if (_activeFolder != null && _activeFolder.Tracks.Count > 0)
                {
                    PlayTrack(_activeFolder.Tracks[0]);
                }
                return;
            }

            // 1. If currently playing a local MP3 track via WPF MediaPlayer
            if (!_currentTrack.IsStreamUrl && File.Exists(_currentTrack.PathOrUrl))
            {
                if (_isPlaying)
                {
                    _mediaPlayer.Pause();
                    _isPlaying = false;
                    _playPauseBtn.Content = "▶️ Play";
                }
                else
                {
                    _mediaPlayer.Play();
                    _isPlaying = true;
                    _playPauseBtn.Content = "⏸️ Pause";
                }
                return;
            }

            // 2. If playing a web stream or external app, dispatch system-wide Virtual Media Key to toggle Play/Pause
            NativeMethods.SendMediaKey(NativeMethods.VK_MEDIA_PLAY_PAUSE);
            
            _isPlaying = !_isPlaying;
            _playPauseBtn.Content = _isPlaying ? "⏸️ Pause" : "▶️ Play";
        }

        public void PausePlayback()
        {
            if (_isPlaying)
            {
                if (_currentTrack != null && !_currentTrack.IsStreamUrl && File.Exists(_currentTrack.PathOrUrl))
                {
                    _mediaPlayer.Pause();
                }
                else
                {
                    NativeMethods.SendMediaKey(NativeMethods.VK_MEDIA_PLAY_PAUSE);
                }
                _isPlaying = false;
                _playPauseBtn.Content = "▶️ Play";
            }
        }

        private void PlayNextTrack()
        {
            if (_activeFolder == null || _activeFolder.Tracks.Count == 0) return;

            if (_isShuffle)
            {
                int randIndex = new Random().Next(_activeFolder.Tracks.Count);
                PlayTrack(_activeFolder.Tracks[randIndex]);
                return;
            }

            if (_currentTrack == null)
            {
                PlayTrack(_activeFolder.Tracks[0]);
                return;
            }
            int index = _activeFolder.Tracks.FindIndex(t => t.Id == _currentTrack?.Id);
            int nextIndex = (index + 1) % _activeFolder.Tracks.Count;
            PlayTrack(_activeFolder.Tracks[nextIndex]);
        }

        private void PlayPrevTrack()
        {
            if (_activeFolder == null || _activeFolder.Tracks.Count == 0) return;

            if (_isShuffle)
            {
                int randIndex = new Random().Next(_activeFolder.Tracks.Count);
                PlayTrack(_activeFolder.Tracks[randIndex]);
                return;
            }

            if (_currentTrack == null)
            {
                PlayTrack(_activeFolder.Tracks[0]);
                return;
            }
            int index = _activeFolder.Tracks.FindIndex(t => t.Id == _currentTrack?.Id);
            int prevIndex = index <= 0 ? _activeFolder.Tracks.Count - 1 : index - 1;
            PlayTrack(_activeFolder.Tracks[prevIndex]);
        }

        private async void PlayTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentTrack == null) return;

            if (_currentTrack.IsStreamUrl || _currentTrack.PathOrUrl.StartsWith("http://") || _currentTrack.PathOrUrl.StartsWith("https://"))
            {
                if (!_isDraggingSlider)
                {
                    var (current, duration) = await ChromeRemoteControl.GetPositionAsync(_currentTrack.PathOrUrl);
                    if (duration > 0)
                    {
                        _positionSlider.IsEnabled = true;
                        _positionSlider.Maximum = duration;
                        _positionSlider.Value = current;
                        _timeLabel.Text = $"{TimeSpan.FromSeconds(current):mm\\:ss} / {TimeSpan.FromSeconds(duration):mm\\:ss}";

                        // Check if the stream has finished playing (within 1.5 seconds of the end)
                        if (current >= duration - 1.5 && !_trackAdvanceTriggered)
                        {
                            _trackAdvanceTriggered = true;
                            if (_loopMode == LoopMode.Track)
                            {
                                await ChromeRemoteControl.SeekAsync(_currentTrack.PathOrUrl, 0);
                                _trackAdvanceTriggered = false;
                            }
                            else if (_loopMode == LoopMode.Folder)
                            {
                                PlayNextTrack();
                            }
                            else
                            {
                                if (_activeFolder != null)
                                {
                                    int index = _activeFolder.Tracks.FindIndex(t => t.Id == _currentTrack.Id);
                                    if (index >= 0 && index < _activeFolder.Tracks.Count - 1)
                                    {
                                        PlayTrack(_activeFolder.Tracks[index + 1]);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            ChromeStreamTracker.KillIfRunning();
                                            ChromeRemoteControl.Shutdown();
                                            if (_streamProcess != null && !_streamProcess.HasExited)
                                            {
                                                _streamProcess.Kill(entireProcessTree: true);
                                            }
                                        }
                                        catch { }
                                        _streamProcess = null;
                                        _isPlaying = false;
                                        _playPauseBtn.Content = "▶️ Play";
                                        _playTimer.Stop();
                                        _timeLabel.Text = "Live Web Stream (Finished)";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _positionSlider.IsEnabled = false;
                        _positionSlider.Value = 0;
                        _timeLabel.Text = "Live Web Stream (Connecting...)";
                    }
                }
                return;
            }

            if (!_isDraggingSlider && _mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double total = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                double current = _mediaPlayer.Position.TotalSeconds;
                if (total > 0)
                {
                    _positionSlider.IsEnabled = true;
                    _positionSlider.Maximum = 100;
                    _positionSlider.Value = (current / total) * 100.0;
                    _timeLabel.Text = $"{_mediaPlayer.Position:mm\\:ss} / {_mediaPlayer.NaturalDuration.TimeSpan:mm\\:ss}";
                }
            }
        }

        private void ClearFolderConfirm()
        {
            if (_activeFolder == null || _activeFolder.Tracks.Count == 0) return;

            var result = MessageBox.Show($"Are you sure you want to clear all {_activeFolder.Tracks.Count} tracks from '{_activeFolder.FolderName}'?",
                                         "Clear Folder", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _activeFolder.Tracks.Clear();
                MusicPlaylistManager.SaveLibrary(_library);
                RenderTracksList();
                TextOverlay.Show("Folder cleared!", 2000);
            }
        }

        private void CreateNewFolderPrompt()
        {
            InputPromptOverlay.Show("Enter new Playlist Folder name:", (name) =>
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var newF = new MusicFolder { FolderName = name.Trim() };
                    _library.Folders.Add(newF);
                    _activeFolder = newF;
                    _library.LastActiveFolderId = newF.Id;
                    MusicPlaylistManager.SaveLibrary(_library);
                    RefreshFoldersDropDown();
                    RenderTracksList();
                    TextOverlay.Show($"📁 Created Playlist Folder: '{name}'", 2500);
                }
            });
        }

        private void AddFilesFromPaths(string[] filePaths)
        {
            if (_activeFolder == null || filePaths == null || filePaths.Length == 0) return;

            int count = 0;
            string[] extensions = { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" };

            foreach (string path in filePaths)
            {
                if (Directory.Exists(path))
                {
                    // Recursively add files from directory
                    var subFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));
                    foreach (var sf in subFiles)
                    {
                        AddSingleTrack(sf);
                        count++;
                    }
                }
                else if (File.Exists(path) && extensions.Contains(Path.GetExtension(path).ToLower()))
                {
                    AddSingleTrack(path);
                    count++;
                }
            }

            if (count > 0)
            {
                RenderTracksList();
                TextOverlay.Show($"🎵 Added {count} tracks to Playlist!", 2500);
            }
        }

        private void AddSingleTrack(string filePath)
        {
            string fn = Path.GetFileNameWithoutExtension(filePath);
            var track = new MusicTrack
            {
                Title = fn,
                PathOrUrl = filePath,
                IsStreamUrl = false
            };

            if (_activeFolder != null)
            {
                MusicPlaylistManager.AddTrackToFolderAndAllSongs(_library, _activeFolder, track);
                // Force an immediate save to be safe
                MusicPlaylistManager.SaveLibrary(_library);
            }
        }

        private void BrowseAndAddFile()
        {
            if (_activeFolder == null) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Music Audio Files",
                Filter = "Audio Files (*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.wma)|*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.wma|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                AddFilesFromPaths(dialog.FileNames);
            }
        }

        private void AddUrlStreamPrompt()
        {
            if (_activeFolder == null) return;

            InputPromptOverlay.Show("Enter Audio / YouTube / Soundcloud Link to Download MP3:", (input) =>
            {
                if (!string.IsNullOrWhiteSpace(input))
                {
                    ExecuteDownloadProcess(input.Trim());
                }
            });
        }

        private void ShowWebSearchPrompt()
        {
            if (_activeFolder == null) return;
            MusicWebSearchOverlay.Open((url) =>
            {
                ExecuteDownloadProcess(url);
            });
        }

        private void ShowPlaylistImportPrompt()
        {
            if (_activeFolder == null) return;
            InputPromptOverlay.Show("Enter YouTube / SoundCloud / Spotify Playlist Link:", (url) =>
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    MusicPlaylistImportOverlay.Open(url, (selectedUrls) =>
                    {
                        foreach (var trackUrl in selectedUrls)
                        {
                            ExecuteDownloadProcess(trackUrl);
                        }
                    });
                }
            });
        }

        private void ToggleDownloadsDrawer()
        {
            if (_downloadsDrawerGrid == null) return;

            if (!_isDownloadsDrawerOpen)
            {
                _downloadsDrawerGrid.Width = 200;
                _downloadsDrawerGrid.Visibility = Visibility.Visible;
                _isDownloadsDrawerOpen = true;
            }
            else
            {
                _downloadsDrawerGrid.Width = 0;
                _downloadsDrawerGrid.Visibility = Visibility.Collapsed;
                _isDownloadsDrawerOpen = false;
            }
        }

        private void UpdateDownloadsToggleBtnText()
        {
            if (_downloadsToggleBtn == null) return;
            int activeCount = _downloadQueue.Count(q => q.Status == "Pending" || q.Status == "Downloading...");
            _downloadsToggleBtn.Content = $"📥 Downloads ({activeCount})";
        }

        private void AddQueueItemToPanel(DownloadQueueItem item)
        {
            if (_downloadsQueuePanel == null) return;

            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 2, 0, 2),
                BorderThickness = new Thickness(1)
            };
            rowBorder.SetResourceReference(Border.BorderBrushProperty, "WindowBorderBrush");

            var stack = new StackPanel();

            var titleBlock = new TextBlock
            {
                Text = item.Title,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            item.TitleTextBlock = titleBlock;

            var statusBlock = new TextBlock
            {
                Text = $"Status: {item.Status}",
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0)
            };
            statusBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            item.StatusTextBlock = statusBlock;

            var cancelBtn = new Button
            {
                Content = "✕ Cancel",
                FontSize = 10,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 4, 0, 0),
                Cursor = Cursors.Hand
            };
            cancelBtn.SetResourceReference(Button.BackgroundProperty, "HoverBackgroundBrush");
            cancelBtn.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
            cancelBtn.Click += (s, e) =>
            {
                try { item.Cts.Cancel(); } catch { }
                UpdateQueueItemStatus(item, "Cancelled", item.Title);
                cancelBtn.IsEnabled = false;
                cancelBtn.Content = "Cancelled";
            };
            item.CancelButton = cancelBtn;

            stack.Children.Add(titleBlock);
            stack.Children.Add(statusBlock);
            stack.Children.Add(cancelBtn);
            rowBorder.Child = stack;

            item.BorderElement = rowBorder;
            _downloadsQueuePanel.Children.Add(rowBorder);
        }

        private void UpdateQueueItemStatus(DownloadQueueItem item, string status, string? updatedTitle = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                item.Status = status;
                if (item.StatusTextBlock != null)
                {
                    item.StatusTextBlock.Text = $"Status: {status}";
                    if (status == "Finished")
                        item.StatusTextBlock.Foreground = Brushes.LightGreen;
                    else if (status == "Failed" || status == "Cancelled")
                        item.StatusTextBlock.Foreground = Brushes.OrangeRed;
                    else
                        item.StatusTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                }

                if (updatedTitle != null)
                {
                    item.Title = updatedTitle;
                    if (item.TitleTextBlock != null)
                    {
                        item.TitleTextBlock.Text = updatedTitle;
                    }
                }

                // Hide cancel button once the download is terminal
                if ((status == "Finished" || status == "Failed" || status == "Cancelled") && item.CancelButton != null)
                {
                    item.CancelButton.IsEnabled = false;
                    item.CancelButton.Visibility = Visibility.Collapsed;
                }

                UpdateDownloadsToggleBtnText();
            });
        }

        public void ExecuteDownloadProcess(string rawUrl)
        {
            if (_activeFolder == null) return;

            string url = rawUrl.Trim();

            // Guard: do not attempt download if URL is empty or clearly invalid
            if (string.IsNullOrWhiteSpace(url))
            {
                TextOverlay.Show("⚠️ No URL provided for download.", 3000);
                return;
            }

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            // Sanity check: must look like an actual host, not just "https://"
            if (url == "https://" || url == "http://")
            {
                TextOverlay.Show("⚠️ Invalid URL — please enter a full link.", 3000);
                return;
            }

            // Guard: skip if this URL is already saved as a stream track in the active folder
            // — this prevents re-queuing downloads for web stream entries that already exist
            if (_activeFolder.Tracks.Any(t => t.IsStreamUrl && t.PathOrUrl == url))
            {
                return;
            }

            var queueItem = new DownloadQueueItem
            {
                Url = url,
                Status = "Pending",
                Title = "Parsing URL..."
            };

            _downloadQueue.Add(queueItem);
            AddQueueItemToPanel(queueItem);
            UpdateDownloadsToggleBtnText();

            // Auto-open downloads drawer if closed so the user sees progress
            if (!_isDownloadsDrawerOpen)
            {
                ToggleDownloadsDrawer();
            }

            TextOverlay.Show("📥 Added media to downloads queue...", 3000);

            System.Threading.Tasks.Task.Run(async () =>
            {
                var ct = queueItem.Cts.Token;

                if (ct.IsCancellationRequested) return;
                UpdateQueueItemStatus(queueItem, "Downloading...", "Downloading media...");

                // Use robust Downloads directory instead of AppDomain base
                string baseDownloads = PathHandler.GetDownloadsDirectory();
                string musicDir = Path.Combine(baseDownloads, "Music", _activeFolder.FolderName);
                if (!Directory.Exists(musicDir)) Directory.CreateDirectory(musicDir);

                DateTime startTime = DateTime.Now.AddSeconds(-2);
                string outputTemplate = Path.Combine(musicDir, "%(title)s.%(ext)s");
                string? localPath = null;

                string tsOutput = string.Empty;
                // 1. Primary Engine: DownloadMediaRunner (TypeScript CLI Playwright + Lucida fetcher)
                if (!ct.IsCancellationRequested)
                {
                    try
                    {
                        tsOutput = await DownloadMediaRunner.DownloadAsync(url, musicDir);
                        if (tsOutput.StartsWith("Success:"))
                        {
                            string path = tsOutput.Substring("Success:".Length).Replace("\r", "").Trim();
                            if (File.Exists(path))
                            {
                                localPath = path;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        tsOutput = $"Exception: {ex.Message}";
                    }
                }

                // 2. Fallback Engine: yt-dlp / cmd.exe MP3 conversion
                if (!ct.IsCancellationRequested && (string.IsNullOrEmpty(localPath) || !File.Exists(localPath)))
                {
                    try
                    {
                        string ytdlpPath = Path.Combine(PathHandler.GetProjectRoot(), "Modules", "Layer0", "DownloadMedia", "node_modules", "ytdlp-nodejs", "bin", "yt-dlp.exe");
                        string ffmpegDir = Path.Combine(PathHandler.GetProjectRoot(), "Modules", "Layer0", "DownloadMedia", "node_modules", "ytdlp-nodejs", "bin");
                        
                        string executable = File.Exists(ytdlpPath) ? ytdlpPath : "yt-dlp";
                        string ffmpegArg = File.Exists(Path.Combine(ffmpegDir, "ffmpeg.exe")) ? $"--ffmpeg-location \"{ffmpegDir}\"" : "";

                        // Determine download URL (handle Spotify translation)
                        string downloadUrl = url;
                        if (url.Contains("spotify.com/track/"))
                        {
                            UpdateQueueItemStatus(queueItem, "Resolving...", "Fetching Spotify metadata...");
                            var (title, artist) = await GetSpotifyMetadataAsync(url);
                            if (!string.IsNullOrEmpty(title))
                            {
                                downloadUrl = $"ytsearch1:{artist} {title} audio";
                                UpdateQueueItemStatus(queueItem, "Searching...", $"Searching YouTube for '{title}'...");
                            }
                        }

                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = executable,
                            // SPEED: --concurrent-fragments downloads DASH/HLS fragments in parallel
                            // (often 2-4x faster on YouTube). --newline makes progress parseable.
                            Arguments = $"-x --audio-format mp3 --audio-quality 0 --no-playlist --no-mtime --newline --concurrent-fragments 8 {ffmpegArg} -o \"{outputTemplate}\" \"{downloadUrl}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            StandardOutputEncoding = System.Text.Encoding.UTF8,
                            StandardErrorEncoding = System.Text.Encoding.UTF8
                        };

                        using var proc = System.Diagnostics.Process.Start(psi);
                        if (proc != null)
                        {
                            // Live progress: read yt-dlp output line-by-line, surface the song title
                            // and download %/speed/ETA to the queue item. (Also prevents the redirected
                            // pipe from filling and deadlocking, which the old blind WaitForExit risked.)
                            string detectedTitle = "";
                            var destRx  = new System.Text.RegularExpressions.Regex(@"\[download\] Destination: (?<f>.+)");
                            var pctRx   = new System.Text.RegularExpressions.Regex(@"(?<p>\d{1,3}(?:\.\d)?)%");
                            var speedRx = new System.Text.RegularExpressions.Regex(@"at\s+(?<s>[\d.]+\S*/s)");
                            var etaRx   = new System.Text.RegularExpressions.Regex(@"ETA\s+(?<e>[\d:]+)");
                            void HandleLine(string? line)
                            {
                                if (string.IsNullOrWhiteSpace(line)) return;
                                if (string.IsNullOrEmpty(detectedTitle))
                                {
                                    var dm = destRx.Match(line);
                                    if (dm.Success)
                                    {
                                        detectedTitle = Path.GetFileNameWithoutExtension(dm.Groups["f"].Value.Trim());
                                        if (!string.IsNullOrEmpty(detectedTitle))
                                            UpdateQueueItemStatus(queueItem, "Downloading...", detectedTitle);
                                    }
                                }
                                var pm = pctRx.Match(line);
                                if (pm.Success && line.Contains("[download]"))
                                {
                                    string pct = pm.Groups["p"].Value + "%";
                                    var sm = speedRx.Match(line); var em = etaRx.Match(line);
                                    string extra = (sm.Success ? " · " + sm.Groups["s"].Value : "")
                                                 + (em.Success ? " · ETA " + em.Groups["e"].Value : "");
                                    UpdateQueueItemStatus(queueItem, pct + extra);
                                }
                            }
                            proc.OutputDataReceived += (s, e) => HandleLine(e.Data);
                            proc.ErrorDataReceived  += (s, e) => HandleLine(e.Data);
                            proc.BeginOutputReadLine();
                            proc.BeginErrorReadLine();

                            await proc.WaitForExitAsync(ct);
                            if (ct.IsCancellationRequested) { try { proc.Kill(entireProcessTree: true); } catch { } }
                            if (!ct.IsCancellationRequested)
                            {
                                var files = Directory.GetFiles(musicDir, "*.mp3");
                                string? newest = files.Where(f => File.GetLastWriteTime(f) >= DateTime.Now.AddMinutes(-10))
                                                      .OrderByDescending(File.GetLastWriteTime)
                                                      .FirstOrDefault();
                                if (!string.IsNullOrEmpty(newest) && File.Exists(newest) && new FileInfo(newest).Length > 10000)
                                {
                                    localPath = newest;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                }

                if (ct.IsCancellationRequested)
                {
                    UpdateQueueItemStatus(queueItem, "Cancelled");
                    return;
                }

                // If valid MP3 binary was saved to disk:
                if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                {
                    string songTitle = Path.GetFileNameWithoutExtension(localPath);
                    UpdateQueueItemStatus(queueItem, "Finished", songTitle);

                    var newTrack = new MusicTrack
                    {
                        Title = songTitle,
                        PathOrUrl = localPath,
                        IsStreamUrl = false
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MusicPlaylistManager.AddTrackToFolderAndAllSongs(_library, _activeFolder, newTrack);
                        RenderTracksList();
                        TextOverlay.Show($"✅ Downloaded MP3 to Playlist & All Songs:\n'{songTitle}'", 3500);

                        PlayTrack(newTrack);
                    });
                }
                else
                {
                    // Web Stream fallback if all local MP3 engines fail
                    string hostTitle = url;
                    try { hostTitle = new Uri(url).Host; } catch { }
                    UpdateQueueItemStatus(queueItem, "Finished", $"Web Stream ({hostTitle})");

                    // Write error output to DownloadError.log file
                    try
                    {
                        string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                        string logPath = Path.Combine(logDir, "DownloadError.log");
                        File.WriteAllText(logPath, $"URL: {url}\nPrimary Engine Output:\n{tsOutput}\n");
                    }
                    catch { }

                    // Display CLI Output Terminal Overlay with the errors
                    CliOutputOverlay.Show("Media Downloader Error Output", $"Downloader failed for url: {url}\n\nConsole output:\n{tsOutput}");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var streamTrack = new MusicTrack
                        {
                            Title = $"Web Stream ({hostTitle})",
                            PathOrUrl = url,
                            IsStreamUrl = true
                        };

                        MusicPlaylistManager.AddTrackToFolderAndAllSongs(_library, _activeFolder, streamTrack);
                        RenderTracksList();
                        TextOverlay.Show($"⚠️ Download failed, playing Web Stream ({hostTitle})", 4000);

                        PlayTrack(streamTrack);
                    });
                }
            });
        }

        private static async Task<(string title, string artist)> GetSpotifyMetadataAsync(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                string html = await client.GetStringAsync(url);

                string title = "";
                string artist = "";

                var titleMatch = Regex.Match(html, @"<meta\s+property=""og:title""\s+content=""([^""]+)""");
                if (titleMatch.Success) title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value);

                var descMatch = Regex.Match(html, @"<meta\s+property=""og:description""\s+content=""([^""]+)""");
                if (descMatch.Success)
                {
                    string desc = System.Net.WebUtility.HtmlDecode(descMatch.Groups[1].Value);
                    var parts = desc.Split(new[] { '·', '•', '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        artist = parts[1].Trim();
                    }
                }

                if (string.IsNullOrEmpty(title))
                {
                    var titleTagMatch = Regex.Match(html, @"<title>([^<]+)</title>");
                    if (titleTagMatch.Success)
                    {
                        string fullTitle = System.Net.WebUtility.HtmlDecode(titleTagMatch.Groups[1].Value);
                        if (fullTitle.Contains(" | Spotify")) fullTitle = fullTitle.Replace(" | Spotify", "");
                        var hyphenParts = fullTitle.Split('-');
                        if (hyphenParts.Length > 1)
                        {
                            title = hyphenParts[0].Trim();
                            artist = hyphenParts[1].Replace("song by", "").Replace("song and lyrics by", "").Trim();
                        }
                        else
                        {
                            title = fullTitle;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(title))
                {
                    return (title, artist);
                }
            }
            catch { }

            return ("Unknown Spotify Track", "");
        }

        private Button CreateButton(string content, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = content,
                Margin = new Thickness(4, 0, 4, 0),
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI")
            };
            btn.SetResourceReference(Button.BackgroundProperty, "HoverBackgroundBrush");
            btn.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
            btn.Click += onClick;
            return btn;
        }
        private void ToggleShuffle()
        {
            _isShuffle = !_isShuffle;
            if (_shuffleBtn != null)
            {
                _shuffleBtn.Content = _isShuffle ? "🔀 Shuffle: On" : "🔀 Shuffle: Off";
            }
            TextOverlay.Show(_isShuffle ? "🔀 Shuffle Enabled" : "🔀 Shuffle Disabled", 1500);
        }

        private void ToggleLoopMode()
        {
            if (_loopBtn == null) return;

            if (_loopMode == LoopMode.Off)
            {
                _loopMode = LoopMode.Track;
                _loopBtn.Content = "🔂 Track";
                TextOverlay.Show("🔂 Repeat Mode: Current Track", 2000);
            }
            else if (_loopMode == LoopMode.Track)
            {
                _loopMode = LoopMode.Folder;
                _loopBtn.Content = "🔁 Folder";
                TextOverlay.Show("🔁 Repeat Mode: Current Folder", 2000);
            }
            else
            {
                _loopMode = LoopMode.Off;
                _loopBtn.Content = "🔁 Off";
                TextOverlay.Show("🔁 Repeat Mode: Off", 2000);
            }
        }

        public static void ShowOverlay()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new MusicPlaylistOverlay();
                _instance.Show();
            }
            else
            {
                _instance.Activate();
            }
        }
    }
}
