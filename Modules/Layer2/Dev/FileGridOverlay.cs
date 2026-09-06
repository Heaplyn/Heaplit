// Developer: heaplyn
// Date: 2026-08-09
// Summary: Glassmorphic file launcher grid overlay displaying pinned files as interactive dashboard cards. Supports visual pinning, opening, and removing files.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class PinnedFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public DateTime PinnedAt { get; set; } = DateTime.Now;
    }

    public class FileGridOverlay : BaseOverlay
    {
        private static FileGridOverlay? _instance;
        private readonly WrapPanel _wrapPanel;

        public static void OpenDashboard()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_instance == null)
                {
                    _instance = new FileGridOverlay();
                }

                _instance.RefreshGrid();
                _instance.Show();
            });
        }

        private FileGridOverlay()
            : base("HEAPLIT FILE LAUNCHPAD", width: 550, height: 420)
        {
            this.Closed += (s, e) => { _instance = null; };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ScrollViewer wrapping WrapPanel for scrollable Grid columns
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(6)
            };

            _wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            scrollViewer.Content = _wrapPanel;
            Grid.SetRow(scrollViewer, 0);
            rootGrid.Children.Add(scrollViewer);

            this.UserContent = rootGrid;
        }

        public void RefreshGrid()
        {
            _wrapPanel.Children.Clear();

            var pinnedFiles = LoadPinnedFiles();

            // Render existing file cards
            foreach (var file in pinnedFiles)
            {
                var card = CreateFileCard(file);
                _wrapPanel.Children.Add(card);
            }

            // Render the special "+" Add Card
            var addCard = CreateAddCard();
            _wrapPanel.Children.Add(addCard);
        }

        private Border CreateFileCard(PinnedFile file)
        {
            var cardBorder = new Border
            {
                Width = 100,
                Height = 100,
                Margin = new Thickness(8),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = file.FilePath
            };

            // Card layout stack
            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4)
            };

            // Icon TextBlock based on extension
            string emoji = GetExtensionEmoji(file.FilePath);
            var iconBlock = new TextBlock
            {
                Text = emoji,
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(iconBlock);

            // Filename text block
            string filename = Path.GetFileName(file.FilePath);
            if (string.IsNullOrEmpty(filename)) filename = file.FilePath;
            
            var textBlock = new TextBlock
            {
                Text = filename,
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 32,
                Margin = new Thickness(0, 6, 0, 0)
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            stack.Children.Add(textBlock);

            cardBorder.Child = stack;

            // Hover interactions
            cardBorder.MouseEnter += (s, e) =>
            {
                cardBorder.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                cardBorder.BorderBrush = (Brush)Application.Current.Resources["SelectedBorderBrush"];
            };
            cardBorder.MouseLeave += (s, e) =>
            {
                cardBorder.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
                cardBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
            };

            // Double click / Click to execute natively
            cardBorder.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    OpenNatively(file.FilePath);
                }
            };

            // Context Menu to unpin
            var contextMenu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Unpin File" };
            deleteItem.Click += (s, e) =>
            {
                UnpinFile(file.FilePath);
                RefreshGrid();
            };
            contextMenu.Items.Add(deleteItem);
            cardBorder.ContextMenu = contextMenu;

            return cardBorder;
        }

        private Border CreateAddCard()
        {
            var addBorder = new Border
            {
                Width = 100,
                Height = 100,
                Margin = new Thickness(8),
                CornerRadius = new CornerRadius(8),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1.5),
                Cursor = Cursors.Hand,
                ToolTip = "Pin a new file to the dashboard"
            };

            // Make border dashed
            var dashedStroke = new DoubleCollection(new double[] { 4, 3 });
            // Since WPF border doesn't support dashed property out of the box without templates, we can style it via hover accents
            
            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var iconBlock = new TextBlock
            {
                Text = "➕",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(iconBlock);

            var textBlock = new TextBlock
            {
                Text = "Pin File",
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            stack.Children.Add(textBlock);

            addBorder.Child = stack;

            // Hover interactions
            addBorder.MouseEnter += (s, e) =>
            {
                addBorder.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
                addBorder.BorderBrush = (Brush)Application.Current.Resources["SelectedBorderBrush"];
            };
            addBorder.MouseLeave += (s, e) =>
            {
                addBorder.Background = Brushes.Transparent;
                addBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            };

            addBorder.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    PromptAndPinFile();
                }
            };

            return addBorder;
        }

        private void PromptAndPinFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select File to Pin to Dashboard",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                PinFile(openFileDialog.FileName);
                RefreshGrid();
            }
        }

        public static string GetPinnedJsonPath()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDir))
            {
                string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Data"));
                if (Directory.Exists(devPath))
                {
                    dataDir = devPath;
                }
                else
                {
                    Directory.CreateDirectory(dataDir);
                }
            }
            return Path.Combine(dataDir, "PinnedFiles.json");
        }

        public static List<PinnedFile> LoadPinnedFiles()
        {
            try
            {
                string path = GetPinnedJsonPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<List<PinnedFile>>(json) ?? new List<PinnedFile>();
                }
            }
            catch { }
            return new List<PinnedFile>();
        }

        public static void SavePinnedFiles(List<PinnedFile> files)
        {
            try
            {
                string path = GetPinnedJsonPath();
                string json = JsonSerializer.Serialize(files, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to save dashboard: {ex.Message}", 3000);
            }
        }

        public static void PinFile(string filePath)
        {
            var files = LoadPinnedFiles();
            
            // Check if already pinned
            if (files.Exists(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
            {
                TextOverlay.Show("ℹ️ File is already pinned to dashboard", 2500);
                return;
            }

            files.Add(new PinnedFile
            {
                FilePath = filePath,
                FriendlyName = Path.GetFileName(filePath),
                PinnedAt = DateTime.Now
            });

            SavePinnedFiles(files);
            TextOverlay.Show($"📌 Pinned: {Path.GetFileName(filePath)}", 2500);
        }

        public static void UnpinFile(string filePath)
        {
            var files = LoadPinnedFiles();
            int removed = files.RemoveAll(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            
            if (removed > 0)
            {
                SavePinnedFiles(files);
                TextOverlay.Show($"🗑️ Unpinned: {Path.GetFileName(filePath)}", 2500);
            }
        }

        private static void OpenNatively(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
                TextOverlay.Show($"🚀 Opening: {Path.GetFileName(filePath)}", 2000);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Open failed: {ex.Message}", 3000);
            }
        }

        private static string GetExtensionEmoji(string path)
        {
            if (Directory.Exists(path)) return "📁";

            string ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".txt":
                case ".md":
                case ".doc":
                case ".docx":
                case ".rtf":
                case ".pdf":
                    return "📄";

                case ".cs":
                case ".xaml":
                case ".js":
                case ".ts":
                case ".json":
                case ".xml":
                case ".html":
                case ".css":
                case ".py":
                case ".cpp":
                case ".h":
                case ".bat":
                case ".ps1":
                case ".vbs":
                case ".sh":
                    return "💻";

                case ".mp3":
                case ".wav":
                case ".ogg":
                case ".flac":
                case ".m4a":
                    return "🎵";

                case ".mp4":
                case ".mkv":
                case ".avi":
                case ".mov":
                case ".wmv":
                    return "🎥";

                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":
                case ".ico":
                case ".svg":
                    return "🖼️";

                case ".exe":
                case ".msi":
                case ".lnk":
                    return "⚙️";

                case ".zip":
                case ".rar":
                case ".7z":
                case ".tar":
                case ".gz":
                    return "📦";

                default:
                    return "📎";
            }
        }

        private static string GetProjectRoot()
        {
            string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            if (Directory.Exists(Path.Combine(devPath, "Modules")))
            {
                return devPath;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
