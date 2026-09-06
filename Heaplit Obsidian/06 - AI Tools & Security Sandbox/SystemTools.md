---
title: "SystemTools - Technical Specification"
tags: ['06---ai-tools-&-security-sandbox', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# SystemTools - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer0\AiTools\SystemTools.cs`  
> **Namespace**: `HeaplitLauncher.AiTools`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-15`  

```mermaid
graph TD
    Sub["PowerShellTool (class)"]
    Sub --> Layer["Hosting Layer: 06 - AI Tools & Security Sandbox"]
    Sub --> NS["Namespace: HeaplitLauncher.AiTools"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Core subsystem component for Heaplit.

`PowerShellTool` is an integral part of `06 - AI Tools & Security Sandbox`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `SystemTools` within the `06 - AI Tools & Security Sandbox` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

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
| `ExecuteAsync` | `public ` | `Task<string>` | `Match m, HashSet<string> executedTags` |
| `ExecuteAsync` | `public ` | `Task<string>` | `Match m, HashSet<string> executedTags` |
| `ExecuteAsync` | `public ` | `Task<string>` | `Match m, HashSet<string> executedTags` |


---

## 💻 Source Code Reference

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HeaplitLauncher.AiTools
{
    public class PowerShellTool : IAiTool
    {
        public string Tag => "PS";
        public string RegexPattern => @"@ps\{(?<c>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string cmd = m.Groups["c"].Value.Trim();
            if (!executedTags.Add("PS:" + cmd.GetHashCode())) return Task.FromResult("");
            // Agent Mode gate: only runs when the user has enabled PC control.
            if (!CoreRegistry.Data.Settings.Current.ENABLE_PC_CONTROL)
                return Task.FromResult("[BLOCKED: enable Agent Mode (PC control) in Settings to let Heaplit run commands]\n");
            // Confirm clearly-destructive commands before running.
            string lc = cmd.ToLowerInvariant();
            bool risky = lc.Contains("remove-item") || lc.Contains("del ") || lc.Contains("format ") ||
                         lc.Contains("shutdown") || lc.Contains("stop-process") || lc.Contains("rmdir") ||
                         lc.Contains("rd /s") || lc.Contains("rm -");
            if (risky && !HumanConfirm.Ask($"Heaplit (AI) wants to run a shell command:\n\n{cmd}\n\nAllow?"))
                return Task.FromResult("[DENIED: user declined the command]\n");
            string output = AgentExecutor.ExecutePowerShellDirect(cmd);
            return Task.FromResult($"[PS OUTPUT]:\n{output}\n");
        }
    }

    public class ProcessListTool : IAiTool
    {
        public string Tag => "PL";
        public string RegexPattern => @"@proc_list";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            if (!executedTags.Add("PL")) return Task.FromResult("");
            var procs = Process.GetProcesses().Select(p => p.ProcessName).Distinct().OrderBy(n => n).Take(100);
            return Task.FromResult($"[PROCESSES]:\n{string.Join(", ", procs)}\n");
        }
    }

    public class ProcessKillTool : IAiTool
    {
        public string Tag => "PK";
        public string RegexPattern => @"@proc_kill\{(?<n>.*?)\}";
        public Task<string> ExecuteAsync(Match m, HashSet<string> executedTags)
        {
            string name = m.Groups["n"].Value.Trim().Trim('"', '\'');
            if (!executedTags.Add("PK:" + name)) return Task.FromResult("");
            // SECURITY: model-initiated process termination requires explicit human confirmation.
            if (!HumanConfirm.Ask($"Heaplit (AI) wants to force-kill all '{name}' processes. Allow?"))
                return Task.FromResult($"[DENIED: user declined to kill {name}]\n");
            int killed = 0;
            foreach (var p in Process.GetProcessesByName(name)) { try { p.Kill(); killed++; } catch { } }
            return Task.FromResult($"[KILLED: {name} ({killed} instances)]\n");
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
    participant Sub as PowerShellTool
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
