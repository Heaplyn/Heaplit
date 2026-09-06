---
title: "🛠️ System & Hardware Command Handlers Complete Reference"
tags: ['handlers', 'system', 'stats', 'power', 'brightness', 'killer', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 🛠️ System & Hardware Command Handlers Complete Reference

## 🛠️ Complete Reference: System Command Handlers

Located in `Modules/Layer3/Handlers/System/`:

### 1. `SystemStatsCommandHandler` (`cpu`, `ram`, `sys`, `stats`)
- Background polling of CPU and RAM telemetry via `NativeMethods.GetSystemTimes` and `GlobalMemoryStatusEx`.

### 2. `ProcessKillerCommandHandler` (`kill`, `end task`, `terminate`)
- Searches running tasks and invokes `Process.Kill()`.

### 3. `BrightnessCommandHandler` (`bright`, `dim`, `screen`)
- Adjusts monitor backlight via WMI (`WmiMonitorBrightnessMethods`).

### 4. `PowerCommandHandler` (`shutdown`, `sleep`, `hibernate`)
- Transitions Windows power states via `ExitWindowsEx` and `SetSuspendState`.

### 5. `LockCommandHandler` (`lock`, `lockscreen`)
- Locks desktop workstation via `NativeMethods.LockWorkStation()`.

### 6. `RestartCommandHandler` (`restart`, `reboot`, `rebuild`)
- Terminates current process, rebuilds via `run.bat`, and relaunches `HeaplitLauncher.exe`.
