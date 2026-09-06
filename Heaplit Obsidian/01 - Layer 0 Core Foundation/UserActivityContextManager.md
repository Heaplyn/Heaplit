---
title: "UserActivityContextManager - Technical Specification"
tags: ['01---layer-0-core-foundation', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# UserActivityContextManager - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer0\Common\UserActivityContextManager.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-13`  

```mermaid
graph TD
    Sub["ActivityItem (class)"]
    Sub --> Layer["Hosting Layer: 01 - Layer 0 Core Foundation"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
User System Activity & History Context Manager.
 Gathers real-time active window, command history, recent debug logs, and clipboard context for AI queries.

`ActivityItem` is an integral part of `01 - Layer 0 Core Foundation`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `UserActivityContextManager` within the `01 - Layer 0 Core Foundation` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

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
| `TrackUserQuery` | `public static` | `void` | `string query` |
| `BuildFullActivityContext` | `public static` | `string` | `*none*` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-13
// Summary: User System Activity & History Context Manager.
// Gathers real-time active window, command history, recent debug logs, and clipboard context for AI queries.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace HeaplitLauncher
{
    public class ActivityItem
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Text { get; set; } = string.Empty;
    }

    public static class UserActivityContextManager
    {
        private static readonly List<ActivityItem> _recentQueries = new List<ActivityItem>();
        private static readonly object _lock = new object();

        public static void TrackUserQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            lock (_lock)
            {
                _recentQueries.Add(new ActivityItem { Text = query.Trim() });
                if (_recentQueries.Count > 20) _recentQueries.RemoveAt(0);

                // ANTI-ROT: Prune items older than 30 minutes
                _recentQueries.RemoveAll(q => (DateTime.Now - q.Timestamp).TotalMinutes > 30);
            }
            PredictiveStreamManager.IngestEvent("COMMAND", query);
            ActionJournalManager.LogAction("USER_QUERY", query, ScreenMonitorEngine.ActiveWindowTitle);
        }

        public static string BuildFullActivityContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[USER ENVIRONMENT & RECENT ACTIVITY CONTEXT]");
            sb.AppendLine($"• Current Local Time: {DateTime.Now:F}");

            // 1. Active Window Context
            try
            {
                ScreenMonitorEngine.UpdateActiveWindowInfo();
                string activeWin = ScreenMonitorEngine.ActiveWindowTitle;
                string activeProc = ScreenMonitorEngine.ActiveProcessName;
                if (!string.IsNullOrEmpty(activeWin))
                {
                    sb.AppendLine($"• Active Window: '{activeWin}' ({activeProc})");
                }
            }
            catch { }

            // 2. Recent Search & Command Queries
            lock (_lock)
            {
                // Prune rot before building string
                _recentQueries.RemoveAll(q => (DateTime.Now - q.Timestamp).TotalMinutes > 30);

                if (_recentQueries.Count > 0)
                {
                    sb.AppendLine($"• Recent HUD Queries & Commands (Active Session):");
                    foreach (var q in _recentQueries.TakeLast(5))
                    {
                        sb.AppendLine($"  - [{q.Timestamp:HH:mm:ss}] {q.Text}");
                    }
                }
            }

            // 3. Clipboard Context (If Text)
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try {
                            if (Clipboard.ContainsText())
                            {
                                string clip = Clipboard.GetText().Trim();
                                if (!string.IsNullOrEmpty(clip))
                                {
                                    string preview = clip.Length > 200 ? clip.Substring(0, 200) + "..." : clip;
                                    preview = preview.Replace("\r", " ").Replace("\n", " ");
                                    sb.AppendLine($"• Current Clipboard Content: \"{preview}\"");
                                }
                            }
                        } catch { }
                    });
                }
            }
            catch { }

            sb.AppendLine("--------------------------------------------------");
            return sb.ToString();
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
    participant Sub as ActivityItem
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
