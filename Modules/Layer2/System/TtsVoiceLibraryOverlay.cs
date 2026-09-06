// Developer: heaplyn
// Date: 2026-08-14
// Summary: Custom TTS Voice Library Studio. Manage installed system voices and imported personal audio samples.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class TtsVoiceLibraryOverlay : BaseOverlay
    {
        private static TtsVoiceLibraryOverlay? _instance;
        private ComboBox _voiceCombo = null!;
        private Slider _speedSlider = null!;
        private Slider _volumeSlider = null!;
        private StackPanel _localFilesStack = null!;

        public TtsVoiceLibraryOverlay()
            : base("TTS VOICE SELECTOR & STUDIO", width: 540, height: 720)
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var root = new StackPanel { Margin = new Thickness(10) };
            scroll.Content = root;

            // --- Section 1: System Voices ---
            root.Children.Add(CreateHeader("🔊 Installed Windows System Voices"));

            _voiceCombo = new ComboBox { Margin = new Thickness(0, 4, 0, 8), Padding = new Thickness(8, 6, 8, 6), FontSize = 13 };
            foreach (var v in TtsManager.GetInstalledVoices()) _voiceCombo.Items.Add(v);
            _voiceCombo.SelectedItem = SettingsManager.Current.SELECTED_TTS_VOICE;
            _voiceCombo.SelectionChanged += (s, e) => { if (_voiceCombo.SelectedItem is string sel) TtsManager.SetVoice(sel); };
            root.Children.Add(_voiceCombo);

            root.Children.Add(CreateLabel("Speech Speed:"));
            _speedSlider = new Slider { Minimum = -10, Maximum = 10, Value = SettingsManager.Current.TTS_SPEECH_RATE, Margin = new Thickness(0, 2, 0, 8) };
            _speedSlider.ValueChanged += (s, e) => TtsManager.SetRate((int)_speedSlider.Value);
            root.Children.Add(_speedSlider);

            var testBtn = CreateButton("⚡ Test System Voice");
            testBtn.Click += (s, e) => TtsManager.Speak("System voice test. Online and ready.");
            root.Children.Add(testBtn);

            // --- Section 2: Local Audio Files ---
            root.Children.Add(CreateHeader("📂 Personal Audio Files & Custom Triggers"));

            var importBtn = CreateButton("📥 Import New Audio File (MP3/WAV)...");
            importBtn.Click += (s, e) => {
                var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav;*.m4a;*.ogg" };
                if (dlg.ShowDialog() == true) {
                    TtsSampleDownloader.ImportUserCustomVoiceFile(dlg.FileName);
                    RefreshLocalFiles();
                }
            };
            root.Children.Add(importBtn);

            _localFilesStack = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            root.Children.Add(_localFilesStack);

            this.UserContent = scroll;
            RefreshLocalFiles();
        }

        private void RefreshLocalFiles()
        {
            _localFilesStack.Children.Clear();
            var files = TtsSampleDownloader.GetLocalVoiceFiles();
            if (files.Count == 0) {
                _localFilesStack.Children.Add(new TextBlock { Text = "No custom audio files imported yet.", FontSize = 11, FontStyle = FontStyles.Italic, Foreground = Brushes.Gray });
                return;
            }

            foreach (var file in files) {
                var card = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 4), Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)) };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txt = new TextBlock { Text = "🎵 " + file.name, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White };
                Grid.SetColumn(txt, 0); grid.Children.Add(txt);

                var pBtn = CreateButton("🔊"); pBtn.Width = 30;
                pBtn.Click += (s, e) => TtsSampleDownloader.PreviewLocalFile(file.path);
                Grid.SetColumn(pBtn, 1); grid.Children.Add(pBtn);

                var sBtn = CreateButton("Set"); sBtn.Margin = new Thickness(4, 0, 0, 0);
                sBtn.Click += (s, e) => {
                    SettingsManager.Current.CUSTOM_TTS_SAMPLE_PATH = file.path;
                    SettingsManager.Current.CUSTOM_TTS_VOICE_NAME = file.name;
                    SettingsManager.Save();
                    TextOverlay.Show("✅ Active Custom Sound: " + file.name, 2000);
                };
                Grid.SetColumn(sBtn, 2); grid.Children.Add(sBtn);

                card.Child = grid;
                _localFilesStack.Children.Add(card);
            }
        }

        private static TextBlock CreateHeader(string t) => new TextBlock { Text = t, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 5) };
        private static TextBlock CreateLabel(string t) => new TextBlock { Text = t, FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 5, 0, 2) };
        private static Button CreateButton(string c) => new Button { Content = c, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 2, 0, 2), Cursor = Cursors.Hand };

        public static void ShowOverlay() { if (_instance == null || !_instance.IsLoaded) _instance = new TtsVoiceLibraryOverlay(); _instance.Show(); _instance.Activate(); }
    }
}
