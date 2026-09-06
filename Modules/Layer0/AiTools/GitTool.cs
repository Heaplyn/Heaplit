using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace HeaplitLauncher.AiTools
{
    public class GitTool : IAiTool
    {
        public string Tag => "GIT";
        public string RegexPattern => @"@git\{(?<cmd>.*?)\}";

        public async Task<string> ExecuteAsync(Match match, HashSet<string> executedTags)
        {
            string args = match.Groups["cmd"].Value.Trim();
            if (!executedTags.Add("GIT:" + args.GetHashCode())) return "";

            string root = PathHandler.GetProjectRoot();
            var output = new StringBuilder();

            var startInfo = new ProcessStartInfo {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            return await Task.Run(() => {
                try {
                    using var process = Process.Start(startInfo);
                    if (process == null) return "[GIT ERROR] Failed to start process.";
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    return $"[GIT {args}]:\n{stdout}\n{stderr}\n";
                } catch (Exception ex) { return $"[GIT ERROR]: {ex.Message}\n"; }
            });
        }
    }
}
