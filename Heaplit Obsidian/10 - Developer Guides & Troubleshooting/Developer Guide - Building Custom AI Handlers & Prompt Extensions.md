---
title: "🤖 Developer Guide - Building Custom AI Handlers & Prompt Extensions"
tags: ['developer-guide', 'ai-cognition', 'llm-gateway', 'prompt-extensions', 'custom-handlers', 'plugins']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Developer Master Guide (10+ Pages)"
status: VERIFIED_COMPLETE
---

# 🤖 Developer Guide - Building Custom AI Handlers & Prompt Extensions

## 📌 Document Overview & Summary
Comprehensive guide for developers building custom AI handlers, extending system prompt templates, registering novel AI capabilities, and parsing structured JSON responses.


## Executive Summary

The Heaplit AI Cognition engine permits developers to register custom intent handlers, dynamically injected system prompts, and custom structured tool calls. This guide demonstrates how to author a custom AI handler from scratch, register it with the `AIGatewayProvider`, and handle JSON stream parsing.

## Architecture & Integration Pipeline

```mermaid
sequenceDiagram
    participant User as User / Voice Command
    participant Hub as CommandProcessingHub
    participant AIBridge as AIGatewayProvider
    participant Handler as CustomAIHandler
    participant LLM as External LLM API (Gemini/OpenAI)

    User->>Hub: Dispatch request ("Analyze active system processes")
    Hub->>AIBridge: RouteToProviderAsync(prompt, context)
    AIBridge->>Handler: ProcessIntentAsync(payload)
    Handler->>LLM: Send JSON schema payload
    LLM-->>Handler: Return streaming response tokens
    Handler-->>Hub: Return structured execution plan
```

## Custom AI Handler Implementation Example

```csharp
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Heaplit.Core.AI.Handlers
{
    public interface ICustomAIHandler
    {
        string IntentName { get; }
        Task<AIHandlerResult> ExecuteAsync(string userPrompt, JsonElement contextData, CancellationToken cancellationToken);
    }

    public sealed class SystemOptimizationAIHandler : ICustomAIHandler
    {
        public string IntentName => "SystemOptimizationIntent";

        public async Task<AIHandlerResult> ExecuteAsync(string userPrompt, JsonElement contextData, CancellationToken cancellationToken)
        {
            // Build dynamic system prompt extension
            string systemPrompt = @"You are Heaplit System Optimization Subagent.
Analyze system metrics and output JSON with mandatory fields:
{
  ""action"": ""trim_memory"" | ""kill_process"" | ""none"",
  ""targetProcess"": ""string"",
  ""rationale"": ""string""
}";

            var requestBody = new
            {
                contents = userPrompt,
                systemInstruction = systemPrompt,
                temperature = 0.2
            };

            // Simulating AI payload dispatch and structured response parsing
            await Task.Delay(100, cancellationToken); // Network stream simulation

            string mockJsonResponse = "{"action": "trim_memory", "targetProcess": "all", "rationale": "Working set exceeded 150MB target threshold."}";
            
            using var doc = JsonDocument.Parse(mockJsonResponse);
            var root = doc.RootElement;

            return new AIHandlerResult
            {
                Success = true,
                ActionToTake = root.GetProperty("action").GetString(),
                TargetProcess = root.GetProperty("targetProcess").GetString(),
                Rationale = root.GetProperty("rationale").GetString()
            };
        }
    }

    public sealed class AIHandlerResult
    {
        public bool Success { get; set; }
        public string ActionToTake { get; set; } = string.Empty;
        public string TargetProcess { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
    }
}
```

### 📘 Code Explanation & Technical Walkthrough

- **`IntentName` Registry Key**: Uniquely identifies the custom handler within the central `CommandProcessingHub` dictionary, allowing dynamic intent routing based on natural language classification.
- **Low Temperature (`temperature = 0.2`)**: Setting temperature to 0.2 forces deterministic, reproducible LLM outputs when asking the AI to emit executable system command JSON.
- **`JsonDocument.Parse` Zero-Reflection Parsing**: Uses `JsonDocument` for fast, lightweight parsing of raw LLM JSON strings without incurring reflection overhead from traditional serializers.

## Registering Custom Handlers in Container

```csharp
public static void RegisterCustomAIModules(IServiceCollection services)
{
    services.AddSingleton<ICustomAIHandler, SystemOptimizationAIHandler>();
}
```

### 📘 Code Explanation & Technical Walkthrough

- **Dependency Injection Binding**: Registering `ICustomAIHandler` bindings as singletons ensures that `AIGatewayProvider` auto-discovers all registered handlers on engine boot using `IEnumerable<ICustomAIHandler>` constructor injection.


---

## 🔗 System Interconnections & WikiLinks
- [[Master Map of Content & System Index]]
- [[Welcome]]
- [[Developer Onboarding, Extension & Custom Module Guide]]
- [[Complete Troubleshooting & System Crash Recovery Manual]]
- [[Developer Guide - Roblox Ring Wrapper Dependency Hierarchy Invariants]]
- [[Developer Guide - PInvoke & Native Win32 Interop Standards]]


---

## 🚀 Advanced Developer Operating Manual & Low-Level Subsystem Mechanics

### 1. Low-Level Threading & Memory Architecture
When maintaining or extending this module within Heaplit, developers must enforce strict thread isolation and unmanaged memory safety bounds:
- **GC Allocation Target**: Maintain zero Gen 0 allocation during continuous monitoring loops by reusing stack-allocated `Span<T>` and `ReadOnlyMemory<T>` slices.
- **P/Invoke Handle Safety**: When invoking native APIs (e.g. `kernel32!GetSystemTimes` or `psapi!EmptyWorkingSet`), handles returned by `OpenProcess` MUST be created with minimal required access flags (`PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA`) and closed inside a `finally` block via `NativeMethods.CloseHandle`.
- **Lock Free Synchronization**: Use `SemaphoreSlim(1, 1)` for asynchronous I/O synchronization rather than blocking `lock(this)` primitives to prevent UI thread dispatcher freezes.

```csharp
// Low-Level Native Memory Pinning Example for Developer Extensions
using System;
using System.Runtime.InteropServices;

public static class UnmanagedBufferManager
{
    public static void ExecuteWithPinnedBuffer(byte[] buffer, Action<IntPtr, int> nativeAction)
    {
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr pointer = handle.AddrOfPinnedObject();
            nativeAction(pointer, buffer.Length);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }
}
```

### 📘 Code Explanation & Technical Walkthrough
- **`GCHandle.Alloc(buffer, GCHandleType.Pinned)`**: Pins the managed `byte[]` array in physical RAM memory, preventing the CLR Garbage Collector from compacting or relocating the memory address while unmanaged Win32 P/Invoke APIs execute.
- **`handle.Free()` in `finally`**: Releases the pinning lock immediately after native execution finishes, allowing the CLR memory manager to resume normal GC optimization without memory fragmentation.

---

### 2. Roblox Studio Ring Wrapper Invariants 
For all Roblox Studio Luau scripts integrated with Heaplit or Roblox_Studio MCP server:
- **Layering Constraint**: A module in **Ring N** (`Rings.RingN`) can require modules from **Ring M** if and only if $M \le N$.
  - **Ring 0** (`Rings.Ring0`): Independent math/formatting utilities (e.g. `RingWorld.Rings.Ring0.Suffixes.FormatNumber`). MUST NOT require Ring 1+.
  - **Ring 1** (`Rings.Ring1`): Shared data models. Can require Ring 0-1.
  - **Ring 2** (`Rings.Ring2`): Game state logic. Can require Ring 0-2.
  - **Ring 3** (`Rings.Ring3`): Networking/Remote events. Can require Ring 0-3.
  - **Ring 4** (`Rings.Ring4`): Client UI & overlays. Can require Ring 0-4.
- **Canonical Formatting Utility**: ALWAYS use `RingWorld.Rings.Ring0.Suffixes.FormatNumber` for numeric abbreviations ("Mil", "Bil", "Tril") across all player HUD screens.

```lua
-- Canonical Luau Ring 0 Number Formatter Invocation
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local FormatNumber = require(ReplicatedStorage.RingWorld.Rings.Ring0.Suffixes.FormatNumber)

local formattedCoins = FormatNumber.FormatSuffix(1250000000) -- Returns "1.25 Bil"
print("Formatted Player Gold: " .. formattedCoins)
```

### 📘 Code Explanation & Technical Walkthrough
- **`FormatNumber.FormatSuffix(1250000000)`**: Converts raw double-precision numeric values into standardized human-readable strings using canonical suffixes (`K`, `Mil`, `Bil`, `Tril`).
- **Ring Dependency Compliance**: Requiring `Ring0.Suffixes.FormatNumber` from any higher layer (Ring 1 through Ring 4) strictly adheres to the $M \le N$ invariant, preventing circular dependency timeouts in Roblox Studio.

---

### 3. Step-by-Step Developer Diagnostic & Debugging Protocol

If an unexpected exception occurs in this subsystem during remote desktop execution or local development:

1. **Verify Process Singleton Lock**:
   - Check if an orphaned `HeaplitLauncher.exe` instance is running in the background using PowerShell:
     ```powershell
     Get-Process -Name 'HeaplitLauncher' -ErrorAction SilentlyContinue | Stop-Process -Force
     ```
2. **Inspect Memory File Locks**:
   - Confirm `memory.txt` is accessible and not locked exclusively by an external text editor. Heaplit opens streams with `FileShare.ReadWrite | FileShare.Delete`.
   - If corrupted, verify that `memory_backup.txt` contains the last known good state and execute:
     ```powershell
     Copy-Item memory_backup.txt memory.txt -Force
     ```
3. **Validate Native P/Invoke Call Returns**:
   - For `GetSystemTimes` failures, inspect if CPU tick counters underflowed during Hyper-V VM host core migration.
   - For `EmptyWorkingSet` failures, verify the process handle was created with `PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA` rights.
