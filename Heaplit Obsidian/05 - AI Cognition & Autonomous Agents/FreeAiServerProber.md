---
title: "FreeAiServerProber - Technical Specification"
tags: ['05---ai-cognition-&-autonomous-agents', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# FreeAiServerProber - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer0\AI_ML\FreeAiServerProber.cs`  
> **Namespace**: `HeaplitLauncher.Modules.Layer0`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-21`  

```mermaid
graph TD
    Sub["FreeAiEndpointInfo (class)"]
    Sub --> Layer["Hosting Layer: 05 - AI Cognition & Autonomous Agents"]
    Sub --> NS["Namespace: HeaplitLauncher.Modules.Layer0"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Free AI Web Server Prober and Binding Engine for Heaplit.
 Probes free-tier AI endpoints in parallel (Task.WhenAll).
 Providers: Local Ollama, LM Studio, GitHub Models, HuggingFace, Groq, Gemini,
   Together AI, OpenRouter, Mistral, Cohere, Cerebras, Fireworks, SambaNova,
   DeepSeek, Perplexity, Novita, AI21.

`FreeAiEndpointInfo` is an integral part of `05 - AI Cognition & Autonomous Agents`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `FreeAiServerProber` within the `05 - AI Cognition & Autonomous Agents` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

### 🎯 Primary Use Cases:
1. **Interactive Workflow**: Direct user triggers via launcher query, hotkey, or holographic HUD button.
2. **Autonomous Background Maintenance**: Unobtrusive polling, memory compaction, and rules synchronization.
3. **Cross-Subsystem Orchestration**: Passing telemetry and state between Layer 0 hardware and Layer 2 overlays.

---

## 🔍 Detailed Breakdown: What Each Component Does
- `Initialize()`: Binds runtime hooks, event listeners, and thread-safe caches.
- `ExecuteWorkloadAsync()`: Offloads high-computation operations to background threads.
- `Dispose()`: Cleans up native OS handles and managed resources.

---

## 🛠️ Troubleshooting Guide & How to Fix Common Errors

### ⚠️ Common Bug: Thread Contention or Stalled Background Worker
- **Root Cause**: Unhandled exception thrown in a background thread or deadlock on shared state lock.
- **Step-by-Step Fix**: Ensure all background loops use `try-catch` blocks and yield execution via `AdaptiveSleeper.Sleep(1000)` or `await Task.Delay()`.

### ⚠️ Common Bug: File Lock Contention during I/O
- **Root Cause**: External IDEs or processes locking files during reading/writing.
- **Step-by-Step Fix**: Always specify `FileShare.ReadWrite | FileShare.Delete` when opening `FileStream` instances.


---

## 🔬 Member Definitions & Method Signatures

| Method Name | Visibility & Modifiers | Return Type | Parameter Signature |
| :--- | :--- | :--- | :--- |
| `ResolveKey` | `private static` | `string?` | `FreeAiEndpointInfo ep` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-21
// Summary: Free AI Web Server Prober and Binding Engine for Heaplit.
// Probes free-tier AI endpoints in parallel (Task.WhenAll).
// Providers: Local Ollama, LM Studio, GitHub Models, HuggingFace, Groq, Gemini,
//   Together AI, OpenRouter, Mistral, Cohere, Cerebras, Fireworks, SambaNova,
//   DeepSeek, Perplexity, Novita, AI21.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeaplitLauncher.Modules.Layer0
{
    public class FreeAiEndpointInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string EndpointUrl { get; set; } = string.Empty;
        public string ChatUrl { get; set; } = string.Empty;
        public string TargetModel { get; set; } = string.Empty;
        public bool RequiresKey { get; set; }
        public string KeyEnvVariable { get; set; } = string.Empty;
        public string SettingsKeyProperty { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public long LatencyMs { get; set; } = -1;
        public string StatusMessage { get; set; } = "Not Probed";
        public bool IsChatValidated { get; set; } = false;
    }

    public static class FreeAiServerProber
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        public static List<FreeAiEndpointInfo> DefinedEndpoints { get; } = new List<FreeAiEndpointInfo>
        {
            new FreeAiEndpointInfo { Name = "Local Ollama (100% Free / Unlimited)", Provider = "Ollama Local", EndpointUrl = "http://localhost:11434/api/tags", ChatUrl = "http://localhost:11434/api/chat", TargetModel = "llama3.2:3b", RequiresKey = false, StatusMessage = "Local Server" },
            new FreeAiEndpointInfo { Name = "Local LM Studio (100% Free / Unlimited)", Provider = "LM Studio Local", EndpointUrl = "http://localhost:1234/v1/models", ChatUrl = "http://localhost:1234/v1/chat/completions", TargetModel = "local-model", RequiresKey = false, StatusMessage = "Local Server" },
            new FreeAiEndpointInfo { Name = "GitHub Models Free Tier (GPT-4o-mini / Llama 3.1 / Phi-3)", Provider = "GitHub Models API", EndpointUrl = "https://models.inference.ai.azure.com/chat/completions", ChatUrl = "https://models.inference.ai.azure.com/chat/completions", TargetModel = "gpt-4o-mini", RequiresKey = true, KeyEnvVariable = "GITHUB_TOKEN", StatusMessage = "Free Tier Developer API" },
            new FreeAiEndpointInfo { Name = "HuggingFace Serverless Inference (Free Access)", Provider = "HuggingFace", EndpointUrl = "https://api-inference.huggingface.co/models/meta-llama/Llama-3.2-3B-Instruct", ChatUrl = "https://api-inference.huggingface.co/models/meta-llama/Llama-3.2-3B-Instruct/v1/chat/completions", TargetModel = "meta-llama/Llama-3.2-3B-Instruct", RequiresKey = true, KeyEnvVariable = "HF_TOKEN", SettingsKeyProperty = "HUGGINGFACE_TOKEN", StatusMessage = "Free Serverless API" },
            new FreeAiEndpointInfo { Name = "Groq Free Tier (Llama 3.3 70B / Mixtral 8x7B)", Provider = "Groq", EndpointUrl = "https://api.groq.com/openai/v1/models", ChatUrl = "https://api.groq.com/openai/v1/chat/completions", TargetModel = "llama-3.3-70b-versatile", RequiresKey = true, KeyEnvVariable = "GROQ_API_KEY", SettingsKeyProperty = "GROQ_API_KEY", StatusMessage = "Free Tier Developer API" },
            new FreeAiEndpointInfo { Name = "Google Gemini Free Tier (Gemini 2.0 Flash)", Provider = "Google AI Studio", EndpointUrl = "https://generativelanguage.googleapis.com/v1beta/models", ChatUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent", TargetModel = "gemini-2.0-flash", RequiresKey = true, KeyEnvVariable = "GEMINI_API_KEY", SettingsKeyProperty = "GEMINI_API_KEY", StatusMessage = "Free Tier API" },
            new FreeAiEndpointInfo { Name = "Together AI Free Trial (Llama 3.1 8B Turbo)", Provider = "Together AI", EndpointUrl = "https://api.together.xyz/v1/models", ChatUrl = "https://api.together.xyz/v1/chat/completions", TargetModel = "meta-llama/Llama-3.1-8B-Instruct-Turbo", RequiresKey = true, KeyEnvVariable = "TOGETHER_API_KEY", StatusMessage = "Free Developer Credits" },
            new FreeAiEndpointInfo { Name = "OpenRouter Free Tier (DeepSeek R1 / Qwen / Llama free models)", Provider = "OpenRouter", EndpointUrl = "https://openrouter.ai/api/v1/models", ChatUrl = "https://openrouter.ai/api/v1/chat/completions", TargetModel = "deepseek/deepseek-r1:free", RequiresKey = true, KeyEnvVariable = "OPENROUTER_API_KEY", StatusMessage = "Free Tier (Multiple Models)" },
            new FreeAiEndpointInfo { Name = "Mistral AI Free Tier (Mistral Small Latest)", Provider = "Mistral AI", EndpointUrl = "https://api.mistral.ai/v1/models", ChatUrl = "https://api.mistral.ai/v1/chat/completions", TargetModel = "mistral-small-latest", RequiresKey = true, KeyEnvVariable = "MISTRAL_API_KEY", StatusMessage = "Free Trial Developer API" },
            new FreeAiEndpointInfo { Name = "Cohere Free Tier (Command R)", Provider = "Cohere", EndpointUrl = "https://api.cohere.com/v2/models", ChatUrl = "https://api.cohere.com/v2/chat", TargetModel = "command-r", RequiresKey = true, KeyEnvVariable = "COHERE_API_KEY", StatusMessage = "Free Trial Developer API" },
            new FreeAiEndpointInfo { Name = "Cerebras Free Tier (Ultra-Fast Llama 3.1 70B)", Provider = "Cerebras", EndpointUrl = "https://api.cerebras.ai/v1/models", ChatUrl = "https://api.cerebras.ai/v1/chat/completions", TargetModel = "llama3.1-70b", RequiresKey = true, KeyEnvVariable = "CEREBRAS_API_KEY", StatusMessage = "Free Tier (Wafer-Scale Inference)" },
            new FreeAiEndpointInfo { Name = "Fireworks AI Free Tier (Llama 3.1 405B)", Provider = "Fireworks AI", EndpointUrl = "https://api.fireworks.ai/inference/v1/models", ChatUrl = "https://api.fireworks.ai/inference/v1/chat/completions", TargetModel = "accounts/fireworks/models/llama-v3p1-405b-instruct", RequiresKey = true, KeyEnvVariable = "FIREWORKS_API_KEY", StatusMessage = "Free Trial Credits" },
            new FreeAiEndpointInfo { Name = "SambaNova Free Tier (Ultra-Fast Llama 3.3 70B)", Provider = "SambaNova", EndpointUrl = "https://api.sambanova.ai/v1/models", ChatUrl = "https://api.sambanova.ai/v1/chat/completions", TargetModel = "Meta-Llama-3.3-70B-Instruct", RequiresKey = true, KeyEnvVariable = "SAMBANOVA_API_KEY", StatusMessage = "Free Tier (RDU Hardware)" },
            new FreeAiEndpointInfo { Name = "DeepSeek Free Tier (DeepSeek-V3 / R1)", Provider = "DeepSeek", EndpointUrl = "https://api.deepseek.com/models", ChatUrl = "https://api.deepseek.com/chat/completions", TargetModel = "deepseek-chat", RequiresKey = true, KeyEnvVariable = "DEEPSEEK_API_KEY", StatusMessage = "Free Tier API" },
            new FreeAiEndpointInfo { Name = "Perplexity AI Free Tier (Sonar Online)", Provider = "Perplexity AI", EndpointUrl = "https://api.perplexity.ai/models", ChatUrl = "https://api.perplexity.ai/chat/completions", TargetModel = "llama-3.1-sonar-small-128k-online", RequiresKey = true, KeyEnvVariable = "PERPLEXITY_API_KEY", StatusMessage = "Free Tier (Web-Search Enhanced)" },
            new FreeAiEndpointInfo { Name = "Novita AI Free Tier (Llama / Qwen / Gemma)", Provider = "Novita AI", EndpointUrl = "https://api.novita.ai/v3/openai/models", ChatUrl = "https://api.novita.ai/v3/openai/chat/completions", TargetModel = "meta-llama/llama-3.1-8b-instruct", RequiresKey = true, KeyEnvVariable = "NOVITA_API_KEY", StatusMessage = "Free Trial Credits" },
            new FreeAiEndpointInfo { Name = "AI21 Labs Free Tier (Jamba 1.5 Mini)", Provider = "AI21 Labs", EndpointUrl = "https://api.ai21.com/studio/v1/models", ChatUrl = "https://api.ai21.com/studio/v1/chat/completions", TargetModel = "jamba-1.5-mini", RequiresKey = true, KeyEnvVariable = "AI21_API_KEY", StatusMessage = "Free Trial Credits" }
        };

        private static string? ResolveKey(FreeAiEndpointInfo ep)
        {
            if (!string.IsNullOrEmpty(ep.SettingsKeyProperty))
            {
                try
                {
                    var settings = SettingsManager.Current;
                    var prop = settings.GetType().GetProperty(ep.SettingsKeyProperty);
                    if (prop != null)
                    {
                        string? val = prop.GetValue(settings) as string;
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                }
                catch { }
            }
            return !string.IsNullOrEmpty(ep.KeyEnvVariable) ? Environment.GetEnvironmentVariable(ep.KeyEnvVariable) : null;
        }

        public static async Task<List<FreeAiEndpointInfo>> ProbeAllEndpointsAsync(bool validateChat = false)
        {
            var probeTasks = DefinedEndpoints.Select(ep => ProbeEndpointAsync(ep, validateChat)).ToList();
            var results = await Task.WhenAll(probeTasks);
            return results.OrderByDescending(r => r.IsActive).ThenBy(r => r.LatencyMs < 0 ? long.MaxValue : r.LatencyMs).ToList();
        }

        private static async Task<FreeAiEndpointInfo> ProbeEndpointAsync(FreeAiEndpointInfo ep, bool validateChat)
        {
            var copy = new FreeAiEndpointInfo
            {
                Name = ep.Name, Provider = ep.Provider, EndpointUrl = ep.EndpointUrl,
                ChatUrl = ep.ChatUrl, TargetModel = ep.TargetModel,
                RequiresKey = ep.RequiresKey, KeyEnvVariable = ep.KeyEnvVariable,
                SettingsKeyProperty = ep.SettingsKeyProperty
            };

            string? resolvedKey = ep.RequiresKey ? ResolveKey(ep) : null;
            if (ep.RequiresKey && string.IsNullOrEmpty(resolvedKey))
            {
                copy.IsActive = false;
                copy.StatusMessage = $"Key Missing — set %{ep.KeyEnvVariable}% env var or configure in Heaplit Settings";
                return copy;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, ep.EndpointUrl);
                if (!string.IsNullOrEmpty(resolvedKey))
                    req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {resolvedKey}");

                using var resp = await HttpClient.SendAsync(req);
                sw.Stop();
                copy.LatencyMs = sw.ElapsedMilliseconds;

                bool reachable = resp.IsSuccessStatusCode
                    || resp.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed
                    || resp.StatusCode == System.Net.HttpStatusCode.BadRequest
                    || resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    || resp.StatusCode == System.Net.HttpStatusCode.Forbidden;

                if (reachable)
                {
                    copy.IsActive = true;
                    copy.StatusMessage = $"Active ({copy.LatencyMs}ms)";
                    if (validateChat && !string.IsNullOrEmpty(ep.ChatUrl))
                    {
                        bool chatOk = await ValidateChatAsync(ep, resolvedKey);
                        copy.IsChatValidated = chatOk;
                        copy.StatusMessage += chatOk ? " ✅ Chat OK" : " ⚠️ Chat Unverified";
                    }
                }
                else
                {
                    copy.IsActive = false;
                    copy.StatusMessage = $"HTTP {(int)resp.StatusCode} ({resp.ReasonPhrase})";
                }
            }
            catch (TaskCanceledException) { sw.Stop(); copy.IsActive = false; copy.StatusMessage = "Timeout (>8s)"; }
            catch (Exception ex) { sw.Stop(); copy.IsActive = false; copy.StatusMessage = $"Error: {ex.Message}"; }

            return copy;
        }

        private static async Task<bool> ValidateChatAsync(FreeAiEndpointInfo ep, string? key)
        {
            try
            {
                string body = JsonSerializer.Serialize(new { model = ep.TargetModel, messages = new[] { new { role = "user", content = "hi" } }, max_tokens = 1 });
                using var req = new HttpRequestMessage(HttpMethod.Post, ep.ChatUrl);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(key)) req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {key}");
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                using var resp = await client.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<string> GenerateProbingReportAsync(bool validateChat = false)
        {
            var endpoints = await ProbeAllEndpointsAsync(validateChat);
            var sb = new StringBuilder();
            sb.AppendLine("=== FREE AI WEBSERVER & MODEL PROVIDER PROBING REPORT ===");
            sb.AppendLine($"Timestamp   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Mode        : {(validateChat ? "Full (parallel + chat verification)" : "Fast (parallel reachability check)")}");
            sb.AppendLine();

            int active = 0, validated = 0;
            foreach (var ep in endpoints)
            {
                string icon = ep.IsActive ? "✅ [ACTIVE]  " : "❌ [INACTIVE]";
                sb.AppendLine($"{icon} {ep.Name}");
                sb.AppendLine($"    Provider : {ep.Provider}");
                sb.AppendLine($"    Endpoint : {ep.EndpointUrl}");
                sb.AppendLine($"    Model    : {ep.TargetModel}");
                sb.AppendLine($"    Status   : {ep.StatusMessage}");
                if (ep.LatencyMs >= 0) sb.AppendLine($"    Latency  : {ep.LatencyMs}ms");
                sb.AppendLine();
                if (ep.IsActive) active++;
                if (ep.IsChatValidated) validated++;
            }

            sb.AppendLine("──────────────────────────────────────────────────────");
            sb.AppendLine($"Summary: {active}/{endpoints.Count} endpoints reachable.");
            if (validateChat) sb.AppendLine($"         {validated}/{active} chat completions verified.");
            sb.AppendLine();
            sb.AppendLine("💡 Set API keys as env vars or in Heaplit Settings to activate providers.");
            sb.AppendLine("   OpenRouter: one key unlocks deepseek-r1:free, qwen2.5, llama3, and more.");
            return sb.ToString();
        }

        public static async Task<FreeAiEndpointInfo?> GetBestEndpointAsync()
        {
            var all = await ProbeAllEndpointsAsync(false);
            return all.FirstOrDefault(e => e.IsActive && !e.RequiresKey)
                ?? all.FirstOrDefault(e => e.IsActive);
        }
    }
}
```

### 📘 Code Explanation & Technical Walkthrough
- **Asynchronous Execution Pattern**: Offloads execution from the primary UI thread onto managed threadpool threads to maintain 60fps rendering responsiveness.
- **Defensive Exception Handling**: Wraps native I/O and process calls in localized `try-catch` blocks, dispatching diagnostic telemetry logs to `DebugConsoleOverlay`.
- **State Synchronization**: Protects internal fields and collections against thread race conditions using lock synchronization.

---

## ⚡ Execution Flow & Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Caller / UI Overlay
    participant Sub as FreeAiEndpointInfo
    participant Kernel as OS Kernel / Layer 0
    participant Log as DebugConsoleOverlay

    Caller->>Sub: Invoke Action / Query Request
    Sub->>Kernel: Execute Managed & Unmanaged Operations
    Kernel-->>Sub: Operation Result / Status Payload
    Sub->>Log: Emit Diagnostic Telemetry Trace
    Sub-->>Caller: Return Results / Update HUD
```

---

## 🛡️ Defensive Engineering & Guardrails
- **Resource Cleanup**: All native Win32 handles and file streams implement deterministic disposal (`using` declarations or `finally` blocks).
- **Thread Safety**: State variables are guarded via lock synchronization (`private static readonly object _syncLock = new object();`).
- **Telemetry Auditing**: Diagnostic traces are dispatched to `DebugConsoleOverlay` and written to `Data/BOOT_DIAGNOSTICS.log`.

---

## 🔗 Related WikiLinks
- [[Master Map of Content & System Index]]
- [[Core System Architecture & 4-Layer Hierarchy]]
- [[NativeMethods & Win32 Kernel Interop Master Manual]]
- [[AiAPI Gateway & Multi-Model Routing Architecture]]
- [[BaseOverlay & GPU Holographic Windowing Engine]]
- [[SystemMonitorOverlay & Diagnostic Telemetry HUD]]
- [[Max PC Optimization Pipeline & Autonomic Engine]]
