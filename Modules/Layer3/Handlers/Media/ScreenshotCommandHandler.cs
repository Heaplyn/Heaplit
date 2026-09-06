// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to capture a screenshot of the primary screen and save it to the system Pictures folder.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace HeaplitLauncher
{
    public class ScreenshotCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            query = query.Trim().ToLower();
            return query == "screenshot" || query == "screen capture" || query == "capture" || query.Contains("screenshots");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim().ToLower();

            double similarity = SearchUtil.GetSimilarity(query, "screenshot");

            suggestions.Add(new CommandResult
            {
                TITLE       = "Capture Screenshot",
                DESCRIPTION = "Save a PNG capture of your primary display to your Pictures folder",
                SIMILARITY  = similarity + 0.5,
                EXECUTE     = () => TakeScreenshot()
            });

            if (query.Contains("folder") || query.Contains("open") || query.Contains("view") || query.Contains("recent"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "Open Screenshots Folder",
                    DESCRIPTION = "Open the folder containing automatic memory captures",
                    SIMILARITY = 4.8,
                    EXECUTE = () => OpenScreenshotsFolder()
                });
            }

            return suggestions;
        }

        private static void OpenScreenshotsFolder()
        {
            try
            {
                string path = Path.Combine(PathHandler.GetDataDirectory(), "Screenshots");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            catch { }
        }

        private static void TakeScreenshot()
        {
            try
            {
                var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
                int width = screen.Bounds.Width;
                int height = screen.Bounds.Height;

                using (var bmp = new Bitmap(width, height))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    }

                    string screenshotsDir = Path.Combine(PathHandler.GetDataDirectory(), "Screenshots");
                    if (!Directory.Exists(screenshotsDir)) Directory.CreateDirectory(screenshotsDir);

                    string filename = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string savePath = Path.Combine(screenshotsDir, filename);

                    bmp.Save(savePath, ImageFormat.Png);
                    TextOverlay.Show($"📸 Screenshot Saved to Data:\n{filename}", 3500);
                }
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Screenshot failed: {ex.Message}", 3000);
            }
        }
    }
}
