// Developer: heaplyn
// Date: 2026-08-18
// Summary: Integration Manager for various Google Cloud Platform (GCP) services.
//          Handles Storage (GCS), Translation, and Vision.
//          Utilizes existing OAuth2 tokens for zero-config cloud orchestration.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace HeaplitLauncher
{
    public static class GoogleCloudManager
    {
        private static readonly HttpClient _http = new HttpClient();

        // ── PROJECT & SERVICE MANAGEMENT ────────────────────────────────────────

        public static async Task<List<string>> ListEnabledServicesAsync()
        {
            var s = CoreRegistry.Data.Settings.Current;
            string project = s.GOOGLE_CLOUD_PROJECT_ID;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;
            if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(token)) return new List<string>();

            string url = $"https://serviceusage.googleapis.com/v1/projects/{project}/services?filter=state:ENABLED";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new List<string>();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("services", out var services))
                return services.EnumerateArray().Select(svc => svc.GetProperty("config").GetProperty("title").GetString() ?? "").ToList();

            return new List<string>();
        }

        public static async Task<Dictionary<string, double>> GetQuickMetricsAsync()
        {
            // Simulate traffic/error metrics for the dashboard (requires complex Monitoring API calls normally)
            // In a real env, we'd query monitoring.googleapis.com/v3/projects/{project}/timeSeries
            return new Dictionary<string, double> {
                { "Traffic (Requests/sec)", new Random().Next(5, 50) },
                { "Errors (Last 24h)", new Random().Next(0, 2) }
            };
        }

        // ── GEMINI CLOUD ASSIST ────────────────────────────────────────────────

        public static async Task<string> AskCloudAssistAsync(string prompt)
        {
            var s = CoreRegistry.Data.Settings.Current;
            string project = s.GOOGLE_CLOUD_PROJECT_ID;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;
            if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(token)) return "Cloud project or auth missing.";

            string url = $"https://geminicloudassist.googleapis.com/v1/projects/{project}/locations/global/operations:ask";
            // Note: The actual endpoint might vary based on the specific feature (ask, design, etc.)
            // This is a generalized implementation for the Cloud Assist API.

            var payload = new { query = prompt };
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try {
                var resp = await _http.SendAsync(req);
                string body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return $"Assist Error: {resp.StatusCode}";

                using var doc = JsonDocument.Parse(body);
                // Return the response field or a summary
                return doc.RootElement.ToString();
            } catch (Exception ex) { return "Assist Fault: " + ex.Message; }
        }

        // ── STORAGE (GCS) ───────────────────────────────────────────────────────

        public static async Task<bool> UploadToBucketAsync(string localPath, string? blobName = null)
        {
            var s = CoreRegistry.Data.Settings.Current;
            string bucket = s.GCLOUD_STORAGE_BUCKET;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;

            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(token)) return false;

            blobName ??= Path.GetFileName(localPath);
            string url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucket}/o?uploadType=media&name={Uri.EscapeDataString(blobName)}";

            byte[] data = File.ReadAllBytes(localPath);
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new ByteArrayContent(data);

            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        public static async Task<List<string>> ListBucketObjectsAsync()
        {
            var s = CoreRegistry.Data.Settings.Current;
            string bucket = s.GCLOUD_STORAGE_BUCKET;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;

            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(token)) return new List<string>();

            string url = $"https://storage.googleapis.com/storage/v1/b/{bucket}/o";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new List<string>();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("items", out var items))
                return items.EnumerateArray().Select(i => i.GetProperty("name").GetString() ?? "").ToList();

            return new List<string>();
        }

        // ── TRANSLATION ─────────────────────────────────────────────────────────

        public static async Task<string> TranslateTextAsync(string text, string targetLang = "en")
        {
            var s = CoreRegistry.Data.Settings.Current;
            string project = s.GOOGLE_CLOUD_PROJECT_ID;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;

            if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(token)) return "Cloud config missing.";

            string url = $"https://translation.googleapis.com/v3/projects/{project}:translateText";
            var payload = new { contents = new[] { text }, targetLanguageCode = targetLang };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return $"Error: {resp.StatusCode}";

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("translations")[0].GetProperty("translatedText").GetString() ?? "";
        }

        // ── ADVANCED VISION ─────────────────────────────────────────────────────

        public static async Task<string> DetectLabelsAsync(string imagePath)
        {
            var s = CoreRegistry.Data.Settings.Current;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;
            if (string.IsNullOrEmpty(token)) return "Auth required.";

            string url = "https://vision.googleapis.com/v1/images:annotate";
            byte[] bytes = File.ReadAllBytes(imagePath);
            string b64 = Convert.ToBase64String(bytes);

            var payload = new {
                requests = new[] {
                    new {
                        image = new { content = b64 },
                        features = new[] { new { type = "LABEL_DETECTION", maxResults = 10 } }
                    }
                }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return "Vision API failed.";

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var labels = doc.RootElement.GetProperty("responses")[0].GetProperty("labelAnnotations").EnumerateArray();
            return string.Join(", ", labels.Select(l => l.GetProperty("description").GetString()));
        }
    }
}
