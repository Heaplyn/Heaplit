---
title: "📊 SystemMonitorOverlay & Diagnostic Telemetry HUD"
tags: ['sysmon', 'telemetry', 'debugger', 'optimizer', 'ui', 'process-manager', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 📊 SystemMonitorOverlay & Diagnostic Telemetry HUD

## 📊 Complete SystemMonitorOverlay Reference

`SystemMonitorOverlay` (`Modules/Layer2/System/SystemMonitorOverlay.cs`) is Heaplit's master diagnostic telemetry HUD, process manager, and 1-click autonomic PC optimizer.

```mermaid
graph TD
    SMO["SystemMonitorOverlay (780 x 600 HUD)"]
    
    subgraph Row0["Row 0: Hero PC Optimizer Bar"]
        B1["🧠 RAM COMPACT (Algorithmic Working Set Reclaim)"]
        B2["🧹 JUNK PURGE (%TEMP% & CrashDumps)"]
        B3["🚀 MAX PC OPTIMIZE (Complete 4-Stage Pipeline)"]
    end

    subgraph Row1["Row 1: Live Telemetry Gauges"]
        G1["⚡ CPU Usage % (GetSystemTimes)"]
        G2["🧠 RAM Used / Total GB (GlobalMemoryStatusEx)"]
        G3["💾 Disk C: Used / Free GB"]
        G4["🌐 Network Live Down / Up Speed"]
        G5["⚙️ Process Count & Uptime"]
    end

    subgraph Row2["Row 2: Interactive Process Manager"]
        Search["🔍 Real-Time Process Search Box"]
        Filters["Presets: ALL | HEAVY (>150MB) | ZOMBIE / HUNG"]
        Grid["Process DataGrid (PID, Name, Memory, Status)"]
        Actions["Actions: End Task | Kill Tree | Trim RAM"]
    end

    subgraph Row3["Row 3: Footer & Quick Utilities"]
        DNS["🌐 Flush DNS Resolver Cache"]
        Status["Live Diagnostic Status Banner"]
    end

    SMO --> Row0
    SMO --> Row1
    SMO --> Row2
    SMO --> Row3
```

---

## ⚡ Interactive Process Actions
- **Double-Click Row**: Instantly invokes `Process.Kill()` on the target process.
- **Kill Process Tree**: Recursively terminates the target process and all child sub-processes.
- **Trim RAM**: Safely calls Win32 `EmptyWorkingSet` on the selected process handle without terminating it.
