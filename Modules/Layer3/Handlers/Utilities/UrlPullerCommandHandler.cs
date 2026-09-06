// Developer: heaplyn
// Date: 2026-08-21
// Summary: Command handler for pulling data from URLs using custom configurations from the Command Bar.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class UrlPullerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.ToLower().Trim();
            return query.StartsWith("pull ") || query == "pull";
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var results = new List<CommandResult>();
            string remainder = query.Length > 5 ? query.Substring(5).Trim() : string.Empty;

            results.Add(new CommandResult
            {
                TITLE = $"🌐 Pull Content from URL",
                DESCRIPTION = string.IsNullOrEmpty(remainder) ? "pull <url> or pull <json_config>" : $"Execute HTTP request to: {remainder}",
                SIMILARITY = string.IsNullOrEmpty(remainder) ? 1.0 : 8.0,
                EXECUTE = () => ExecutePullCommand(remainder)
            });

            return results;
        }

        private async void ExecutePullCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                TextOverlay.Show("❌ Please specify a URL or JSON config.", 3000);
                return;
            }

            TextOverlay.Show("🌐 Executing pull request...", 2000);
            string response = string.Empty;

            try
            {
                if (parameter.StartsWith("{") && parameter.EndsWith("}"))
                {
                    // JSON Config mode
                    var config = JsonSerializer.Deserialize<PullRequestConfig>(parameter, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (config != null)
                    {
                        response = await UrlPullerManager.PullAsync(config);
                    }
                    else
                    {
                        response = "Error parsing PullRequestConfig JSON.";
                    }
                }
                else
                {
                    // Direct URL GET mode
                    var config = new PullRequestConfig { Url = parameter };
                    response = await UrlPullerManager.PullAsync(config);
                }
            }
            catch (Exception ex)
            {
                response = $"Error: {ex.Message}";
            }

            // Display result
            Application.Current.Dispatcher.Invoke(() =>
            {
                var viewWindow = new Window
                {
                    Title = "Heaplit Web Puller Result",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 15, 15, 25)),
                    Foreground = System.Windows.Media.Brushes.White
                };
                var box = new TextBox
                {
                    Text = response,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Foreground = System.Windows.Media.Brushes.White,
                    Padding = new Thickness(10)
                };
                viewWindow.Content = box;
                viewWindow.Show();
            });
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            var list = new List<CommandDesc>();
            list.Add(new CommandDesc("pull <url>", "Pull raw text content from target URL", "pull https://api.ipify.org"));
            list.Add(new CommandDesc("pull <json_config>", "Execute HTTP request with custom headers/cookies config", "pull {\"Url\": \"https://httpbin.org/headers\", \"Headers\": {\"X-Heaplit\": \"Active\"}}"));
            return list;
        }

        public void OnStart() { }
    }
}
