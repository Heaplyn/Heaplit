using System;
using System.Windows;

namespace HeaplitLauncher
{
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string message, double progress)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Text = message;
                LoadingProgress.Value = progress;
            });
        }
    }
}
