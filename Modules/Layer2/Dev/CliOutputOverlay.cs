// Developer: heaplyn
// Date: 2026-08-18
// Summary: Persistent singleton console terminal output window.
//          Fixed copy-to-clipboard functionality and improved thread-safety.

using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class CliOutputOverlay : BaseOverlay
    {
        private static CliOutputOverlay? _instance;
        private readonly RichTextBox _richTextBox;

        public static void Show(string commandTitle, string outputContent)
        {
            WriteLogToDisk(commandTitle, outputContent);
            Application.Current.Dispatcher.Invoke(() => {
                if (_instance == null || !_instance.IsLoaded) _instance = new CliOutputOverlay();
                _instance.AppendOutput(commandTitle, outputContent);
                _instance.Show();
                _instance.BringToFront();
            });
        }

        private CliOutputOverlay() : base("HEAPLIT SYSTEM TERMINAL", width: 750, height: 500)
        {
            this.Closed += (s, e) => { _instance = null; };

            _richTextBox = new RichTextBox {
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 13,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Document = new FlowDocument()
            };
            _richTextBox.Document.PagePadding = new Thickness(10);
            _richTextBox.SetResourceReference(RichTextBox.ForegroundProperty, "TextPrimaryBrush");

            // Fix: Explicit ContextMenu to prevent default RichTextBox behavior if it causes issues
            var cm = new ContextMenu();
            var copyItem = new MenuItem { Header = "📋 Copy Selection" };
            copyItem.Click += (s, e) => { try { Clipboard.SetText(new TextRange(_richTextBox.Selection.Start, _richTextBox.Selection.End).Text); } catch { } };
            cm.Items.Add(copyItem);
            _richTextBox.ContextMenu = cm;

            this.UserContent = _richTextBox;
        }

        private void AppendOutput(string commandTitle, string outputContent)
        {
            var p = new Paragraph();
            p.Inlines.Add(new Run($">>> [{DateTime.Now:HH:mm:ss}] EXEC: {commandTitle.ToUpper()}\n") { Foreground = Brushes.Lime, FontWeight = FontWeights.Bold });
            
            string clean = string.IsNullOrEmpty(outputContent) ? "[No Output]" : outputContent;
            Brush col = Brushes.White;
            if (clean.ToLower().Contains("error") || clean.ToLower().Contains("fail")) col = Brushes.Tomato;
            
            p.Inlines.Add(new Run(clean + "\n") { Foreground = col });
            p.Inlines.Add(new Run(new string('-', 60) + "\n") { Foreground = Brushes.DimGray });

            _richTextBox.Document.Blocks.Add(p);
            _richTextBox.ScrollToEnd();
        }

        private static void WriteLogToDisk(string commandTitle, string outputContent)
        {
            try {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "Heaplit.log"), $"\n[{DateTime.Now}] {commandTitle}:\n{outputContent}\n");
            } catch { }
        }
    }
}
