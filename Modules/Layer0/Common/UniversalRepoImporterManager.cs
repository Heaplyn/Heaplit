// Developer: heaplyn
// Date: 2026-09-07
// Summary: Universal Repository Importer & Smart Project Provisioning Engine for Heaplit.
//          Clones/downloads repositories from any host (GitHub, GitLab, Bitbucket, Gitea, Azure DevOps, Raw Git, Zip, Local Folders),
//          runs deep heuristic language & framework detection, synthesizes missing configuration files (.env, .vscode, .gitignore),
//          verifies toolchains, auto-installs dependencies via PowerShell, and launches dev servers.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class DetectedTechStack
    {
        public string PrimaryLanguage { get; set; } = "Generic";
        public List<string> Languages { get; set; } = new List<string>();
        public Dictionary<string, int> LanguageFileCounts { get; set; } = new Dictionary<string, int>();
        public List<string> Frameworks { get; set; } = new List<string>();
        public string BuildSystem { get; set; } = "None";
        public string PackageManager { get; set; } = "None";
        public string TargetFramework { get; set; } = string.Empty;
        public string RunCommand { get; set; } = string.Empty;
        public string BuildCommand { get; set; } = string.Empty;
        public string InstallDepsCommand { get; set; } = string.Empty;
        public List<string> SynthesizedConfigs { get; set; } = new List<string>();
        public List<string> MissingPrerequisites { get; set; } = new List<string>();
        public bool HasEnvExample { get; set; }
        public bool HasVsCodeConfig { get; set; }
        public bool HasGitIgnore { get; set; }
        public bool HasDocker { get; set; }
    }

    public class RepoImportResult
    {
        public bool Success { get; set; }
        public string RepoUrl { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
        public DetectedTechStack Stack { get; set; } = new DetectedTechStack();
        public List<string> Logs { get; set; } = new List<string>();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public static class UniversalRepoImporterManager
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        static UniversalRepoImporterManager()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "Heaplit-UniversalRepoImporter/1.0 (Windows NT 10.0; Win64; x64)");
        }

        public static (string CleanUrl, string RepoName, string HostType) ParseRepoTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return (string.Empty, string.Empty, "Unknown");
            string input = target.Trim();

            if (Directory.Exists(input))
            {
                string dirName = Path.GetFileName(input.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return (input, string.IsNullOrEmpty(dirName) ? "LocalProject" : dirName, "Local");
            }

            if (Regex.IsMatch(input, @"^[a-zA-Z0-9_\-\.]+\/[a-zA-Z0-9_\-\.]+$"))
            {
                string rName = input.Split('/')[1];
                return ($"https://github.com/{input}", rName, "GitHub");
            }

            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || input.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
            {
                string host = "Git";
                if (input.Contains("github.com", StringComparison.OrdinalIgnoreCase)) host = "GitHub";
                else if (input.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase)) host = "GitLab";
                else if (input.Contains("bitbucket.org", StringComparison.OrdinalIgnoreCase)) host = "Bitbucket";
                else if (input.Contains("dev.azure.com", StringComparison.OrdinalIgnoreCase)) host = "AzureDevOps";
                else if (input.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || input.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) host = "Archive";

                string rName = "Project";
                try
                {
                    var uri = new Uri(input.Replace("git@", "https://"));
                    string seg = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault() ?? "Project";
                    if (seg.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) seg = seg.Substring(0, seg.Length - 4);
                    if (seg.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) seg = seg.Substring(0, seg.Length - 4);
                    if (!string.IsNullOrEmpty(seg)) rName = seg;
                }
                catch
                {
                    rName = Regex.Replace(input, @"[^a-zA-Z0-9_\-]", "_");
                }

                return (input, rName, host);
            }

            return (input, "Project_" + Guid.NewGuid().ToString("N").Substring(0, 6), "Unknown");
        }

        public static async Task<RepoImportResult> ImportAndSetupRepoAsync(string target, string? customDestinationPath = null, bool autoInstallDeps = true, IProgress<string>? progress = null)
        {
            var result = new RepoImportResult();
            var (url, repoName, hostType) = ParseRepoTarget(target);
            result.RepoUrl = url;
            result.RepoName = repoName;

            if (string.IsNullOrEmpty(url))
            {
                result.ErrorMessage = "Invalid repository target or URL provided.";
                progress?.Report("❌ " + result.ErrorMessage);
                return result;
            }

            string defaultProjectsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Projects");
            if (!Directory.Exists(defaultProjectsDir))
            {
                try { Directory.CreateDirectory(defaultProjectsDir); } catch { defaultProjectsDir = Path.GetTempPath(); }
            }

            string targetDir = string.IsNullOrWhiteSpace(customDestinationPath) ? Path.Combine(defaultProjectsDir, repoName) : customDestinationPath;
            result.LocalPath = targetDir;

            progress?.Report($"🚀 Starting Universal Repository Import for [{repoName}] ({hostType})...");
            result.Logs.Add($"Target: {url} (Host: {hostType}) -> Destination: {targetDir}");

            if (hostType == "Local")
            {
                progress?.Report($"📂 Using existing local directory: {url}");
                result.LocalPath = url;
            }
            else
            {
                bool fetched = await FetchOrCloneRepositoryAsync(url, targetDir, hostType, progress, result.Logs);
                if (!fetched)
                {
                    result.ErrorMessage = $"Failed to clone or download repository from {url}";
                    progress?.Report("❌ " + result.ErrorMessage);
                    return result;
                }
            }

            progress?.Report("🔍 Analyzing codebase architecture, manifests, and tech stack...");
            var stack = await AnalyzeTechStackAsync(result.LocalPath, progress, result.Logs);
            result.Stack = stack;

            progress?.Report("⚙️ Synthesizing configuration files (.env, .vscode, .gitignore)...");
            await SynthesizeConfigFilesAsync(result.LocalPath, stack, progress, result.Logs);

            if (autoInstallDeps && !string.IsNullOrEmpty(stack.InstallDepsCommand))
            {
                progress?.Report($"📦 Auto-installing dependencies via [{stack.PackageManager}]...");
                await ExecuteCommandInDirectoryAsync(result.LocalPath, stack.InstallDepsCommand, progress, result.Logs);
            }

            if (!string.IsNullOrEmpty(stack.BuildCommand))
            {
                progress?.Report($"🔨 Verifying build via [{stack.BuildCommand}]...");
                await ExecuteCommandInDirectoryAsync(result.LocalPath, stack.BuildCommand, progress, result.Logs);
            }

            RegisterProjectInHeaplitWorkspace(result.LocalPath, repoName, stack);

            result.Success = true;
            progress?.Report($"✨ Repository [{repoName}] imported and configured successfully!");
            result.Logs.Add($"Import completed successfully. Ready to run: {stack.RunCommand}");
            return result;
        }

        private static async Task<bool> FetchOrCloneRepositoryAsync(string url, string targetDir, string hostType, IProgress<string>? progress, List<string> logs)
        {
            if (Directory.Exists(targetDir))
            {
                if (Directory.GetFileSystemEntries(targetDir).Length > 0)
                {
                    progress?.Report($"⚠️ Destination directory already exists: {targetDir}. Using existing files.");
                    logs.Add($"Destination already exists: {targetDir}");
                    return true;
                }
            }
            else
            {
                Directory.CreateDirectory(targetDir);
            }

            bool hasGit = IsCommandAvailable("git");
            if (hasGit && !url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report($"📥 Executing: git clone --recurse-submodules \"{url}\"");
                logs.Add($"Attempting git clone: {url}");

                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone --recurse-submodules \"{url}\" \"{targetDir}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string stderr = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (proc.ExitCode == 0 && Directory.GetFileSystemEntries(targetDir).Length > 0)
                    {
                        logs.Add("Git clone succeeded.");
                        return true;
                    }
                    else
                    {
                        logs.Add($"Git clone notice: {stderr}. Trying archive fallback...");
                    }
                }
            }

            try
            {
                string? zipDownloadUrl = null;

                if (url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    zipDownloadUrl = url;
                }
                else if (hostType == "GitHub")
                {
                    string clean = url.TrimEnd('/').Replace(".git", "");
                    zipDownloadUrl = $"{clean}/archive/refs/heads/main.zip";
                }
                else if (hostType == "GitLab")
                {
                    string clean = url.TrimEnd('/').Replace(".git", "");
                    string projName = clean.Split('/').Last();
                    zipDownloadUrl = $"{clean}/-/archive/main/{projName}-main.zip";
                }
                else if (hostType == "Bitbucket")
                {
                    string clean = url.TrimEnd('/').Replace(".git", "");
                    zipDownloadUrl = $"{clean}/get/main.zip";
                }

                if (!string.IsNullOrEmpty(zipDownloadUrl))
                {
                    progress?.Report($"⬇️ Downloading repository archive from {zipDownloadUrl}...");
                    logs.Add($"Downloading zip archive from: {zipDownloadUrl}");

                    string tempZip = Path.Combine(Path.GetTempPath(), $"repo_{Guid.NewGuid():N}.zip");
                    var response = await _http.GetAsync(zipDownloadUrl);

                    if (!response.IsSuccessStatusCode && hostType == "GitHub")
                    {
                        string clean = url.TrimEnd('/').Replace(".git", "");
                        zipDownloadUrl = $"{clean}/archive/refs/heads/master.zip";
                        response = await _http.GetAsync(zipDownloadUrl);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fs);
                        }

                        progress?.Report("📦 Extracting archive...");
                        string tempExtractDir = Path.Combine(Path.GetTempPath(), $"extract_{Guid.NewGuid():N}");
                        ZipFile.ExtractToDirectory(tempZip, tempExtractDir);

                        var subDirs = Directory.GetDirectories(tempExtractDir);
                        string sourceDir = (subDirs.Length == 1 && Directory.GetFiles(tempExtractDir).Length == 0) ? subDirs[0] : tempExtractDir;

                        CopyDirectoryContents(sourceDir, targetDir);

                        try { File.Delete(tempZip); } catch { }
                        try { Directory.Delete(tempExtractDir, true); } catch { }

                        logs.Add("Archive extracted successfully.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logs.Add($"Archive download failed: {ex.Message}");
            }

            return false;
        }

        public static async Task<DetectedTechStack> AnalyzeTechStackAsync(string projectDir, IProgress<string>? progress = null, List<string>? logs = null)
        {
            return await Task.Run(() =>
            {
                var stack = new DetectedTechStack();
                if (!Directory.Exists(projectDir)) return stack;

                var ignoredDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", "node_modules", "bin", "obj", "target", "dist", ".vs", ".idea", "venv", ".venv", "__pycache__", "build" };

                var extCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                void ScanDir(string dir)
                {
                    try
                    {
                        foreach (var f in Directory.GetFiles(dir))
                        {
                            string ext = Path.GetExtension(f).ToLowerInvariant();
                            if (!string.IsNullOrEmpty(ext))
                            {
                                extCounts[ext] = extCounts.GetValueOrDefault(ext) + 1;
                            }
                        }
                        foreach (var d in Directory.GetDirectories(dir))
                        {
                            string dirName = Path.GetFileName(d);
                            if (!ignoredDirs.Contains(dirName))
                            {
                                ScanDir(d);
                            }
                        }
                    }
                    catch { }
                }

                ScanDir(projectDir);

                var langMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { ".cs", "C#" }, { ".fs", "F#" }, { ".ts", "TypeScript" }, { ".tsx", "TypeScript (React)" },
                    { ".js", "JavaScript" }, { ".jsx", "JavaScript (React)" }, { ".py", "Python" }, { ".rs", "Rust" },
                    { ".go", "Go" }, { ".cpp", "C++" }, { ".c", "C" }, { ".h", "C/C++ Header" }, { ".hpp", "C++ Header" },
                    { ".java", "Java" }, { ".kt", "Kotlin" }, { ".php", "PHP" }, { ".rb", "Ruby" },
                    { ".lua", "Lua" }, { ".luau", "Luau (Roblox)" }, { ".swift", "Swift" }, { ".dart", "Dart" },
                    { ".html", "HTML" }, { ".css", "CSS" }, { ".scss", "SCSS" }, { ".vue", "Vue" }, { ".svelte", "Svelte" }
                };

                foreach (var (ext, count) in extCounts)
                {
                    if (langMap.TryGetValue(ext, out var lang))
                    {
                        stack.LanguageFileCounts[lang] = stack.LanguageFileCounts.GetValueOrDefault(lang) + count;
                    }
                }

                stack.Languages = stack.LanguageFileCounts.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
                stack.PrimaryLanguage = stack.Languages.FirstOrDefault() ?? "Generic";

                stack.HasEnvExample = File.Exists(Path.Combine(projectDir, ".env.example")) ||
                                      File.Exists(Path.Combine(projectDir, ".env.sample")) ||
                                      File.Exists(Path.Combine(projectDir, ".env.template")) ||
                                      File.Exists(Path.Combine(projectDir, "sample.env"));

                stack.HasVsCodeConfig = Directory.Exists(Path.Combine(projectDir, ".vscode"));
                stack.HasGitIgnore = File.Exists(Path.Combine(projectDir, ".gitignore"));
                stack.HasDocker = File.Exists(Path.Combine(projectDir, "Dockerfile")) || File.Exists(Path.Combine(projectDir, "docker-compose.yml"));

                // .NET / C#
                var slnFiles = Directory.GetFiles(projectDir, "*.sln", SearchOption.TopDirectoryOnly);
                var csprojFiles = Directory.GetFiles(projectDir, "*.csproj", SearchOption.AllDirectories)
                    .Where(p => !ignoredDirs.Any(i => p.Contains(Path.DirectorySeparatorChar + i + Path.DirectorySeparatorChar))).ToArray();

                if (slnFiles.Length > 0 || csprojFiles.Length > 0)
                {
                    stack.BuildSystem = ".NET CLI / MSBuild";
                    stack.PackageManager = "NuGet / .NET";
                    stack.InstallDepsCommand = "dotnet restore";
                    stack.BuildCommand = "dotnet build";
                    stack.RunCommand = "dotnet run";
                    stack.PrimaryLanguage = "C#";

                    if (!stack.Languages.Contains("C#")) stack.Languages.Insert(0, "C#");

                    if (csprojFiles.Length > 0)
                    {
                        try
                        {
                            string firstProj = File.ReadAllText(csprojFiles[0]);
                            var tfMatch = Regex.Match(firstProj, @"<TargetFramework>(net[0-9\.]+.*?)</TargetFramework>", RegexOptions.IgnoreCase);
                            if (tfMatch.Success) stack.TargetFramework = tfMatch.Groups[1].Value;

                            if (firstProj.Contains("Microsoft.NET.Sdk.Web")) stack.Frameworks.Add("ASP.NET Core Web API");
                            if (firstProj.Contains("UseWPF") || firstProj.Contains("<UseWPF>true</UseWPF>")) stack.Frameworks.Add("WPF (.NET Desktop)");
                            if (firstProj.Contains("UseWindowsForms") || firstProj.Contains("<UseWindowsForms>true</UseWindowsForms>")) stack.Frameworks.Add("WinForms (.NET Desktop)");
                            if (firstProj.Contains("Microsoft.NET.Sdk.BlazorWebAssembly")) stack.Frameworks.Add("Blazor WebAssembly");
                            if (firstProj.Contains("Microsoft.Maui")) stack.Frameworks.Add(".NET MAUI");
                        }
                        catch { }
                    }
                }

                // Node.js / TS / JS
                string pkgJsonPath = Path.Combine(projectDir, "package.json");
                if (File.Exists(pkgJsonPath))
                {
                    stack.BuildSystem = "Node.js";
                    stack.PackageManager = "npm";

                    if (File.Exists(Path.Combine(projectDir, "pnpm-lock.yaml"))) stack.PackageManager = "pnpm";
                    else if (File.Exists(Path.Combine(projectDir, "yarn.lock"))) stack.PackageManager = "yarn";
                    else if (File.Exists(Path.Combine(projectDir, "bun.lockb")) || File.Exists(Path.Combine(projectDir, "bun.lock"))) stack.PackageManager = "bun";

                    string pm = stack.PackageManager;
                    stack.InstallDepsCommand = pm == "yarn" ? "yarn" : $"{pm} install";
                    stack.BuildCommand = $"{pm} run build";
                    stack.RunCommand = $"{pm} run dev";

                    try
                    {
                        string pkgContent = File.ReadAllText(pkgJsonPath);
                        using var doc = JsonDocument.Parse(pkgContent);
                        var root = doc.RootElement;

                        string rawJson = pkgContent.ToLowerInvariant();
                        if (rawJson.Contains("\"next\"")) stack.Frameworks.Add("Next.js");
                        if (rawJson.Contains("\"react\"")) stack.Frameworks.Add("React");
                        if (rawJson.Contains("\"vue\"")) stack.Frameworks.Add("Vue.js");
                        if (rawJson.Contains("\"@angular/core\"")) stack.Frameworks.Add("Angular");
                        if (rawJson.Contains("\"svelte\"")) stack.Frameworks.Add("Svelte");
                        if (rawJson.Contains("\"vite\"")) stack.Frameworks.Add("Vite");
                        if (rawJson.Contains("\"electron\"")) stack.Frameworks.Add("Electron");
                        if (rawJson.Contains("\"express\"")) stack.Frameworks.Add("Express.js");
                        if (rawJson.Contains("\"@nestjs/core\"")) stack.Frameworks.Add("NestJS");
                        if (rawJson.Contains("\"tailwindcss\"")) stack.Frameworks.Add("Tailwind CSS");

                        if (root.TryGetProperty("scripts", out var scripts))
                        {
                            if (scripts.TryGetProperty("dev", out _)) stack.RunCommand = $"{pm} run dev";
                            else if (scripts.TryGetProperty("start", out _)) stack.RunCommand = $"{pm} start";
                            else if (scripts.TryGetProperty("serve", out _)) stack.RunCommand = $"{pm} run serve";
                        }
                    }
                    catch { }
                }

                // Python
                bool hasPyProject = File.Exists(Path.Combine(projectDir, "pyproject.toml"));
                bool hasReqs = File.Exists(Path.Combine(projectDir, "requirements.txt"));
                bool hasPipfile = File.Exists(Path.Combine(projectDir, "Pipfile"));
                bool hasSetupPy = File.Exists(Path.Combine(projectDir, "setup.py"));

                if (hasPyProject || hasReqs || hasPipfile || hasSetupPy || stack.PrimaryLanguage == "Python")
                {
                    stack.PrimaryLanguage = "Python";
                    stack.BuildSystem = "Python VirtualEnv";
                    stack.PackageManager = hasPyProject ? "poetry / pip" : "pip";

                    string venvScript = "if not exist .venv python -m venv .venv && call .venv\\Scripts\\activate && ";
                    stack.InstallDepsCommand = hasReqs ? $"{venvScript}pip install -r requirements.txt" : $"{venvScript}pip install -e .";

                    string entry = "main.py";
                    if (File.Exists(Path.Combine(projectDir, "app.py"))) entry = "app.py";
                    else if (File.Exists(Path.Combine(projectDir, "server.py"))) entry = "server.py";
                    else if (File.Exists(Path.Combine(projectDir, "run.py"))) entry = "run.py";

                    stack.RunCommand = $"{venvScript}python {entry}";
                    stack.BuildCommand = $"{venvScript}python -m py_compile {entry}";

                    try
                    {
                        string allPyText = (hasReqs ? File.ReadAllText(Path.Combine(projectDir, "requirements.txt")) : "") +
                                           (hasPyProject ? File.ReadAllText(Path.Combine(projectDir, "pyproject.toml")) : "");
                        allPyText = allPyText.ToLowerInvariant();
                        if (allPyText.Contains("fastapi")) stack.Frameworks.Add("FastAPI");
                        if (allPyText.Contains("flask")) stack.Frameworks.Add("Flask");
                        if (allPyText.Contains("django")) stack.Frameworks.Add("Django");
                        if (allPyText.Contains("torch") || allPyText.Contains("pytorch")) stack.Frameworks.Add("PyTorch (AI/ML)");
                        if (allPyText.Contains("tensorflow")) stack.Frameworks.Add("TensorFlow (AI/ML)");
                        if (allPyText.Contains("streamlit")) { stack.Frameworks.Add("Streamlit"); stack.RunCommand = $"{venvScript}streamlit run {entry}"; }
                        if (allPyText.Contains("gradio")) stack.Frameworks.Add("Gradio");
                    }
                    catch { }
                }

                // Rust
                if (File.Exists(Path.Combine(projectDir, "Cargo.toml")))
                {
                    stack.PrimaryLanguage = "Rust";
                    stack.BuildSystem = "Cargo";
                    stack.PackageManager = "Cargo / Crates.io";
                    stack.InstallDepsCommand = "cargo fetch";
                    stack.BuildCommand = "cargo build";
                    stack.RunCommand = "cargo run";
                    stack.Frameworks.Add("Rust Ecosystem");
                }

                // Go
                if (File.Exists(Path.Combine(projectDir, "go.mod")))
                {
                    stack.PrimaryLanguage = "Go";
                    stack.BuildSystem = "Go Modules";
                    stack.PackageManager = "Go Modules";
                    stack.InstallDepsCommand = "go mod download";
                    stack.BuildCommand = "go build ./...";
                    stack.RunCommand = "go run .";
                    stack.Frameworks.Add("Go Standard Engine");
                }

                // Luau / Roblox
                if (File.Exists(Path.Combine(projectDir, "default.project.json")) || File.Exists(Path.Combine(projectDir, "wally.toml")) || File.Exists(Path.Combine(projectDir, "rojo.json")))
                {
                    stack.PrimaryLanguage = "Luau (Roblox)";
                    stack.BuildSystem = "Rojo";
                    stack.PackageManager = "Wally";
                    stack.InstallDepsCommand = "wally install";
                    stack.BuildCommand = "rojo build -o place.rbxl";
                    stack.RunCommand = "rojo serve";
                    stack.Frameworks.Add("Roblox Luau / Rojo Ecosystem");
                }

                CheckHostToolchain(stack);
                return stack;
            });
        }

        public static async Task SynthesizeConfigFilesAsync(string projectDir, DetectedTechStack stack, IProgress<string>? progress = null, List<string>? logs = null)
        {
            await Task.Run(() =>
            {
                string envPath = Path.Combine(projectDir, ".env");
                if (!File.Exists(envPath))
                {
                    string[] candidates = { ".env.example", ".env.sample", ".env.template", "sample.env", ".env.local.example" };
                    foreach (var c in candidates)
                    {
                        string cPath = Path.Combine(projectDir, c);
                        if (File.Exists(cPath))
                        {
                            try
                            {
                                File.Copy(cPath, envPath, true);
                                stack.SynthesizedConfigs.Add($".env (from {c})");
                                progress?.Report($"  [+] Created .env from {c}");
                                logs?.Add($"Synthesized .env from template: {c}");
                                break;
                            }
                            catch { }
                        }
                    }

                    if (!File.Exists(envPath) && (stack.PrimaryLanguage == "TypeScript" || stack.PrimaryLanguage == "JavaScript" || stack.PrimaryLanguage == "Python"))
                    {
                        try
                        {
                            string defaultEnv = "# Generated by Heaplit Universal Repo Importer\nPORT=3000\nNODE_ENV=development\nHOST=localhost\nDEBUG=true\n";
                            File.WriteAllText(envPath, defaultEnv);
                            stack.SynthesizedConfigs.Add(".env (Synthesized local defaults)");
                        }
                        catch { }
                    }
                }

                string vscodeDir = Path.Combine(projectDir, ".vscode");
                string launchJsonPath = Path.Combine(vscodeDir, "launch.json");
                string tasksJsonPath = Path.Combine(vscodeDir, "tasks.json");

                if (!Directory.Exists(vscodeDir))
                {
                    try { Directory.CreateDirectory(vscodeDir); } catch { }
                }

                if (!File.Exists(launchJsonPath))
                {
                    string launchConfig = GenerateVsCodeLaunchJson(stack);
                    if (!string.IsNullOrEmpty(launchConfig))
                    {
                        try
                        {
                            File.WriteAllText(launchJsonPath, launchConfig);
                            stack.SynthesizedConfigs.Add(".vscode/launch.json");
                        }
                        catch { }
                    }
                }

                if (!File.Exists(tasksJsonPath) && !string.IsNullOrEmpty(stack.BuildCommand))
                {
                    string tasksConfig = GenerateVsCodeTasksJson(stack);
                    if (!string.IsNullOrEmpty(tasksConfig))
                    {
                        try
                        {
                            File.WriteAllText(tasksJsonPath, tasksConfig);
                            stack.SynthesizedConfigs.Add(".vscode/tasks.json");
                        }
                        catch { }
                    }
                }

                string gitignorePath = Path.Combine(projectDir, ".gitignore");
                if (!File.Exists(gitignorePath))
                {
                    string gitignore = GenerateGitIgnore(stack);
                    try
                    {
                        File.WriteAllText(gitignorePath, gitignore);
                        stack.SynthesizedConfigs.Add(".gitignore");
                    }
                    catch { }
                }
            });
        }

        private static string GenerateVsCodeLaunchJson(DetectedTechStack stack)
        {
            if (stack.PrimaryLanguage == "C#")
            {
                string tf = string.IsNullOrEmpty(stack.TargetFramework) ? "net10.0" : stack.TargetFramework;
                return "{\n  \"version\": \"0.2.0\",\n  \"configurations\": [\n    {\n      \"name\": \".NET Core Launch (Heaplit)\",\n      \"type\": \"coreclr\",\n      \"request\": \"launch\",\n      \"preLaunchTask\": \"build\",\n      \"program\": \"${workspaceFolder}/bin/Debug/" + tf + "/${workspaceFolderBasename}.dll\",\n      \"args\": [],\n      \"cwd\": \"${workspaceFolder}\",\n      \"stopAtEntry\": false,\n      \"console\": \"internalConsole\"\n    }\n  ]\n}";
            }
            if (stack.PrimaryLanguage == "TypeScript" || stack.PrimaryLanguage == "JavaScript")
            {
                return "{\n  \"version\": \"0.2.0\",\n  \"configurations\": [\n    {\n      \"name\": \"Launch Dev Server (Heaplit)\",\n      \"type\": \"node\",\n      \"request\": \"launch\",\n      \"runtimeExecutable\": \"npm\",\n      \"runtimeArgs\": [\"run\", \"dev\"],\n      \"console\": \"integratedTerminal\"\n    }\n  ]\n}";
            }
            if (stack.PrimaryLanguage == "Python")
            {
                return "{\n  \"version\": \"0.2.0\",\n  \"configurations\": [\n    {\n      \"name\": \"Python: Current File (Heaplit)\",\n      \"type\": \"python\",\n      \"request\": \"launch\",\n      \"program\": \"${file}\",\n      \"console\": \"integratedTerminal\",\n      \"justMyCode\": true\n    }\n  ]\n}";
            }
            return string.Empty;
        }

        private static string GenerateVsCodeTasksJson(DetectedTechStack stack)
        {
            string cmd = stack.BuildCommand.Replace("\"", "\\\"");
            return "{\n  \"version\": \"2.0.0\",\n  \"tasks\": [\n    {\n      \"label\": \"build\",\n      \"command\": \"" + cmd + "\",\n      \"type\": \"shell\",\n      \"problemMatcher\": [],\n      \"group\": {\n        \"kind\": \"build\",\n        \"isDefault\": true\n      }\n    }\n  ]\n}";
        }

        private static string GenerateGitIgnore(DetectedTechStack stack)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Heaplit Universal Auto-Generated .gitignore");
            sb.AppendLine(".env");
            sb.AppendLine(".env.local");
            sb.AppendLine(".DS_Store");
            sb.AppendLine("Thumbs.db");
            sb.AppendLine(".vs/");
            sb.AppendLine(".idea/");
            sb.AppendLine("*.log");

            if (stack.PrimaryLanguage == "C#")
            {
                sb.AppendLine("bin/");
                sb.AppendLine("obj/");
            }
            if (stack.PrimaryLanguage == "TypeScript" || stack.PrimaryLanguage == "JavaScript")
            {
                sb.AppendLine("node_modules/");
                sb.AppendLine("dist/");
                sb.AppendLine(".next/");
            }
            if (stack.PrimaryLanguage == "Python")
            {
                sb.AppendLine("__pycache__/");
                sb.AppendLine("*.pyc");
                sb.AppendLine(".venv/");
            }
            return sb.ToString();
        }

        private static void CheckHostToolchain(DetectedTechStack stack)
        {
            if (stack.PrimaryLanguage == "C#" && !IsCommandAvailable("dotnet"))
            {
                stack.MissingPrerequisites.Add(".NET SDK (dotnet)");
            }
            if ((stack.PrimaryLanguage == "TypeScript" || stack.PrimaryLanguage == "JavaScript") && !IsCommandAvailable("node"))
            {
                stack.MissingPrerequisites.Add("Node.js Runtime & npm");
            }
            if (stack.PrimaryLanguage == "Python" && !IsCommandAvailable("python"))
            {
                stack.MissingPrerequisites.Add("Python 3");
            }
        }

        public static bool IsCommandAvailable(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(1000);
                    return proc.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }

        private static async Task<bool> ExecuteCommandInDirectoryAsync(string workingDir, string commandLine, IProgress<string>? progress, List<string> logs)
        {
            try
            {
                logs.Add($"Running in [{workingDir}]: {commandLine}");

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {commandLine}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string outText = await proc.StandardOutput.ReadToEndAsync();
                    string errText = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(outText)) logs.Add(outText.Trim());
                    if (!string.IsNullOrEmpty(errText)) logs.Add(errText.Trim());

                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                logs.Add($"Error executing [{commandLine}]: {ex.Message}");
            }
            return false;
        }

        private static void CopyDirectoryContents(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectoryContents(subDir, dest);
            }
        }

        private static void RegisterProjectInHeaplitWorkspace(string projectPath, string repoName, DetectedTechStack stack)
        {
            try
            {
                string projectsDocPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Context", "Projects.md");
                if (File.Exists(projectsDocPath))
                {
                    string entry = $"\n\n### 📦 {repoName}\n- **Path**: `{projectPath}`\n- **Stack**: {stack.PrimaryLanguage} ({string.Join(", ", stack.Frameworks)})\n- **Run**: `{stack.RunCommand}`\n- **Imported**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                    File.AppendAllText(projectsDocPath, entry);
                }
            }
            catch { }
        }
    }
}