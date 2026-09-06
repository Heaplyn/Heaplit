// Developer: heaplyn
// Date: 2026-09-02
// Summary: Maps each LLM provider to the page where a user creates an API key, and opens it in the
//          browser. Lets the settings UI offer a "Get API Key" button that takes the user straight
//          to the right place — easiest possible setup.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HeaplitLauncher
{
    public static class ApiKeyPortals
    {
        public static readonly Dictionary<string, string> Links = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Gemini"]     = "https://aistudio.google.com/apikey",
            ["OpenAI"]     = "https://platform.openai.com/api-keys",
            ["Anthropic"]  = "https://console.anthropic.com/settings/keys",
            ["Groq"]       = "https://console.groq.com/keys",
            ["OpenRouter"] = "https://openrouter.ai/keys",
            ["Mistral"]    = "https://console.mistral.ai/api-keys",
            ["Perplexity"] = "https://www.perplexity.ai/settings/api",
            ["DeepSeek"]   = "https://platform.deepseek.com/api_keys",
            ["X-AI"]       = "https://console.x.ai",
        };

        public static bool Has(string provider) => Links.ContainsKey(provider);

        public static void Open(string provider)
        {
            if (!Links.TryGetValue(provider, out var url)) return;
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { }
        }
    }
}
