using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;

namespace HeaplitLauncher.AiTools
{
    public class ClipboardTool : IAiTool
    {
        public string Tag => "CLIP";
        public string RegexPattern => @"@clip_write\{(?<t>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string t = m.Groups["t"].Value;
            Application.Current.Dispatcher.Invoke(() => { try { Clipboard.SetText(t); } catch { } });
            return Task.FromResult($"[CLIPBOARD UPDATED]\n");
        }
    }

    public class RegistryReadTool : IAiTool
    {
        public string Tag => "REG_R";
        public string RegexPattern => @"@reg_read\{(?<p>.*?)\}\{(?<k>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            try {
                string p = m.Groups["p"].Value;
                string k = m.Groups["k"].Value;
                var val = Registry.GetValue(p, k, "NOT_FOUND");
                return Task.FromResult($"[REGISTRY {p}\\{k}]: {val}\n");
            } catch (Exception ex) { return Task.FromResult($"[REG ERROR]: {ex.Message}\n"); }
        }
    }

    public class ArchiveTool : IAiTool
    {
        public string Tag => "ZIP";
        public string RegexPattern => @"@zip\{(?<s>.*?)\}\{(?<d>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            try {
                string s = m.Groups["s"].Value;
                string d = m.Groups["d"].Value;
                if (File.Exists(d)) File.Delete(d);
                ZipFile.CreateFromDirectory(s, d);
                return Task.FromResult($"[ARCHIVE CREATED: {d}]\n");
            } catch (Exception ex) { return Task.FromResult($"[ZIP ERROR]: {ex.Message}\n"); }
        }
    }

    public class ScreenInfoTool : IAiTool
    {
        public string Tag => "SCR";
        public string RegexPattern => @"@monitor_info";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            return Application.Current.Dispatcher.Invoke(() => {
                var w = SystemParameters.PrimaryScreenWidth;
                var h = SystemParameters.PrimaryScreenHeight;
                double dpi = 96.0;
                if (Application.Current.MainWindow != null) dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerInchX;
                return Task.FromResult($"[MONITOR]: {w}x{h}, DPI: {dpi}\n");
            });
        }
    }
}
