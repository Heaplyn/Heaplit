// Developer: heaplyn
// Date: 2026-08-09
// Summary: Reusable, glassmorphic input prompt overlay window to gather arguments for CLI commands visually on the screen.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class InputPromptOverlay : BaseOverlay
    {
        private readonly TextBox _inputTextBox;
        private readonly Action<string> _onSubmit;

        public static void Show(string promptMessage, Action<string> onSubmit, string defaultText = "")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var prompt = new InputPromptOverlay(promptMessage, onSubmit, defaultText);
                prompt.Show();
            });
        }

        private InputPromptOverlay(string promptMessage, Action<string> onSubmit, string defaultText)
            : base("HEAPLIT INPUT REQUIRED", width: 420, height: 130)
        {
            _onSubmit = onSubmit;

            var grid = new Grid { Margin = new Thickness(8) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = promptMessage,
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var inputRowGrid = new Grid();
            inputRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _inputTextBox = new TextBox
            {
                Text = defaultText,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI")
            };
            _inputTextBox.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            _inputTextBox.SetResourceReference(TextBox.CaretBrushProperty, "AccentCaretBrush");
            _inputTextBox.SetResourceReference(TextBox.BorderBrushProperty, "SelectedBorderBrush");
            _inputTextBox.KeyDown += TextBox_KeyDown;
            Grid.SetColumn(_inputTextBox, 0);
            inputRowGrid.Children.Add(_inputTextBox);

            var browseButton = new Button
            {
                Content = "📁 Browse...",
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(10, 2, 10, 2),
                Cursor = Cursors.Hand,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI")
            };
            browseButton.SetResourceReference(Button.BackgroundProperty, "HoverBackgroundBrush");
            browseButton.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
            browseButton.Click += (s, e) => BrowseFile();
            Grid.SetColumn(browseButton, 1);
            inputRowGrid.Children.Add(browseButton);

            Grid.SetRow(inputRowGrid, 1);
            grid.Children.Add(inputRowGrid);

            this.UserContent = grid;

            this.Loaded += (s, e) =>
            {
                _inputTextBox.Focus();
                _inputTextBox.SelectAll();
            };
        }

        private void BrowseFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select File"
            };

            if (dialog.ShowDialog() == true)
            {
                _inputTextBox.Text = dialog.FileName;
                _onSubmit?.Invoke(dialog.FileName);
                FadeOutAndClose();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string input = _inputTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    _onSubmit?.Invoke(input);
                    FadeOutAndClose();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                FadeOutAndClose();
                e.Handled = true;
            }
        }
    }
}
