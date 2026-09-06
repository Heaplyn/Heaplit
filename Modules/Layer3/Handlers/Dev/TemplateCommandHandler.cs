// Developer: heaplyn
// Date: 2026-08-14
// Summary: Handles commands to save, list, and import code templates using the Template Cache.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public class TemplateCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "template");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim();
            string lower = q.ToLower();

            // 1. Template List
            if (lower == "template list" || lower == "template")
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🗂️ List All Code Templates",
                    DESCRIPTION = "Show all saved templates in the Cache",
                    EXECUTE = () =>
                    {
                        var list = TemplateCacheManager.ListTemplates();
                        if (list.Count == 0)
                        {
                            TextOverlay.Show("🗂️ Template Cache is empty.", 3000);
                        }
                        else
                        {
                            string formatted = "Saved Templates:\n" + string.Join("\n", list.Select(t => $"- {t}"));
                            ChatOverlay.ShowChat();
                            ChatOverlay.LogConsoleAction("Template Cache", formatted);
                            TextOverlay.Show($"Listed {list.Count} templates in Console.", 3000);
                        }
                    },
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "template") + 8.5 * 0.01)
                });
            }

            // 2. Template Save
            if (lower.StartsWith("template save "))
            {
                string name = q.Substring(14).Trim();
                suggestions.Add(new CommandResult
                {
                    TITLE = $"💾 Save Clipboard as Template '{name}'",
                    DESCRIPTION = "Saves current clipboard text content as a template",
                    EXECUTE = () =>
                    {
                        string clipboardText = string.Empty;
                        try
                        {
                            if (Clipboard.ContainsText())
                            {
                                clipboardText = Clipboard.GetText();
                            }
                        }
                        catch { }

                        if (string.IsNullOrWhiteSpace(clipboardText))
                        {
                            TextOverlay.Show("⚠️ Clipboard does not contain text to save.", 3000);
                        }
                        else
                        {
                            bool success = TemplateCacheManager.SaveTemplate(name, clipboardText);
                            if (success)
                            {
                                TextOverlay.Show($"✅ Saved template '{name}' successfully!", 3000);
                                TtsManager.Speak($"Saved template {name}.");
                            }
                            else
                            {
                                TextOverlay.Show("❌ Failed to save template.", 3000);
                            }
                        }
                    },
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "template") + 8.5 * 0.01)
                });
            }

            // 3. Template Import/Adapt
            if (lower.StartsWith("template import ") || lower.StartsWith("template adapt "))
            {
                string remainder = q.Substring(16).Trim();
                string[] parts = remainder.Split(new[] { ' ' }, 2);
                string templateName = parts[0];
                string adjustments = parts.Length > 1 ? parts[1] : "No adjustments specified";

                suggestions.Add(new CommandResult
                {
                    TITLE = $"⚡ Import & Adapt Template '{templateName}'",
                    DESCRIPTION = $"Adjusts template with AI: \"{adjustments}\"",
                    EXECUTE = () =>
                    {
                        Task.Run(async () =>
                        {
                            string result = await TemplateCacheManager.AdaptTemplateWithAi(templateName, adjustments);
                            if (result.StartsWith("Error:"))
                            {
                                TextOverlay.Show($"❌ {result}", 4000);
                            }
                        });
                    },
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "template") + 8.5 * 0.01)
                });
            }

            // Default suggestion helper
            if (suggestions.Count == 0)
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "🗂️ Template Cache commands",
                    DESCRIPTION = "Usage: template list | template save [name] | template import [name] [changes]",
                    EXECUTE = () =>
                    {
                        TextOverlay.Show("Usage: template list | template save [name] | template import [name] [changes]", 4000);
                    },
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "template") + 3.0 * 0.01)
                });
            }

            return suggestions;
        }
    }
}
