// Developer: heaplyn
// Date: 2026-08-18
// Summary: AI Auto-Evolution & Code Self-Mutation Engine.
//          Enhanced to support partial code modification and full project backups.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace HeaplitLauncher
{
    public static class SelfMutationEngine
    {
        public static string MutationStatus { get; private set; } = "Idle.";
        public static string MutationLogs { get; private set; } = "";

        public static async Task<MutationResult> ModifyCodeAsync(string relPath, string search, string replace)
        {
            string fullPath = Path.Combine(PathHandler.GetProjectRoot(), relPath);
            if (!File.Exists(fullPath)) return new MutationResult(false, $"File not found: {relPath}");

            string content = File.ReadAllText(fullPath);
            if (!content.Contains(search)) return new MutationResult(false, "Search string not found in target file.");

            string newContent = content.Replace(search, replace);
            return await MutateCodeAsync(fullPath, newContent);
        }

        public static async Task<MutationResult> MutateCodeAsync(string targetFilePath, string newCodeContent)
        {
            if (!File.Exists(targetFilePath)) return new MutationResult(false, "Target not found.");

            MutationStatus = "Evolving: Creating full system backup...";
            await SelfBackupManager.CreateBackupAsync("pre_mutation");

            string originalContent = File.ReadAllText(targetFilePath);

            try
            {
                File.WriteAllText(targetFilePath, newCodeContent);
            }
            catch (Exception ex)
            {
                return new MutationResult(false, $"Write failed: {ex.Message}");
            }

            MutationStatus = "Evolving: Verifying Neural Integrity (Build)...";
            bool buildSuccess = await RunBuildCheckAsync();

            if (buildSuccess)
            {
                DebugConsoleOverlay.Log("Evolution-Code", $"Mutation successful in {Path.GetFileName(targetFilePath)}. Sir, I'm restarting to apply changes.");
                _ = Task.Delay(2000).ContinueWith(_ => {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => { try { NativeMethods.Restart(); } catch { Environment.Exit(0); } });
                });
                return new MutationResult(true, "Evolution successful. System rebooting.");
            }
            else
            {
                // Revert
                File.WriteAllText(targetFilePath, originalContent);
                return new MutationResult(false, "Build failed. Mutation reverted for safety.");
            }
        }

        private static async Task<bool> RunBuildCheckAsync()
        {
            string projectDir = PathHandler.GetProjectRoot();
            var startInfo = new ProcessStartInfo {
                FileName = "dotnet", Arguments = "build", WorkingDirectory = projectDir,
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };

            return await Task.Run(() => {
                try {
                    using var process = Process.Start(startInfo);
                    if (process == null) return false;
                    process.WaitForExit(45000);
                    return process.ExitCode == 0;
                } catch { return false; }
            });
        }
    }

    public class MutationResult
    {
        public bool Success { get; }
        public string Message { get; }
        public MutationResult(bool success, string msg) { Success = success; Message = msg; }
    }
}
