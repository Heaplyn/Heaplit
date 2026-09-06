using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace HeaplitLauncher
{
    public static class CodeEditorManager
    {
        public static bool OpenInVSCode(string filePath, int? line = null)
        {
            try
            {
                string args = line.HasValue ? $"--goto \"{filePath}:{line}\"" : $"\"{filePath}\"";
                return RunProcess("code", args) || RunProcess("code.cmd", args);
            }
            catch { return false; }
        }

        public static bool OpenInVisualStudio(string filePath)
        {
            try
            {
                // Find devenv.exe path or use environmental variable if in PATH
                return RunProcess("devenv.exe", $"\"{filePath}\"");
            }
            catch { return false; }
        }

        public static bool OpenInCursor(string filePath)
        {
            try
            {
                return RunProcess("cursor", $"\"{filePath}\"");
            }
            catch { return false; }
        }

        public static bool OpenInJetBrains(string ideName, string filePath)
        {
            // ideName can be idea, pycharm, webstorm, rider, etc.
            try
            {
                return RunProcess(ideName, $"\"{filePath}\"");
            }
            catch { return false; }
        }

        private static bool RunProcess(string fileName, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                // If simple execution fails, try searching in common paths
                string? fullPath = FindExecutable(fileName);
                if (fullPath != null)
                {
                    Process.Start(new ProcessStartInfo(fullPath, args) { UseShellExecute = true });
                    return true;
                }
                return false;
            }
        }

        private static string? FindExecutable(string name)
        {
            // Check common locations if not in PATH
            string[] searchPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "bin", name),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "bin", name),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "cursor", name)
            };

            foreach (var p in searchPaths)
            {
                if (File.Exists(p)) return p;
            }
            return null;
        }

        public static List<string> GetInstalledEditors()
        {
            var list = new List<string>();
            if (FindExecutable("code") != null || CanRun("code")) list.Add("VS Code");
            if (FindExecutable("cursor") != null || CanRun("cursor")) list.Add("Cursor");
            if (CanRun("devenv.exe")) list.Add("Visual Studio");
            if (CanRun("rider64.exe")) list.Add("Rider");
            return list;
        }

        private static bool CanRun(string cmd)
        {
            try
            {
                var psi = new ProcessStartInfo("where", cmd) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(psi);
                p?.WaitForExit();
                return p?.ExitCode == 0;
            }
            catch { return false; }
        }
    }
}
