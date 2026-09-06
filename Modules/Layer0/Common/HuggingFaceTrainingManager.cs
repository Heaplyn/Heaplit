// Developer: heaplyn
// Date: 2026-08-15
// Summary: Autonomous Hugging Face Dataset & Training Manager.
//          Collects user-AI interaction logs, cleans them, and uploads them to a private HF dataset.
//          This enables constant "Self-Learning" by building a fine-tuning dataset in the cloud.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class HuggingFaceTrainingManager
    {
        private static bool IsRunning = false;
        private static readonly HttpClient _httpClient = new HttpClient();

        public static void Start()
        {
            if (IsRunning) return;
            if (string.IsNullOrEmpty(SettingsManager.Current.HUGGINGFACE_API_KEY)) return;

            IsRunning = true;
            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    if (SettingsManager.Current.ENABLE_HF_AUTO_TRAINING)
                    {
                        try
                        {
                            await ProcessTrainingCycleAsync();
                        }
                        catch (Exception ex)
                        {
                            DebugConsoleOverlay.Log("HF-Training-Error", ex.Message);
                        }
                    }

                    // Run every 4 hours to avoid rate limits
                    await AdaptiveSleeper.DelayAsync(TimeSpan.FromHours(4));
                }
            });

            DebugConsoleOverlay.Log("HF-Training", "Hugging Face Auto-Training Engine active.");
        }

        private static async Task ProcessTrainingCycleAsync()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Conversations");
            if (!Directory.Exists(logDir)) return;

            var files = Directory.GetFiles(logDir, "*.txt");
            if (files.Length == 0) return;

            var trainingData = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    string content = await File.ReadAllTextAsync(file);
                    // Basic parsing of the custom chat log format
                    var turns = content.Split("==========================================================================", StringSplitOptions.RemoveEmptyEntries);

                    foreach (var turn in turns)
                    {
                        int uIdx = turn.IndexOf("USER: ");
                        int jIdx = turn.IndexOf("HEAPLIT: ");

                        if (uIdx >= 0 && jIdx > uIdx)
                        {
                            string user = turn.Substring(uIdx + 6, jIdx - (uIdx + 6)).Trim();
                            string heaplit = turn.Substring(jIdx + 8).Trim();

                            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(heaplit))
                            {
                                trainingData.Add(new { instruction = user, response = heaplit });
                            }
                        }
                    }
                }
                catch { }
            }

            if (trainingData.Count == 0) return;

            // Upload to Hugging Face
            await UploadDatasetAsync(trainingData);
        }

        private static async Task UploadDatasetAsync(List<object> data)
        {
            string apiKey = SettingsManager.Current.HUGGINGFACE_API_KEY;
            string datasetId = SettingsManager.Current.HF_TRAINING_DATASET_ID;

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(datasetId)) return;

            DebugConsoleOverlay.Log("HF-Training", $"Uploading {data.Count} samples to dataset '{datasetId}'...");

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            // Using HF API to upload/update a file in the dataset
            // Endpoint: https://huggingface.co/api/datasets/{repo_id}/upload/{path}
            string fileName = $"train_{DateTime.Now:yyyyMMdd}.json";
            string url = $"https://huggingface.co/api/datasets/{datasetId}/upload/main/{fileName}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            content.Add(fileContent, "file", fileName);

            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                DebugConsoleOverlay.Log("HF-Training", $"Successfully synced training data to cloud.");
            }
            else
            {
                string err = await response.Content.ReadAsStringAsync();
                DebugConsoleOverlay.Log("HF-Training-Error", $"Upload failed: {response.StatusCode} - {err}");
            }
        }
    }
}
