// Developer: heaplyn
// Date: 2026-08-14
// Summary: Template Cache Engine.
//          Saves custom code templates to disk, lists available templates,
//          and uses LLM reasoning to adapt snippets to specific contexts.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public static class TemplateCacheManager
    {
        private static string GetTemplateDirectory()
        {
            string dataDir = PathHandler.GetDataDirectory();
            string templatesDir = Path.Combine(dataDir, "Templates");
            if (!Directory.Exists(templatesDir))
            {
                Directory.CreateDirectory(templatesDir);
            }
            return templatesDir;
        }

        public static bool SaveTemplate(string name, string content)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(content)) return false;

            try
            {
                // Clean name to be a safe filename
                string cleanName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(GetTemplateDirectory(), cleanName + ".txt");

                File.WriteAllText(filePath, content);
                DebugConsoleOverlay.Log("Templates", $"Saved template '{cleanName}' ({content.Length} chars).");
                return true;
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("Templates Error", $"Failed to save template: {ex.Message}");
                return false;
            }
        }

        public static List<string> ListTemplates()
        {
            try
            {
                string dir = GetTemplateDirectory();
                var files = Directory.GetFiles(dir, "*.txt");
                return files.Select(Path.GetFileNameWithoutExtension).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public static string GetTemplate(string name)
        {
            try
            {
                string cleanName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(GetTemplateDirectory(), cleanName + ".txt");

                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
            }
            catch { }
            return string.Empty;
        }

        public static async Task<string> AdaptTemplateWithAi(string templateName, string adjustments)
        {
            string templateContent = GetTemplate(templateName);
            if (string.IsNullOrEmpty(templateContent))
            {
                return $"Error: Template '{templateName}' not found.";
            }

            try
            {
                TextOverlay.Show($"⚡ Adapting template '{templateName}'...", 3000);
                DebugConsoleOverlay.Log("Templates", $"Requesting AI adjustment for '{templateName}': {adjustments}");

                string prompt = $"You are a code generation utility. Take this code template:\n\n" +
                                $"```\n{templateContent}\n```\n\n" +
                                $"Modify this template according to these instructions: \"{adjustments}\".\n" +
                                $"Return ONLY the modified code. Do not output markdown formatting blocks, and do not explain anything. Just output the raw adjusted code code file.";

                // Use the Unified LLM Router
                string adjustedCode = await CoreRegistry.Intelligence.Llm.AskAsync(prompt, null);
                
                // Strip markdown code fencing if the LLM outputted them anyway
                adjustedCode = StripCodeFences(adjustedCode);

                // Copy to clipboard
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Clipboard.SetText(adjustedCode);
                });

                string msg = $"Template '{templateName}' adapted and copied to Clipboard!";
                TtsManager.Speak("Template adapted and copied to clipboard.");
                TextOverlay.Show("✅ Template copied to clipboard!", 3000);
                DebugConsoleOverlay.Log("Templates", "Adjusted code copied to Clipboard.");

                return adjustedCode;
            }
            catch (Exception ex)
            {
                return $"Error adapting template: {ex.Message}";
            }
        }

        private static string StripCodeFences(string code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            string clean = code.Trim();
            if (clean.StartsWith("```"))
            {
                int start = clean.IndexOf('\n');
                if (start != -1) clean = clean.Substring(start + 1);
                else clean = clean.Substring(3);

                if (clean.EndsWith("```"))
                {
                    clean = clean.Substring(0, clean.Length - 3);
                }
            }
            return clean.Trim();
        }
    }
}
