---
title: "📊 Developer Guide - CPU Times Delta Calculation & Underflow Protection"
tags: ['developer-guide', 'cpu-monitoring', 'getsystemtimes', 'kernel32', 'underflow-protection']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Developer Master Guide (10+ Pages)"
status: VERIFIED_COMPLETE
---

# 📊 Developer Guide - CPU Times Delta Calculation & Underflow Protection

## 📌 Document Overview & Summary
Deep-dive mathematical guide detailing unmanaged CPU timing calculations via NativeMethods.GetSystemTimes, tick delta conversions, multi-core aggregation, and underflow protections.


## Mathematical Formulation

Total CPU utilization across all system cores is computed by measuring the rate of change of System Idle Time, System Kernel Time, and System User Time between two distinct time snapshots $T_1$ and $T_2$.

$$\Delta 	ext{Idle} = 	ext{Idle}_{T_2} - 	ext{Idle}_{T_1}$$

$$\Delta 	ext{Kernel} = 	ext{Kernel}_{T_2} - 	ext{Kernel}_{T_1}$$

$$\Delta 	ext{User} = 	ext{User}_{T_2} - 	ext{User}_{T_1}$$

$$	ext{System Total Delta} = \Delta 	ext{Kernel} + \Delta 	ext{User}$$

$$	ext{CPU Usage \%} = \left(1.0 - rac{\Delta 	ext{Idle}}{	ext{System Total Delta}}
ight) 	imes 100.0$$

> [!NOTE]
> In Windows `GetSystemTimes`, **Kernel Time includes Idle Time**. Therefore, $\Delta 	ext{Kernel}$ represents total time spent executing kernel-mode code *plus* idle time.

## C# Implementation & Safe Calculator Class

```csharp
using System;
using Heaplit.Core.Native;

namespace Heaplit.Core.Optimization
{
    public sealed class CpuUsageMonitor
    {
        private FILETIME _prevIdle;
        private FILETIME _prevKernel;
        private FILETIME _prevUser;
        private bool _isFirstSample = true;

        public double SampleCpuUsagePercentage()
        {
            if (!NativeMethods.GetSystemTimes(out FILETIME currIdle, out FILETIME currKernel, out FILETIME currUser))
            {
                return 0.0;
            }

            if (_isFirstSample)
            {
                _prevIdle = currIdle;
                _prevKernel = currKernel;
                _prevUser = currUser;
                _isFirstSample = false;
                return 0.0;
            }

            ulong idleDelta = currIdle.ToTicks() - _prevIdle.ToTicks();
            ulong kernelDelta = currKernel.ToTicks() - _prevKernel.ToTicks();
            ulong userDelta = currUser.ToTicks() - _prevUser.ToTicks();

            // Store current snapshot for next sample interval
            _prevIdle = currIdle;
            _prevKernel = currKernel;
            _prevUser = currUser;

            ulong totalSys = kernelDelta + userDelta;

            // Underflow & zero-delta safety protection
            if (totalSys == 0 || currIdle.ToTicks() < _prevIdle.ToTicks())
            {
                return 0.0;
            }

            double cpuPercent = (1.0 - ((double)idleDelta / totalSys)) * 100.0;
            return Math.Clamp(cpuPercent, 0.0, 100.0);
        }
    }
}
```

### 📘 Code Explanation & Technical Walkthrough

- **First Sample Initialization (`_isFirstSample`)**: On the initial poll call, baseline snapshots are captured without computing a delta, preventing massive false spike readings on startup.
- **Underflow Protection (`currIdle.ToTicks() < _prevIdle.ToTicks()`)**: Safeguards against out-of-order counter returns caused by hypervisor host CPU migration during remote desktop (RDP) sessions.
- **`Math.Clamp(cpuPercent, 0.0, 100.0)`**: Guarantees that arithmetic rounding precision limits never emit out-of-range percentage values to UI HUD charts.


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
