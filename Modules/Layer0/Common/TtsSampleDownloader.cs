// Developer: heaplyn
// Date: 2026-08-14
// Summary: Manages local custom TTS audio files. Removed GitHub cloud fetching for privacy and speed.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HeaplitLauncher
{
    public class TtsVoiceItem
    {
        public string name { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
    }

    public static class TtsSampleDownloader
    {
        public static readonly string VoiceDirectory = Path.Combine(PathHandler.GetDataDirectory(), "TtsVoices");
        private static MediaPlayer? _previewPlayer;

        static TtsSampleDownloader()
        {
            if (!Directory.Exists(VoiceDirectory))
            {
                Directory.CreateDirectory(VoiceDirectory);
            }
        }

        /// <summary>
        /// Scans the local Data/TtsVoices directory for imported audio files.
        /// </summary>
        public static List<TtsVoiceItem> GetLocalVoiceFiles()
        {
            var voices = new List<TtsVoiceItem>();
            try
            {
                if (Directory.Exists(VoiceDirectory))
                {
                    var files = Directory.GetFiles(VoiceDirectory, "*.*");
                    foreach (var file in files)
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        if (ext == ".mp3" || ext == ".wav" || ext == ".m4a" || ext == ".ogg")
                        {
                            voices.Add(new TtsVoiceItem { name = Path.GetFileName(file), path = file });
                        }
                    }
                }
            }
            catch { }
            return voices;
        }

        public static void PreviewLocalFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _previewPlayer?.Stop();
                _previewPlayer = new MediaPlayer();
                _previewPlayer.Open(new Uri(filePath, UriKind.Absolute));
                _previewPlayer.Play();
            });
        }

        public static void ImportUserCustomVoiceFile(string absolutePath)
        {
            if (!File.Exists(absolutePath) && !Directory.Exists(absolutePath))
            {
                TextOverlay.Show("⚠️ Selection does not exist.", 2500);
                return;
            }

            try
            {
                if (Directory.Exists(absolutePath))
                {
                    // Import as a Voice Pack Directory
                    string folderName = Path.GetFileName(absolutePath);
                    string destDir = Path.Combine(VoiceDirectory, folderName);
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    foreach (var file in Directory.GetFiles(absolutePath, "*.*"))
                    {
                        File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
                    }

                    SettingsManager.Current.CUSTOM_TTS_SAMPLE_PATH = destDir;
                    SettingsManager.Current.CUSTOM_TTS_VOICE_NAME = "Pack: " + folderName;
                    SettingsManager.Current.USE_CUSTOM_TTS_SOUND_FILE = true;
                    SettingsManager.Save();
                    TextOverlay.Show($"✅ Voice Pack Imported:\n{folderName}", 3000);
                }
                else
                {
                    // Import single file
                    string ext = Path.GetExtension(absolutePath).ToLower();
                    if (ext != ".mp3" && ext != ".wav" && ext != ".m4a" && ext != ".ogg")
                    {
                        TextOverlay.Show("⚠️ Unsupported format.", 3500);
                        return;
                    }

                    string fileName = Path.GetFileName(absolutePath);
                    string destPath = Path.Combine(VoiceDirectory, "User_" + fileName);
                    File.Copy(absolutePath, destPath, true);

                    SettingsManager.Current.CUSTOM_TTS_SAMPLE_PATH = destPath;
                    SettingsManager.Current.CUSTOM_TTS_VOICE_NAME = "Custom: " + fileName;
                    SettingsManager.Current.USE_CUSTOM_TTS_SOUND_FILE = true;
                    SettingsManager.Save();
                    TextOverlay.Show($"✅ Custom Voice file imported!", 3000);
                }
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"❌ Import failed: {ex.Message}", 3000);
            }
        }
    }
}
