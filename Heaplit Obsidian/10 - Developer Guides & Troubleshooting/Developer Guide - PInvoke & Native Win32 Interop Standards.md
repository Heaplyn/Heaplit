---
title: "⚙️ Developer Guide - PInvoke & Native Win32 Interop Standards"
tags: ['developer-guide', 'pinvoke', 'win32', 'nativemethods', 'kernel32', 'user32', 'psapi']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Developer Master Guide (10+ Pages)"
status: VERIFIED_COMPLETE
---

# ⚙️ Developer Guide - PInvoke & Native Win32 Interop Standards

## 📌 Document Overview & Summary
Exhaustive standard for unmanaged Win32 P/Invoke interop in Heaplit, detailing exact DllImport signatures, struct memory layouts, pinning, set error handling, and memory working set optimization.


## Executive Overview

Heaplit leverages unmanaged Windows APIs for low-overhead CPU timing, physical working set trimming, transparent overlay window positioning, and global input handling. To prevent heap corruption, access violations (`0xC0000005`), and GC relocation bugs, all P/Invoke declarations must strictly follow the standards defined in this guide.

## Canonical `NativeMethods.cs` Definition Standards

All P/Invoke methods must be declared inside `internal static partial class NativeMethods` decorated with `[SuppressUnmanagedCodeSecurity]`.

```csharp
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Heaplit.Core.Native
{
    [SuppressUnmanagedCodeSecurity]
    internal static partial class NativeMethods
    {
        // SetLastError = true allows Marshal.GetLastWin32Error() inspection
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTimes(
            out FILETIME lpIdleTime,
            out FILETIME lpKernelTime,
            out FILETIME lpUserTime);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint processAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        // Process Access Rights Flags
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_SET_QUOTA = 0x0100;
        public const uint PROCESS_VM_READ = 0x0010;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public readonly ulong ToTicks()
        {
            return ((ulong)dwHighDateTime << 32) | dwLowDateTime;
        }
    }
}
```

### 📘 Code Explanation & Technical Walkthrough

- **`FILETIME` Struct Layout**: Decorated with `[StructLayout(LayoutKind.Sequential)]` to guarantee that the 64-bit Windows file time structure maps sequentially in memory (4 bytes for low dword, 4 bytes for high dword). The `ToTicks()` helper bit-shifts `dwHighDateTime` left by 32 bits and ORs it with `dwLowDateTime` to yield total 100-nanosecond tick intervals.
- **`GetSystemTimes` Marshalling**: Parameters are declared as `out FILETIME` to allow the CLR marshaller to pass native memory pointers directly into `kernel32.dll`. `[return: MarshalAs(UnmanagedType.Bool)]` guarantees that the 32-bit Win32 `BOOL` return value is correctly converted into a C# `bool`.
- **`EmptyWorkingSet` Process Access Requirements**: Operating `EmptyWorkingSet(IntPtr hProcess)` requires process handles created via `OpenProcess` with `PROCESS_QUERY_INFORMATION (0x0400)` and `PROCESS_SET_QUOTA (0x0100)`. Passing handles with excessive privileges (such as `PROCESS_ALL_ACCESS`) violates least-privilege security standards and causes access denied errors (`ERROR_ACCESS_DENIED / 5`) when targeting protected system services.
- **`CloseHandle` Clean-up**: Every handle returned by `OpenProcess` must be wrapped in a `try...finally` block calling `NativeMethods.CloseHandle(hProcess)` to avoid kernel process handle leaks that degrade Windows kernel pool memory.

## CPU Timing & Delta Math Invariants

When calculating CPU utilization percentage using `GetSystemTimes`, developers must handle 64-bit tick wrapping and system idle counter underflows.

$$	ext{CPU Usage \%} = 100.0 	imes \left(1.0 - rac{	ext{IdleDelta}}{	ext{KernelDelta} + 	ext{UserDelta}}
ight)$$

```csharp
public static double CalculateCpuUsagePercentage(FILETIME prevIdle, FILETIME prevKernel, FILETIME prevUser,
                                                FILETIME currIdle, FILETIME currKernel, FILETIME currUser)
{
    ulong idleDelta = currIdle.ToTicks() - prevIdle.ToTicks();
    ulong kernelDelta = currKernel.ToTicks() - prevKernel.ToTicks();
    ulong userDelta = currUser.ToTicks() - prevUser.ToTicks();

    ulong totalSys = kernelDelta + userDelta;
    if (totalSys == 0 || currIdle.ToTicks() < prevIdle.ToTicks())
    {
        return 0.0; // Underflow protection fallback
    }

    double usage = (1.0 - ((double)idleDelta / totalSys)) * 100.0;
    return Math.Clamp(usage, 0.0, 100.0);
}
```

### 📘 Code Explanation & Technical Walkthrough

- **Underflow Safeguard (`currIdle.ToTicks() < prevIdle.ToTicks()`)**: Under virtualization hypervisors (e.g., Hyper-V, VMware, RDP sessions), timing counters can occasionally register out-of-order tick snapshots during host CPU core migration. The safety check `currIdle < prevIdle` prevents negative numbers or integer underflow wraparounds (`18,446,744,073,709,551,615`), gracefully returning `0.0%` CPU utilization until the next valid sample interval.
- **`Math.Clamp(usage, 0.0, 100.0)`**: Guarantees that multi-core system kernel times (where Kernel Time includes Idle Time) never produce values lower than 0% or higher than 100%.

## Troubleshooting Native Interop Edge Cases

> [!WARNING]
> Never pass managed delegate callbacks into Win32 function hooks (`SetWindowsHookEx`) without assigning the delegate to a static GC root field. If GC collects the delegate while `user32.dll` still holds the function pointer, the application will crash with `ExecutionEngineException` or `AccessViolationException` when an input event fires.


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
