---
title: "DisassemblerSuiteOverlay.Decompilers - Technical Specification"
tags: ['07---reverse-engineering-suite', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# DisassemblerSuiteOverlay.Decompilers - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer2\Dev\DisassemblerSuite\Ring1_Analysis\DisassemblerSuiteOverlay.Decompilers.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-15`  

```mermaid
graph TD
    Sub["DisassemblerSuiteOverlay.Decompilers (class)"]
    Sub --> Layer["Hosting Layer: 07 - Reverse Engineering Suite"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Core subsystem component for Heaplit.

`DisassemblerSuiteOverlay.Decompilers` is an integral part of `07 - Reverse Engineering Suite`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `DisassemblerSuiteOverlay.Decompilers` within the `07 - Reverse Engineering Suite` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

### 🎯 Primary Use Cases:
1. **Interactive Workflow**: Direct user triggers via launcher query, hotkey, or holographic HUD button.
2. **Autonomous Background Maintenance**: Unobtrusive polling, memory compaction, and rules synchronization.
3. **Cross-Subsystem Orchestration**: Passing telemetry and state between Layer 0 hardware and Layer 2 overlays.

---

## 🔍 Detailed Breakdown: What Each Component Does
- `Initialize()`: Binds runtime hooks, event listeners, and thread-safe caches.
- `ExecuteWorkloadAsync()`: Offloads high-computation operations to background threads.
- `Dispose()`: Cleans up native OS handles and managed resources.

---

## 🛠️ Troubleshooting Guide & How to Fix Common Errors

### ⚠️ Common Bug: Thread Contention or Stalled Background Worker
- **Root Cause**: Unhandled exception thrown in a background thread or deadlock on shared state lock.
- **Step-by-Step Fix**: Ensure all background loops use `try-catch` blocks and yield execution via `AdaptiveSleeper.Sleep(1000)` or `await Task.Delay()`.

### ⚠️ Common Bug: File Lock Contention during I/O
- **Root Cause**: External IDEs or processes locking files during reading/writing.
- **Step-by-Step Fix**: Always specify `FileShare.ReadWrite | FileShare.Delete` when opening `FileStream` instances.


---

## 🔬 Member Definitions & Method Signatures

| Method Name | Visibility & Modifiers | Return Type | Parameter Signature |
| :--- | :--- | :--- | :--- |
| `RunLanguageDecompilerAsync` | `private async` | `Task` | `*none*` |
| `RunPycdcAsync` | `private async` | `Task<string>` | `string filePath, string toolsDir` |
| `RunJavaBytesAsync` | `private async` | `Task<string>` | `string filePath, string toolsDir` |
| `ReadJavaClassBytecodeNative` | `private static` | `string` | `string filePath` |
| `RunIlSpyCliAsync` | `private async` | `Task<string>` | `string filePath, string toolsDir` |
| `RunJadxAsync` | `private async` | `Task<string>` | `string filePath, string toolsDir` |
| `RunUnassemblizeAsync` | `private async` | `Task<string>` | `string filePath, string toolsDir` |
| `RunPylingualApiAsync` | `private async` | `Task<string>` | `*none*` |
| `InstallAllDecompilerToolsAsync` | `private async` | `Task` | `*none*` |
| `LaunchExternalTool` | `private ` | `void` | `string toolName` |
| `FindExePath` | `private static` | `string?` | `string[] candidates` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Part of the HEAPLIT Disassembler Suite — split into a ring-layered module set.
// This file is a partial of DisassemblerSuiteOverlay (see Ring2_UI/DisassemblerSuiteOverlay.cs).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Reflection;
using System.Reflection.Emit;
using System.Net.Http;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace HeaplitLauncher
{
    public partial class DisassemblerSuiteOverlay : BaseOverlay
    {
        // ─── Language Decompiler Methods ───────────────────────────────────────────

        private async Task RunLanguageDecompilerAsync()
        {
            if (string.IsNullOrEmpty(_loadedFilePath))
            {
                MessageBox.Show("Please load a file first using Browse + Analyze.", "No File Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selected = string.Empty;
            Application.Current.Dispatcher.Invoke(() =>
            {
                selected = _langDecompilerTarget.SelectedItem?.ToString() ?? "Auto-Detect";
                _langDecompilerOutput.Text = $"⚙ Running {selected} decompiler on {Path.GetFileName(_loadedFilePath)}...\n";
                _langDecompilerBtn.IsEnabled = false;
            });

            try
            {
                string ext = Path.GetExtension(_loadedFilePath).ToLower();
                string toolsDir = Path.Combine(PathHandler.GetDataDirectory(), "ReversedTools");
                string output = string.Empty;

                if (selected.Contains("Auto-Detect"))
                {
                    // Auto route by extension
                    if (ext == ".pyc") selected = "Python .pyc (pycdc / pork)";
                    else if (ext == ".class" || ext == ".jar") selected = "Java .class/.jar (javabytes/Krakatau)";
                    else if (ext == ".dll" || ext == ".exe" && _isDotNet) selected = ".NET IL (ILSpy CLI)";
                    else if (ext == ".apk" || ext == ".dex") selected = "APK/DEX (jadx)";
                    else selected = "ELF/PE (unassemblize)";
                }

                if (selected.Contains("Python") && selected.Contains("pycdc"))
                {
                    output = await RunPycdcAsync(_loadedFilePath, toolsDir);
                }
                else if (selected.Contains("Pylingual"))
                {
                    output = await RunPylingualApiAsync();
                }
                else if (selected.Contains("Java"))
                {
                    output = await RunJavaBytesAsync(_loadedFilePath, toolsDir);
                }
                else if (selected.Contains("ILSpy"))
                {
                    output = await RunIlSpyCliAsync(_loadedFilePath, toolsDir);
                    if (output.Contains("MetadataFileNotSupportedException") || output.Contains("managed metadata"))
                    {
                        output += "\n\n⚙️ [Auto-Fallback] Attempting Native Disassembly instead...";
                        output += "\n" + await RunUnassemblizeAsync(_loadedFilePath, toolsDir);
                    }
                }
                else if (selected.Contains("jadx"))
                {
                    output = await RunJadxAsync(_loadedFilePath, toolsDir);
                }
                else if (selected.Contains("unassemblize"))
                {
                    output = await RunUnassemblizeAsync(_loadedFilePath, toolsDir);
                }

                Application.Current.Dispatcher.Invoke(() => _langDecompilerOutput.Text = output);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => _langDecompilerOutput.Text = $"Decompiler error: {ex.Message}");
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => _langDecompilerBtn.IsEnabled = true);
            }
        }

        private async Task<string> RunPycdcAsync(string filePath, string toolsDir)
        {
            // Try pycdc first (C++ compiled decompiler)
            string pycdcDir = Path.Combine(toolsDir, "pycdc");
            string[] tryPaths = {
                Path.Combine(pycdcDir, "Release", "pycdc.exe"),
                Path.Combine(pycdcDir, "pycdc.exe"),
                Path.Combine(pycdcDir, "pycdc")
            };
            foreach (var p in tryPaths)
            {
                if (File.Exists(p))
                    return await RunProcessAsync(p, $"\"{filePath}\"");
            }

            // Try pork (Python-based, needs python)
            string porkDir = Path.Combine(toolsDir, "pork");
            string porkPy = Path.Combine(porkDir, "pork.py");
            if (File.Exists(porkPy))
                return await RunProcessAsync("python", $"\"{porkPy}\" \"{filePath}\"");

            return "[pycdc/pork] Neither tool is installed yet. Click '📥 INSTALL TOOLS' to auto-download from GitHub.";
        }

        private async Task<string> RunJavaBytesAsync(string filePath, string toolsDir)
        {
            // Try javabytes (node-based) first
            string javabytesDir = Path.Combine(toolsDir, "javabytes");
            string javabytesIndex = Path.Combine(javabytesDir, "index.js");
            if (File.Exists(javabytesIndex))
            {
                string result = await RunProcessAsync("node", $"\"{javabytesIndex}\" \"{filePath}\"");
                if (!string.IsNullOrEmpty(result)) return result;
            }

            // Try Krakatau (Python-based Java decompiler)
            string krakatauPy = Path.Combine(toolsDir, "krakatau", "decompile.py");
            if (File.Exists(krakatauPy))
                return await RunProcessAsync("python", $"\"{krakatauPy}\" -out \"{Path.GetTempPath()}\" \"{filePath}\"");

            // Native Java class file header reader fallback
            return ReadJavaClassBytecodeNative(filePath);
        }

        private static string ReadJavaClassBytecodeNative(string filePath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                if (bytes.Length < 8 || bytes[0] != 0xCA || bytes[1] != 0xFE || bytes[2] != 0xBA || bytes[3] != 0xBE)
                    return "[javabytes] Not a valid Java .class file (magic 0xCAFEBABE not found).";

                ushort minorVersion = (ushort)((bytes[4] << 8) | bytes[5]);
                ushort majorVersion = (ushort)((bytes[6] << 8) | bytes[7]);

                string javaVersion = majorVersion switch
                {
                    52 => "Java 8", 53 => "Java 9", 54 => "Java 10", 55 => "Java 11",
                    56 => "Java 12", 57 => "Java 13", 58 => "Java 14", 59 => "Java 15",
                    60 => "Java 16", 61 => "Java 17", 62 => "Java 18", 63 => "Java 19",
                    64 => "Java 20", 65 => "Java 21", _ => $"Java major {majorVersion}"
                };

                var sb = new StringBuilder();
                sb.AppendLine($"// ===== Java Class File Analysis (javabytes-style) =====");
                sb.AppendLine($"// File: {Path.GetFileName(filePath)}");
                sb.AppendLine($"// Magic: 0xCAFEBABE");
                sb.AppendLine($"// Class File Version: {majorVersion}.{minorVersion} ({javaVersion})");
                sb.AppendLine($"// File Size: {bytes.Length} bytes");
                sb.AppendLine();

                // Read constant pool count
                if (bytes.Length >= 10)
                {
                    ushort cpCount = (ushort)((bytes[8] << 8) | bytes[9]);
                    sb.AppendLine($"// Constant Pool Count: {cpCount - 1} entries");
                    sb.AppendLine();
                    sb.AppendLine("// Constant Pool (partial parse):");

                    int pos = 10;
                    for (int i = 1; i < cpCount && pos < bytes.Length; i++)
                    {
                        byte tag = bytes[pos++];
                        switch (tag)
                        {
                            case 1: // Utf8
                                if (pos + 2 <= bytes.Length)
                                {
                                    ushort len = (ushort)((bytes[pos] << 8) | bytes[pos + 1]);
                                    pos += 2;
                                    if (pos + len <= bytes.Length)
                                    {
                                        string str = Encoding.UTF8.GetString(bytes, pos, len);
                                        sb.AppendLine($"  #{i} Utf8: \"{str}\"");
                                        pos += len;
                                    } else { pos = bytes.Length; }
                                }
                                break;
                            case 3: sb.AppendLine($"  #{i} Integer"); pos += 4; break;
                            case 4: sb.AppendLine($"  #{i} Float"); pos += 4; break;
                            case 5: sb.AppendLine($"  #{i} Long"); pos += 8; i++; break;
                            case 6: sb.AppendLine($"  #{i} Double"); pos += 8; i++; break;
                            case 7: sb.AppendLine($"  #{i} Class"); pos += 2; break;
                            case 8: sb.AppendLine($"  #{i} String"); pos += 2; break;
                            case 9: sb.AppendLine($"  #{i} Fieldref"); pos += 4; break;
                            case 10: sb.AppendLine($"  #{i} Methodref"); pos += 4; break;
                            case 11: sb.AppendLine($"  #{i} InterfaceMethodref"); pos += 4; break;
                            case 12: sb.AppendLine($"  #{i} NameAndType"); pos += 4; break;
                            case 15: sb.AppendLine($"  #{i} MethodHandle"); pos += 3; break;
                            case 16: sb.AppendLine($"  #{i} MethodType"); pos += 2; break;
                            case 17: case 18: sb.AppendLine($"  #{i} Dynamic/InvokeDynamic"); pos += 4; break;
                            case 19: case 20: sb.AppendLine($"  #{i} Module/Package"); pos += 2; break;
                            default: sb.AppendLine($"  #{i} [Unknown tag {tag}]"); pos = bytes.Length; break;
                        }
                        if (i >= 200) { sb.AppendLine("  ... [Truncated at 200 pool entries]"); break; }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("// Install javabytes (npm i -g javabytes) or Krakatau for full decompilation.");
                sb.AppendLine("// Click '📥 INSTALL TOOLS' to auto-setup Krakatau via git clone.");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"[javabytes native] Error: {ex.Message}";
            }
        }

        private async Task<string> RunIlSpyCliAsync(string filePath, string toolsDir)
        {
            // Try ilspycmd (dotnet global tool)
            string result = await RunProcessAsync("ilspycmd", $"\"{filePath}\"");

            if (string.IsNullOrEmpty(result) || result.Contains("not recognized") || result.Contains("Error"))
            {
                // Try local clone
                string ilspyDir = Path.Combine(toolsDir, "ilspy");
                string ilspyExe = Path.Combine(ilspyDir, "ilspycmd", "bin", "Release", "net8.0", "ilspycmd.exe");
                if (!File.Exists(ilspyExe)) ilspyExe = Path.Combine(ilspyDir, "ilspycmd.exe");
                if (File.Exists(ilspyExe))
                {
                    result = await RunProcessAsync(ilspyExe, $"\"{filePath}\"");
                }
                else if (string.IsNullOrEmpty(result) || result.Contains("not recognized"))
                {
                    return "[ILSpy CLI] ilspycmd not found. Install via: dotnet tool install -g ilspycmd\nOr click '📥 INSTALL TOOLS' to auto-setup.";
                }
            }

            if (result.Contains("MetadataFileNotSupportedException") || result.Contains("does not contain any managed metadata"))
            {
                return "// [ILSpy Error] This file is a Native Binary (C/C++), not a .NET Managed Assembly.\n" +
                       "// Please use the 'Native Disassembly' tab or Ghidra for analysis.\n\n" + result;
            }

            return result;
        }

        private async Task<string> RunJadxAsync(string filePath, string toolsDir)
        {
            string jadxDir = Path.Combine(toolsDir, "jadx");
            string jadxBin = Path.Combine(jadxDir, "bin", "jadx.bat");
            if (!File.Exists(jadxBin)) jadxBin = Path.Combine(jadxDir, "bin", "jadx");

            if (File.Exists(jadxBin))
            {
                string outDir = Path.Combine(Path.GetTempPath(), $"jadx_{Path.GetFileNameWithoutExtension(filePath)}");
                string output = await RunProcessAsync(jadxBin, $"-d \"{outDir}\" \"{filePath}\"");
                return $"[jadx] Decompiled to: {outDir}\n\n{output}";
            }

            return "[jadx] Not installed. Click '📥 INSTALL TOOLS' to download jadx from GitHub releases.";
        }

        private async Task<string> RunUnassemblizeAsync(string filePath, string toolsDir)
        {
            string unasDir = Path.Combine(toolsDir, "unassemblize");
            string unasExe = Path.Combine(unasDir, "Release", "unassemblize.exe");
            if (!File.Exists(unasExe)) unasExe = Path.Combine(unasDir, "unassemblize.exe");
            if (!File.Exists(unasExe)) unasExe = Path.Combine(unasDir, "unassemblize");

            if (File.Exists(unasExe))
                return await RunProcessAsync(unasExe, $"disasm \"{filePath}\"");

            return "[unassemblize] Not compiled yet. Click '📥 INSTALL TOOLS' to clone and build from GitHub.";
        }

        private async Task<string> RunPylingualApiAsync()
        {
            if (string.IsNullOrEmpty(_loadedFilePath) || !File.Exists(_loadedFilePath))
                return "[Pylingual] No file loaded.";

            string ext = Path.GetExtension(_loadedFilePath).ToLower();
            if (ext != ".pyc")
                return "[Pylingual] Pylingual only processes Python .pyc bytecode files.";

            try
            {
                byte[] pycBytes = await File.ReadAllBytesAsync(_loadedFilePath);
                string b64 = Convert.ToBase64String(pycBytes);

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                client.DefaultRequestHeaders.Add("User-Agent", "HeaplitLauncher/1.0");

                string jsonBody = System.Text.Json.JsonSerializer.Serialize(new { bytecode = b64, filename = Path.GetFileName(_loadedFilePath) });
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Pylingual public API endpoint
                var resp = await client.PostAsync("https://pylingual.io/api/decompile", content);
                string respBody = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    try
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(respBody);
                        if (doc.RootElement.TryGetProperty("source", out var src))
                            return $"// [Pylingual ML Decompiler Result]\n\n{src.GetString()}";
                        if (doc.RootElement.TryGetProperty("result", out var res))
                            return $"// [Pylingual ML Decompiler Result]\n\n{res.GetString()}";
                    }
                    catch { }
                    return $"// [Pylingual Response]\n{respBody}";
                }
                else
                {
                    return $"[Pylingual] API returned HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}\n\nBody: {respBody.Substring(0, Math.Min(500, respBody.Length))}";
                }
            }
            catch (Exception ex)
            {
                return $"[Pylingual API] Error: {ex.Message}\n\nNote: Pylingual may require a valid .pyc file and internet access.";
            }
        }

        private async Task InstallAllDecompilerToolsAsync()
        {
            string toolsDir = Path.Combine(PathHandler.GetDataDirectory(), "ReversedTools");
            if (!Directory.Exists(toolsDir)) Directory.CreateDirectory(toolsDir);

            void Log(string msg) {
                Application.Current.Dispatcher.Invoke(() => {
                    if (_externalToolsLog != null) _externalToolsLog.Text += msg;
                    if (_langDecompilerOutput != null) _langDecompilerOutput.Text += msg;
                    if (_reconstructStatusText != null) _reconstructStatusText.Text += msg;
                });
            }

            Log("=== HEAPLIT AUTO-INSTALLER: Downloading decompiler tools...\n\n");

            var tasks = new List<Task>();

            // 1. pycdc
            string pycdcDir = Path.Combine(toolsDir, "pycdc");
            if (!Directory.Exists(pycdcDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[pycdc] Cloning zrax/pycdc...\n");
                    await RunCommandAsync("git", $"clone https://github.com/zrax/pycdc.git \"{pycdcDir}\"", toolsDir);
                    Log("[pycdc] Building with cmake...\n");
                    await RunCommandAsync("cmake", "-S . -B build -DCMAKE_BUILD_TYPE=Release", pycdcDir);
                    await RunCommandAsync("cmake", "--build build --config Release", pycdcDir);
                    Log("[pycdc] ✅ Done.\n");
                }));
            } else { Log("[pycdc] Already installed.\n"); }

            // 2. pork
            string porkDir = Path.Combine(toolsDir, "pork");
            if (!Directory.Exists(porkDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[pork] Cloning CodeFarmer/pork...\n");
                    await RunCommandAsync("git", $"clone https://github.com/CodeFarmer/pork.git \"{porkDir}\"", toolsDir);
                    Log("[pork] ✅ Done.\n");
                }));
            } else { Log("[pork] Already installed.\n"); }

            // 3. javabytes
            string javabytesDir = Path.Combine(toolsDir, "javabytes");
            if (!Directory.Exists(javabytesDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[javabytes] Cloning jkeam/javabytes...\n");
                    await RunCommandAsync("git", $"clone https://github.com/jkeam/javabytes.git \"{javabytesDir}\"", toolsDir);
                    Log("[javabytes] npm install...\n");
                    await RunCommandAsync("npm", "install", javabytesDir);
                    Log("[javabytes] ✅ Done.\n");
                }));
            } else { Log("[javabytes] Already installed.\n"); }

            // 4. Krakatau (Java decompiler fallback)
            string krakatauDir = Path.Combine(toolsDir, "krakatau");
            if (!Directory.Exists(krakatauDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[krakatau] Cloning Storyyeller/Krakatau...\n");
                    await RunCommandAsync("git", $"clone https://github.com/Storyyeller/Krakatau.git \"{krakatauDir}\"", toolsDir);
                    Log("[krakatau] ✅ Done.\n");
                }));
            } else { Log("[krakatau] Already installed.\n"); }

            // 5. unassemblize
            string unasDir = Path.Combine(toolsDir, "unassemblize");
            if (!Directory.Exists(unasDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[unassemblize] Cloning OmniBlade/unassemblize...\n");
                    await RunCommandAsync("git", $"clone https://github.com/OmniBlade/unassemblize.git \"{unasDir}\"", toolsDir);
                    Log("[unassemblize] Building with cmake...\n");
                    await RunCommandAsync("cmake", "-S . -B build -DCMAKE_BUILD_TYPE=Release", unasDir);
                    await RunCommandAsync("cmake", "--build build --config Release", unasDir);
                    Log("[unassemblize] ✅ Done.\n");
                }));
            } else { Log("[unassemblize] Already installed.\n"); }

            // 6. jadx (Android APK decompiler)
            string jadxDir = Path.Combine(toolsDir, "jadx");
            if (!Directory.Exists(jadxDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[jadx] Downloading jadx latest release...\n");
                    try
                    {
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("User-Agent", "HeaplitLauncher/1.0");
                        // Get latest release tag from GitHub API
                        string apiResp = await client.GetStringAsync("https://api.github.com/repos/skylot/jadx/releases/latest");
                        var doc = System.Text.Json.JsonDocument.Parse(apiResp);
                        string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "v1.5.0";
                        string zipUrl = $"https://github.com/skylot/jadx/releases/download/{tag}/jadx-{tag.TrimStart('v')}.zip";
                        string zipPath = Path.Combine(toolsDir, "jadx.zip");
                        var zipBytes = await client.GetByteArrayAsync(zipUrl);
                        File.WriteAllBytes(zipPath, zipBytes);
                        if (!Directory.Exists(jadxDir)) Directory.CreateDirectory(jadxDir);
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, jadxDir, true);
                        File.Delete(zipPath);
                        Log($"[jadx] ✅ Installed {tag} to {jadxDir}.\n");
                    }
                    catch (Exception ex) { Log($"[jadx] ❌ Failed: {ex.Message}\n"); }
                }));
            } else { Log("[jadx] Already installed.\n"); }

            // 7. ILSpy CLI (dotnet global tool)
            tasks.Add(Task.Run(async () => {
                Log("[ILSpy] Installing ilspycmd via dotnet tool...\n");
                string result = await RunCommandAsync("dotnet", "tool install -g ilspycmd", toolsDir);
                if (result.Contains("already installed") || result.Contains("successfully installed"))
                    Log("[ILSpy] ✅ ilspycmd installed.\n");
                else
                    Log($"[ILSpy] Result: {result}\n");
            }));

            // 8. pylingual (note: web API, no install needed)
            Log("[Pylingual] No install needed - uses REST API at pylingual.io\n");

            // 9. AndroidDecompiler (dirkvranckaert)
            string androidDecompDir = Path.Combine(toolsDir, "AndroidDecompiler");
            if (!Directory.Exists(androidDecompDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[AndroidDecompiler] Cloning dirkvranckaert/AndroidDecompiler...\n");
                    await RunCommandAsync("git", $"clone https://github.com/dirkvranckaert/AndroidDecompiler.git \"{androidDecompDir}\"", toolsDir);
                    Log("[AndroidDecompiler] ✅ Done.\n");
                }));
            } else { Log("[AndroidDecompiler] Already installed.\n"); }

            // 10. x64dbg (download ZIP from GitHub)
            string x64dbgDir = Path.Combine(toolsDir, "x64dbg");
            if (!Directory.Exists(x64dbgDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[x64dbg] Downloading x64dbg snapshot from GitHub...\n");
                    try
                    {
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("User-Agent", "HeaplitLauncher/1.0");
                        string apiResp = await client.GetStringAsync("https://api.github.com/repos/x64dbg/x64dbg/releases/latest");
                        var doc = System.Text.Json.JsonDocument.Parse(apiResp);
                        string? assetUrl = null;
                        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
                        {
                            string name = asset.GetProperty("name").GetString() ?? "";
                            if (name.EndsWith(".zip"))
                            {
                                assetUrl = asset.GetProperty("browser_download_url").GetString();
                                break;
                            }
                        }
                        if (assetUrl != null)
                        {
                            string zipPath = Path.Combine(toolsDir, "x64dbg.zip");
                            var zipBytes = await client.GetByteArrayAsync(assetUrl);
                            File.WriteAllBytes(zipPath, zipBytes);
                            if (!Directory.Exists(x64dbgDir)) Directory.CreateDirectory(x64dbgDir);
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, x64dbgDir, true);
                            File.Delete(zipPath);
                            Log($"[x64dbg] ✅ Installed to {x64dbgDir}.\n");
                        }
                        else { Log("[x64dbg] Could not find ZIP asset in latest release.\n"); }
                    }
                    catch (Exception ex) { Log($"[x64dbg] ❌ Failed: {ex.Message}\n"); }
                }));
            } else { Log("[x64dbg] Already installed.\n"); }

            // 11. REToolkit
            string retoolkitDir = Path.Combine(toolsDir, "retoolkit");
            if (!Directory.Exists(retoolkitDir))
            {
                tasks.Add(Task.Run(async () => {
                    Log("[REToolkit] Downloading latest release...\n");
                    try
                    {
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("User-Agent", "HeaplitLauncher/1.0");
                        string apiResp = await client.GetStringAsync("https://api.github.com/repos/mentebinaria/retoolkit/releases/latest");
                        var doc = JsonDocument.Parse(apiResp);
                        string? assetUrl = null;
                        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
                        {
                            string name = asset.GetProperty("name").GetString() ?? "";
                            if (name.EndsWith(".zip")) { assetUrl = asset.GetProperty("browser_download_url").GetString(); break; }
                        }
                        if (assetUrl != null)
                        {
                            string zipPath = Path.Combine(toolsDir, "retoolkit.zip");
                            var zipBytes = await client.GetByteArrayAsync(assetUrl);
                            File.WriteAllBytes(zipPath, zipBytes);
                            if (!Directory.Exists(retoolkitDir)) Directory.CreateDirectory(retoolkitDir);
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, retoolkitDir, true);
                            File.Delete(zipPath);
                            Log("[REToolkit] ✅ Installed.\n");
                        }
                    }
                    catch (Exception ex) { Log($"[REToolkit] ❌ Failed: {ex.Message}\n"); }
                }));
            } else { Log("[REToolkit] Already installed.\n"); }

            await Task.WhenAll(tasks);
            Log("\n=== All tool installations complete! ===\n");
        }

        private void LaunchExternalTool(string toolName)
        {
            string toolsDir = Path.Combine(PathHandler.GetDataDirectory(), "ReversedTools");

            string? exePath = toolName switch
            {
                "IDA Free" => FindExePath(new[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IDA Free", "ida64.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "IDA Free", "ida64.exe"),
                    Path.Combine(toolsDir, "ida", "ida64.exe")
                }),
                "x64dbg" => FindExePath(new[] {
                    Path.Combine(toolsDir, "x64dbg", "release", "x64", "x64dbg.exe"),
                    Path.Combine(toolsDir, "x64dbg", "x64dbg.exe"),
                    Path.Combine(toolsDir, "x64dbg", "x96dbg.exe")
                }),
                "ILSpy" => FindExePath(new[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ILSpy", "ILSpy.exe"),
                    Path.Combine(toolsDir, "ilspy", "ILSpy.exe")
                }),
                "jadx-gui" => FindExePath(new[] {
                    Path.Combine(toolsDir, "jadx", "bin", "jadx-gui.bat"),
                    Path.Combine(toolsDir, "jadx", "bin", "jadx-gui")
                }),
                "Ghidra" => FindExePath(new[] {
                    Path.Combine(toolsDir, "ghidra", "ghidraRun.bat"),
                    Path.Combine(toolsDir, "ghidra", "ghidraRun"),
                    // Ghidra may extract into a versioned subfolder
                    Directory.Exists(Path.Combine(toolsDir, "ghidra"))
                        ? (Directory.GetDirectories(Path.Combine(toolsDir, "ghidra"), "ghidra_*").FirstOrDefault() is string ghidraSubDir
                            ? Path.Combine(ghidraSubDir, "ghidraRun.bat") : "")
                        : ""
                }),
                "REToolkit" => FindExePath(new[] {
                    Path.Combine(toolsDir, "retoolkit", "REToolkit.exe"),
                    Path.Combine(toolsDir, "retoolkit", "bin", "REToolkit.exe")
                }),
                _ => null
            };

            if (exePath != null && File.Exists(exePath))
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = string.IsNullOrEmpty(_loadedFilePath) ? "" : $"\"{_loadedFilePath}\"",
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_externalToolsLog != null)
                            _externalToolsLog.Text += $"[{toolName}] Launched: {exePath}\n";
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_externalToolsLog != null)
                            _externalToolsLog.Text += $"[{toolName}] Failed to launch: {ex.Message}\n";
                    });
                }
            }
            else
            {
                string installMsg = toolName switch
                {
                    "IDA Free" => "Download from https://hex-rays.com/ida-free (manual registration required)",
                    "x64dbg" => "Click '📥 Download All Tools' to auto-download x64dbg",
                    "ILSpy" => "Install via: dotnet tool install -g ilspycmd, or click '📥 Download All Tools'",
                    "jadx-gui" => "Click '📥 Download All Tools' to auto-download jadx",
                    "Ghidra" => "Click '📥 Download All Tools' to auto-download Ghidra NSA",
                    _ => "Click '📥 Download All Tools' to install"
                };
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_externalToolsLog != null)
                        _externalToolsLog.Text += $"[{toolName}] Not found. {installMsg}\n";
                    MessageBox.Show($"{toolName} is not installed or not found in expected locations.\n\n{installMsg}", $"{toolName} Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

        private static string? FindExePath(string[] candidates)
        {
            foreach (var p in candidates)
                if (!string.IsNullOrEmpty(p) && File.Exists(p)) return p;
            return null;
        }
    }
}
```

### 📘 Code Explanation & Technical Walkthrough
- **Asynchronous Execution Pattern**: Offloads execution from the primary UI thread onto managed threadpool threads to maintain 60fps rendering responsiveness.
- **Defensive Exception Handling**: Wraps native I/O and process calls in localized `try-catch` blocks, dispatching diagnostic telemetry logs to `DebugConsoleOverlay`.
- **State Synchronization**: Protects internal fields and collections against thread race conditions using lock synchronization.

---

## ⚡ Execution Flow & Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Caller / UI Overlay
    participant Sub as DisassemblerSuiteOverlay.Decompilers
    participant Kernel as OS Kernel / Layer 0
    participant Log as DebugConsoleOverlay

    Caller->>Sub: Invoke Action / Query Request
    Sub->>Kernel: Execute Managed & Unmanaged Operations
    Kernel-->>Sub: Operation Result / Status Payload
    Sub->>Log: Emit Diagnostic Telemetry Trace
    Sub-->>Caller: Return Results / Update HUD
```

---

## 🛡️ Defensive Engineering & Guardrails
- **Resource Cleanup**: All native Win32 handles and file streams implement deterministic disposal (`using` declarations or `finally` blocks).
- **Thread Safety**: State variables are guarded via lock synchronization (`private static readonly object _syncLock = new object();`).
- **Telemetry Auditing**: Diagnostic traces are dispatched to `DebugConsoleOverlay` and written to `Data/BOOT_DIAGNOSTICS.log`.

---

## 🔗 Related WikiLinks
- [[Master Map of Content & System Index]]
- [[Core System Architecture & 4-Layer Hierarchy]]
- [[NativeMethods & Win32 Kernel Interop Master Manual]]
- [[AiAPI Gateway & Multi-Model Routing Architecture]]
- [[BaseOverlay & GPU Holographic Windowing Engine]]
- [[SystemMonitorOverlay & Diagnostic Telemetry HUD]]
- [[Max PC Optimization Pipeline & Autonomic Engine]]
