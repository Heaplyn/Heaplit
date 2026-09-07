// Developer: heaplyn
// Date: 2026-09-06
// Summary: Bridges Heaplit Assistant to the PNG -> TTF Font Vectorizer and Font Generator engine.
//          Supports converting single character PNGs, spritesheet alphabets, and folders of glyphs
//          into functional TrueType (.ttf) fonts and registering/applying them into Heaplit HUD.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class PngToTtfEngine
    {
        public static string ConverterScriptPath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string scriptPath = Path.Combine(baseDir, "Modules", "Layer0", "Common", "PngToTtfConverter.py");
                if (File.Exists(scriptPath)) return scriptPath;

                // Walk up to find project root if running from bin
                string current = baseDir;
                for (int i = 0; i < 5; i++)
                {
                    string candidate = Path.Combine(current, "Modules", "Layer0", "Common", "PngToTtfConverter.py");
                    if (File.Exists(candidate)) return candidate;
                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }
                return scriptPath;
            }
        }

        /// <summary>
        /// Automatically converts a PNG image or folder of PNGs to a TrueType font (.ttf).
        /// </summary>
        /// <param name="inputPath">Path to PNG file or folder with character PNGs.</param>
        /// <param name="outputPath">Output .ttf file path (defaults to same name in same folder).</param>
        /// <param name="fontName">Name of the font family.</param>
        /// <param name="threshold">Binarization threshold (0-255, default 128).</param>
        /// <param name="invert">Whether to invert the foreground/background mask.</param>
        public static async Task<(bool success, string outputFilePath, string error)> ConvertAsync(
            string inputPath,
            string outputPath = "",
            string fontName = "",
            int threshold = 128,
            bool invert = false)
        {
            if (string.IsNullOrWhiteSpace(inputPath) || (!File.Exists(inputPath) && !Directory.Exists(inputPath)))
            {
                return (false, "", $"Input path '{inputPath}' does not exist.");
            }

            if (string.IsNullOrWhiteSpace(fontName))
            {
                fontName = Path.GetFileNameWithoutExtension(inputPath);
                if (string.IsNullOrWhiteSpace(fontName)) fontName = "HeaplitCustomFont";
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                if (Directory.Exists(inputPath))
                {
                    outputPath = Path.Combine(inputPath, $"{fontName}.ttf");
                }
                else
                {
                    string dir = Path.GetDirectoryName(inputPath) ?? "";
                    outputPath = Path.Combine(dir, $"{fontName}.ttf");
                }
            }

            return await Task.Run(() =>
            {
                try
                {
                    string script = ConverterScriptPath;
                    if (!File.Exists(script))
                    {
                        return (false, "", $"Font converter engine script not found at '{script}'.");
                    }

                    string args = $"\"{script}\" \"{inputPath}\" -o \"{outputPath}\" -n \"{fontName}\" -t {threshold}";
                    if (invert) args += " --invert";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = args,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc == null) return (false, "", "Failed to start Python font conversion process.");

                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(30000);

                    if (proc.ExitCode == 0 && File.Exists(outputPath))
                    {
                        return (true, outputPath, "");
                    }
                    else
                    {
                        string err = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                        return (false, "", string.IsNullOrWhiteSpace(err) ? "Conversion failed with unknown error." : err);
                    }
                }
                catch (Exception ex)
                {
                    return (false, "", ex.Message);
                }
            });
        }

        /// <summary>
        /// Converts PNG to TTF and automatically applies it as the active Heaplit HUD font.
        /// </summary>
        public static async Task<bool> ConvertAndApplyToHeaplitAsync(string inputPath, string fontName = "")
        {
            var (success, ttfPath, error) = await ConvertAsync(inputPath, fontName: fontName);
            if (!success || !File.Exists(ttfPath))
            {
                TextOverlay.Show($"❌ Font conversion failed: {error}", 3500);
                return false;
            }

            try
            {
                SettingsManager.Current.CUSTOM_FONT_PATH = ttfPath;
                if (!string.IsNullOrWhiteSpace(fontName))
                {
                    SettingsManager.Current.CUSTOM_FONT_FAMILY = fontName;
                }
                SettingsManager.Save();
                ThemeManager.ApplyFont(ttfPath);

                TextOverlay.Show($"✨ Font Generated & Applied: {Path.GetFileName(ttfPath)}", 3000);
                return true;
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Font created but failed to apply: {ex.Message}", 3000);
                return false;
            }
        }
    }
}
