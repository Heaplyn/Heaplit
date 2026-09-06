// Developer: heaplyn
// Date: 2026-08-16
// Summary: Universal Project Compiler & Build Orchestrator.
//          Supports C#, C++, Rust, Python (Nuitka/PyInstaller), and Node.js.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class BuildSystemManager
    {
        public static async Task<string> BuildProjectAsync(string language, string projectPath, string options = "")
        {
            try
            {
                if (!Directory.Exists(projectPath) && !File.Exists(projectPath))
                    return $"Error: Path not found: {projectPath}";

                string lang = language.ToLower().Trim();
                TextOverlay.Show($"🛠️ Building {lang.ToUpper()} Project...", 4000);

                return lang switch
                {
                    "csharp" or "cs" or "dotnet" => await RunBuildAsync("dotnet", $"build {options}", projectPath),
                    "cpp" or "c++" or "gcc"    => await RunBuildAsync("cmake", $"--build . {options}", projectPath),
                    "rust" or "rs" or "cargo"  => await RunBuildAsync("cargo", $"build --release {options}", projectPath),
                    "python" or "py"           => await RunBuildAsync("python", $"-m PyInstaller --onefile {options} {projectPath}", Path.GetDirectoryName(projectPath)!),
                    "node" or "js" or "npm"    => await RunBuildAsync("npm", $"run build {options}", projectPath),
                    _ => $"Error: Unsupported build language: {language}"
                };
            }
            catch (Exception ex)
            {
                return $"Build Exception: {ex.Message}";
            }
        }

        private static async Task<string> RunBuildAsync(string command, string args, string workingDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"> Executing: {command} {args}");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return $"Error: Failed to launch {command}. Is it in your PATH?";

                var outTask = proc.StandardOutput.ReadToEndAsync();
                var errTask = proc.StandardError.ReadToEndAsync();

                await Task.WhenAll(outTask, errTask);
                proc.WaitForExit();

                sb.AppendLine(outTask.Result);
                sb.AppendLine(errTask.Result);

                if (proc.ExitCode == 0)
                {
                    TextOverlay.Show($"✅ {command.ToUpper()} Build Successful!", 3000);
                    return $"SUCCESS: Build completed with code 0.\n\n{sb}";
                }
                else
                {
                    TextOverlay.Show($"❌ {command.ToUpper()} Build Failed (Code {proc.ExitCode})", 5000);
                    return $"FAILURE: Build exited with code {proc.ExitCode}.\n\n{sb}";
                }
            }
            catch (Exception ex)
            {
                return $"Execution Error: {ex.Message}. Make sure '{command}' is installed and configured in your system environment.";
            }
        }
    }
}
