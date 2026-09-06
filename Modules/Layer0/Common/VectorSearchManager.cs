// Developer: heaplyn
// Date: 2026-08-18
// Summary: Integration Manager for Google Cloud Vector Search (Vertex AI).
//          Handles text embedding generation and vector similarity search.
//          Used for high-dimensional semantic retrieval and Godellian evolution.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace HeaplitLauncher
{
    public class VectorSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public double Distance { get; set; }
    }

    public static class VectorSearchManager
    {
        private static readonly HttpClient _http = new HttpClient();

        /// <summary>
        /// Generates a high-dimensional vector for a string of text using Google's embedding model.
        /// </summary>
        public static async Task<float[]> GetEmbeddingAsync(string text)
        {
            var s = CoreRegistry.Data.Settings.Current;
            string project = s.GOOGLE_CLOUD_PROJECT_ID;
            string location = s.GOOGLE_CLOUD_LOCATION;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;

            if (string.IsNullOrEmpty(project) || string.IsNullOrEmpty(token))
                throw new Exception("Google Cloud project or OAuth token missing for embeddings.");

            string url = $"https://{location}-aiplatform.googleapis.com/v1/projects/{project}/locations/{location}/publishers/google/models/text-embedding-004:predict";

            var payload = new
            {
                instances = new[] { new { content = text, task_type = "RETRIEVAL_DOCUMENT" } }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Embedding API Error: {body}");

            using var doc = JsonDocument.Parse(body);
            var values = doc.RootElement.GetProperty("predictions")[0].GetProperty("embeddings").GetProperty("values").EnumerateArray();
            return values.Select(v => (float)v.GetDouble()).ToArray();
        }

        /// <summary>
        /// Queries the Google Vector Search Index for similar items.
        /// </summary>
        public static async Task<List<VectorSearchResult>> SearchSimilarAsync(float[] queryVector, int topK = 5)
        {
            var s = CoreRegistry.Data.Settings.Current;
            string project = s.GOOGLE_CLOUD_PROJECT_ID;
            string location = s.GOOGLE_CLOUD_LOCATION;
            string endpointId = s.GOOGLE_VECTOR_ENDPOINT_ID;
            string token = s.GOOGLE_OAUTH_ACCESS_TOKEN;

            if (string.IsNullOrEmpty(endpointId)) return new List<VectorSearchResult>();

            // Endpoint for matching
            string url = $"https://{location}-aiplatform.googleapis.com/v1/projects/{project}/locations/{location}/indexEndpoints/{endpointId}:findNeighbors";

            var payload = new
            {
                queries = new[] {
                    new {
                        datapoint = new { datapoint_id = "query", feature_vector = queryVector },
                        neighbor_count = topK
                    }
                }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Vector Search API Error: {body}");

            var results = new List<VectorSearchResult>();
            using var doc = JsonDocument.Parse(body);
            var nearestNeighbors = doc.RootElement.GetProperty("nearestNeighbors")[0].GetProperty("neighbors").EnumerateArray();

            foreach (var neighbor in nearestNeighbors)
            {
                results.Add(new VectorSearchResult
                {
                    Id = neighbor.GetProperty("datapoint").GetProperty("datapointId").GetString() ?? "",
                    Distance = neighbor.GetProperty("distance").GetDouble()
                });
            }

            return results;
        }

        /// <summary>
        /// Inserts or updates a datapoint in the Google Vector Search Index (via Cloud Storage ingest normally, here we simulate metadata association).
        /// </summary>
        public static async Task UpsertMemoryAsync(string text, string metadataJson)
        {
            // Note: Cloud Vector Search typically uses batch ingestion from JSONL files in GCS.
            // For a "Live" feel, Heaplit will log these locally then trigger a re-index or use a hybrid approach.
            DebugConsoleOverlay.Log("Vector-Search", $"Queueing semantic ingest: {text.Take(30)}...");

            // Generate embedding locally to associate with the memory
            float[] vector = await GetEmbeddingAsync(text);

            // Associate this vector with the memory locally
            // In a full production env, we'd upload to GCS and call IndexUpdate
        }
    }
}
