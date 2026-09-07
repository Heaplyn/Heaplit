# Windows Security Healer & Defender Malware Recovery

## Overview
The **Windows Security Healer & Malware Recovery Engine** is an enterprise-grade remediation and self-defense subsystem built into Heaplit. If malicious software infects the host system and tampers with or disables Windows Defender, Windows Firewall, Security Center services, or injects rogue Group Policies/exclusions, Heaplit can detect the compromise and perform a complete 1-click restoration.

---

## Key Features

### 1. Multi-Vector Security Auditing
- **Windows Defender Subsystems**: Audits Real-Time Protection, Behavior Monitoring, IOAV (Attachment/Download Scans), Script Scanning (AMSI), and Cloud-Delivered AI Protection.
- **Firewall Profile Telemetry**: Inspects Domain, Private, and Public network firewall active states.
- **Critical Security Services**: Audits service run states for `WinDefend`, `WdNisSvc`, `Sense`, `SecurityHealthService`, `wscsvc` (Security Center), `MpsSvc` (Firewall), and `wuauserv` (Windows Update).
- **Group Policy & Registry Locks**: Detects malware-enforced keys such as `DisableAntiSpyware`, `DisableRealtimeMonitoring`, `DisableTaskMgr`, `DisableRegistryTools`, and `DisableCMD`.
- **Rogue Defender Exclusions**: Detects malware-created folder whitelists (e.g. `C:\`, `AppData`, `Temp`) and binary extensions (`exe`, `dll`, `ps1`, `vbs`).
- **DNS & Hosts File Hijacking**: Detects domain redirection or blackholing of Microsoft Update, Defender telemetry, and antivirus vendors.

---

## 2. 1-Click Automated Recovery Protocol
When executed via `WindowsSecurityManager.ReenableWindowsSecurityAsync()`:
1. **Registry Lock Purge**: Strips all malicious policy keys from `HKLM` and `HKCU`.
2. **Service Elevation & Startup Fix**: Configures all Defender, Firewall, and Security Center services to `Automatic` startup and forces immediate start triggers.
3. **Defense Enforcement**: Enables Real-Time Monitoring, Behavior Monitoring, IOAV, AMSI Script Scan, Cloud Reporting (MAPS Level 2), and PUA Protection.
4. **Firewall Reset**: Re-activates Windows Firewall on Domain, Private, and Public profiles.
5. **Exclusion Scrub**: Purges dangerous root/temporary directory whitelists.
6. **Hosts Integrity Restoration**: Cleans malicious domain sinkholes from `%SystemRoot%\System32\drivers\etc\hosts`.
7. **Threat Signature Fetch**: Triggers an emergency signature update (`Update-MpSignature`).
8. **Malware Quick Scan**: Dispatches an asynchronous Defender Quick Scan (`Start-MpScan -ScanType QuickScan`).

---

## 3. UI & Command Integration
- **Overlay**: `WindowsSecurityHealerOverlay.cs`
  - Real-time Health Meter (0–100%) and posture badge.
  - Interactive vector breakdown cards (Antivirus, Firewall, Services, Registry Locks, Exclusions, Hosts file).
  - Remediation action buttons with live progress log streaming.
- **Command Bar Triggers**:
  - `security`
  - `defender`
  - `fixsecurity`
  - `reenablesecurity`
  - `antivirus`
  - `firewall`
  - `malwarerecovery`
