// Developer: heaplyn
// Date: 2026-09-06
// Summary: Command handler for converting PNG images, spritesheet grids, and character folders to TTF font files.
//          Supports commands: "png2ttf", "png to ttf", "font", "fontstudio", "convert font", "make font".

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace HeaplitLauncher
{
    public class FontStudioCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            string q = query.Trim().ToLower();
            if (string.IsNullOrEmpty(q)) return false;

            return q == "font" || q == "fontstudio" || q == "png2ttf" || q == "png to ttf" || q == "png2font"
                || q.StartsWith("font ") || q.StartsWith("png2ttf") || q.StartsWith("png to ttf")
                || q.Contains("convert font") || q.Contains("make font") || q.Contains("create font")
                || q.Contains("png to font") || q.Contains("font generator") || q.Contains("font studio");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string trimmed = query.Trim();
            string lower = trimmed.ToLower();
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double similarity = SearchUtil.BestSimilarity(query, "font", "fontstudio", "png2ttf", "png to ttf", "png2font", "create font", "font studio");

            // Open Font Studio Master
            suggestions.Add(new CommandResult
            {
                TITLE = "🔤 Open PNG ➔ TTF Font Studio",
                DESCRIPTION = "Auto-vectorize PNG font sheets or glyph folders into usable TrueType (.ttf) fonts",
                SIMILARITY = similarity + 5.0,
                EXECUTE = () => FontStudioOverlay.ShowOverlay()
            });

            // "png2ttf <path>" or "font <path>"
            if (parts.Length >= 2)
            {
                string target = trimmed.Substring(parts[0].Length).Trim().Trim('"', '\'');
                if (!string.IsNullOrEmpty(target) && (File.Exists(target) || Directory.Exists(target)))
                {
                    string fontName = Path.GetFileNameWithoutExtension(target) + "Font";
                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"⚡ Auto Convert '{Path.GetFileName(target)}' ➔ TTF Font",
                        DESCRIPTION = $"Generate TrueType font '{fontName}.ttf' and apply to Heaplit HUD",
                        SIMILARITY = similarity + 6.0,
                        EXECUTE = () =>
                        {
                            Task.Run(async () =>
                            {
                                await PngToTtfEngine.ConvertAndApplyToHeaplitAsync(target, fontName);
                            });
                        }
                    });
                }
            }

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("font", "Open PNG to TTF Font Studio", "font"),
                new CommandDesc("png2ttf", "Open PNG to TTF Font Studio", "png2ttf"),
                new CommandDesc("png2ttf [file.png]", "Convert PNG alphabet sheet to TrueType font", "png2ttf fontsheet.png")
            };
        }
    }
}
