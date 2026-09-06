// Developer: heaplyn
// Date: 2026-08-14
// Summary: Universal Web Scraper and Automatic Installer Engine.
// Scrapes target website pages, extracts download links for Windows installers, downloads files, and executes them.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class UniversalInstaller
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static UniversalInstaller()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public static async Task<string> InstallFromUrlAsync(string url)
        {
            try
            {
                TextOverlay.Show($"🌐 Scrapes page to find installer links...", 3000);
                string html = await _httpClient.GetStringAsync(url);

                // Regex to find download link references in hrefs
                var linkRegex = new Regex(@"href\s*=\s*[""'](https?://[^""']+\.(?:exe|msi|zip|bat))[""']", RegexOptions.IgnoreCase);
                var matches = linkRegex.Matches(html);

                string? bestDownloadLink = null;

                foreach (Match match in matches)
                {
                    string href = match.Groups[1].Value;
                    
                    // Prioritize windows 64-bit releases or standard install binaries
                    if (href.Contains("win", StringComparison.OrdinalIgnoreCase) || 
                        href.Contains("x64", StringComparison.OrdinalIgnoreCase) || 
                        href.Contains("setup", StringComparison.OrdinalIgnoreCase) ||
                        href.Contains("install", StringComparison.OrdinalIgnoreCase))
                    {
                        bestDownloadLink = href;
                        break;
                    }
                    bestDownloadLink ??= href;
                }

                // If no direct binary link, try scraping standard anchors
                if (string.IsNullOrEmpty(bestDownloadLink))
                {
                    var anchorRegex = new Regex(@"href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                    var anchors = anchorRegex.Matches(html);
                    foreach (Match anchor in anchors)
                    {
                        string href = anchor.Groups[1].Value;
                        if (href.Contains("download", StringComparison.OrdinalIgnoreCase) && href.StartsWith("http"))
                        {
                            bestDownloadLink = href;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(bestDownloadLink))
                {
                    return $"Error: No direct Windows installation executable (.exe/.msi) found on page: {url}";
                }

                TextOverlay.Show($"📥 Downloading installer: {Path.GetFileName(bestDownloadLink)}", 3500);

                string downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(downloadDir)) downloadDir = Path.GetTempPath();

                string fileName = Path.GetFileName(new Uri(bestDownloadLink).AbsolutePath);
                if (string.IsNullOrEmpty(fileName)) fileName = "installer_setup.exe";

                string localFilePath = Path.Combine(downloadDir, fileName);

                using (var response = await _httpClient.GetAsync(bestDownloadLink))
                using (var fs = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                TextOverlay.Show($"🚀 Launching installer: {fileName}", 3000);

                var psi = new ProcessStartInfo
                {
                    FileName = localFilePath,
                    UseShellExecute = true
                };
                Process.Start(psi);

                return $"Successfully scraped page, downloaded installer to: {localFilePath}, and launched execution.";
            }
            catch (Exception ex)
            {
                return $"Error installing from webpage: {ex.Message}";
            }
        }
    }
}
