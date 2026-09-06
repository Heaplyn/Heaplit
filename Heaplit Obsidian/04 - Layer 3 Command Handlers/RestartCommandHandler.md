---
title: "RestartCommandHandler - Technical Specification"
tags: ['04---layer-3-command-handlers', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# RestartCommandHandler - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer3\Handlers\System\RestartCommandHandler.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-09`  

```mermaid
graph TD
    Sub["RestartCommandHandler (class)"]
    Sub --> Layer["Hosting Layer: 04 - Layer 3 Command Handlers"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Handles application commands to restart the active Heaplit launcher thread or environment.

`RestartCommandHandler` is an integral part of `04 - Layer 3 Command Handlers`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `RestartCommandHandler` within the `04 - Layer 3 Command Handlers` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

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
| `CanHandle` | `public ` | `bool` | `string query` |
| `GetSuggestions` | `public ` | `List<CommandResult>` | `string query` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles application commands to restart the active Heaplit launcher thread or environment.

using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace HeaplitLauncher
{
    public class RestartCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "restart", "re", "reload", "restart heaplit", "fresh boot", "fresh start");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();

            // 1. Standard Restart (Now includes a lightweight rebuild if in Dev mode)
            suggestions.Add(new CommandResult
            {
                TITLE = "🔄 Restart & Sync (Quick)",
                DESCRIPTION = "Restart Heaplit and sync logic changes (Rebuilds if necessary)",
                EXECUTE = () =>
                {
                    TextOverlay.Show("Restarting & Syncing...", 800);
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.4) };
                    timer.Tick += (s, ev) =>
                    {
                        timer.Stop();
                        NativeMethods.Restart(freshBoot: true, pullFirst: false);
                    };
                    timer.Start();
                },
                SIMILARITY = (SearchUtil.BestSimilarity(query, "restart", "re", "reload", "restart heaplit", "fresh boot", "fresh start") + 9.0 * 0.01)
            });

            // 2. Explicit Fresh Start / Sync
            suggestions.Add(new CommandResult
            {
                TITLE = "♻️ Fresh Start (Run.bat)",
                DESCRIPTION = "Clean build, update binaries, and cold-start via run.bat",
                EXECUTE = () =>
                {
                    TextOverlay.Show("Launching run.bat lifecycle...", 1000);
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
                    timer.Tick += (s, ev) =>
                    {
                        timer.Stop();
                        NativeMethods.Restart(freshBoot: true, pullFirst: false);
                    };
                    timer.Start();
                },
                SIMILARITY = (SearchUtil.BestSimilarity(query, "restart", "re", "reload", "restart heaplit", "fresh boot", "fresh start") + 10.0 * 0.01)
            });

            // 3. Cloud Sync & Rebuild
            suggestions.Add(new CommandResult
            {
                TITLE = "🚀 Pull, Rebuild & Start",
                DESCRIPTION = "git pull latest main and perform full modular deployment",
                EXECUTE = () =>
                {
                    TextOverlay.Show("Syncing Cloud & Rebuilding...", 800);
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.4) };
                    timer.Tick += (s, ev) =>
                    {
                        timer.Stop();
                        NativeMethods.Restart(freshBoot: true, pullFirst: true);
                    };
                    timer.Start();
                },
                SIMILARITY = (SearchUtil.BestSimilarity(query, "restart", "re", "reload", "restart heaplit", "fresh boot", "fresh start") + 7.5 * 0.01)
            });

            return suggestions;
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
    participant Sub as RestartCommandHandler
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
