using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public class VideoSummarizerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "summarize");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim();
            var parts = q.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            double similarity = SearchUtil.BestSimilarity(query, "summarize"); // High priority match

            if (parts.Length > 1)
            {
                string target = parts[1].Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE = $"🎬 Summarize Content: \"{Path.GetFileName(target)}\"",
                    DESCRIPTION = $"AI-Summarize local video/audio or YouTube URL: {target}",
                    SIMILARITY = similarity,
                    EXECUTE = () => RunSummarization(target)
                });
            }
            else
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🎬 Summarize Video/Audio...",
                    DESCRIPTION = "Prompt for a YouTube URL or local file path to summarize",
                    SIMILARITY = similarity - 0.5,
                    EXECUTE = () => InputPromptOverlay.Show("Enter video URL or local file path:", RunSummarization)
                });
            }

            return suggestions;
        }

        private void RunSummarization(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                TextOverlay.Show("⚠️ Target cannot be empty", 2500);
                return;
            }

            TextOverlay.Show("🎬 Summarizer active. See console overlay...", 3000);

            Task.Run(async () =>
            {
                try
                {
                    string summary = await VideoSummarizer.SummarizeVideoAsync(target, (log) =>
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            TextOverlay.Show(log, 2000);
                            DebugConsoleOverlay.Log("Video Summarizer", log);
                        });
                    });

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CliOutputOverlay.Show("Video/Audio AI Summary", summary);
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        string err = $"⚠️ Summarizer Error: {ex.Message}";
                        TextOverlay.Show(err, 3500);
                        DebugConsoleOverlay.Log("Video Summarizer Error", ex.Message);
                        CliOutputOverlay.Show("Video/Audio Summarizer Error", err);
                    });
                }
            });
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("summarize <url>", "Summarize YouTube video captions/transcript", "summarize https://youtube.com/..."),
                new CommandDesc("summarize <file_path>", "Extract and AI-summarize local video/audio files", "summarize C:\\video.mp4")
            };
        }
    }
}
