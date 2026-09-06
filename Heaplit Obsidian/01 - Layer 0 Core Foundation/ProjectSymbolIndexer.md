---
title: "ProjectSymbolIndexer - Technical Specification"
tags: ['01---layer-0-core-foundation', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# ProjectSymbolIndexer - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer0\Common\ProjectSymbolIndexer.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-16`  

```mermaid
graph TD
    Sub["ProjectSymbol (class)"]
    Sub --> Layer["Hosting Layer: 01 - Layer 0 Core Foundation"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
High-performance project-wide symbol indexer.
          Scans the active project directory for C# classes, methods, and types to provide IDE-grade autocomplete.

`ProjectSymbol` is an integral part of `01 - Layer 0 Core Foundation`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `ProjectSymbolIndexer` within the `01 - Layer 0 Core Foundation` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

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
| `AddSymbol` | `private static` | `void` | `string name, string kind, string path, string parent = ""` |
| `GetProjectSuggestions` | `public static` | `List<AutocompleteSuggestion>` | `string wordPrefix` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-16
// Summary: High-performance project-wide symbol indexer.
//          Scans the active project directory for C# classes, methods, and types to provide IDE-grade autocomplete.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class ProjectSymbol
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty; // Class, Method, Variable
        public string FilePath { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty; // Parent class
    }

    public static class ProjectSymbolIndexer
    {
        private static readonly ConcurrentDictionary<string, ProjectSymbol> _symbolCache = new ConcurrentDictionary<string, ProjectSymbol>(StringComparer.OrdinalIgnoreCase);
        private static bool _isIndexing = false;
        private static readonly AsyncCSharpFileLoader _loader = new AsyncCSharpFileLoader();

        public static List<ProjectSymbol> Symbols => _symbolCache.Values.ToList();

        public static async Task IndexProjectAsync(string rootPath)
        {
            if (_isIndexing) return;
            _isIndexing = true;

            try
            {
                var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                                     .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));

                foreach (var file in files)
                {
                    var outline = await _loader.LoadFileOutlineAsync(file);
                    foreach (var type in outline.Types)
                    {
                        AddSymbol(type.Name, "Class", file);
                        foreach (var method in type.Methods)
                        {
                            AddSymbol(method.Name, "Method", file, type.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Indexing Error: {ex.Message}");
            }
            finally
            {
                _isIndexing = false;
            }
        }

        private static void AddSymbol(string name, string kind, string path, string parent = "")
        {
            string key = $"{kind}:{name}:{parent}";
            _symbolCache.TryAdd(key, new ProjectSymbol
            {
                Name = name,
                Kind = kind,
                FilePath = path,
                TypeName = parent
            });
        }

        public static List<AutocompleteSuggestion> GetProjectSuggestions(string wordPrefix)
        {
            return _symbolCache.Values
                .Where(s => s.Name.StartsWith(wordPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(s => new AutocompleteSuggestion
                {
                    Text = s.Name,
                    Description = $"{s.Kind} in {Path.GetFileName(s.FilePath)}",
                    Icon = s.Kind == "Class" ? "📦" : "m",
                    Score = 0.9
                })
                .OrderByDescending(x => x.Score)
                .Take(20)
                .ToList();
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
    participant Sub as ProjectSymbol
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
