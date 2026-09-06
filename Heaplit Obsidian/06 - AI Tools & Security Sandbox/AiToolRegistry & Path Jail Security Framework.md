---
title: "🔒 AiToolRegistry & Path Jail Security Framework"
tags: ['aitools', 'registry', 'jail', 'security', 'sandbox', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 🔒 AiToolRegistry & Path Jail Security Framework

## 🔒 Tool Reflection & Sandboxed Execution

`AiToolRegistry` (`Modules/Layer0/AiTools/AiToolRegistry.cs`) indexes all classes implementing `IAiTool` and exports standard JSON function schemas to LLMs.

```mermaid
graph LR
    Scan["Reflection Scan (IAiTool)"] --> Reg["AiToolRegistry"]
    Reg --> Schema["Generate JSON Function Call Schemas"]
    Schema --> Prompt["Injected into LLM Tools Parameter"]
    LLM["LLM Function Call Response"] --> Dispatch["AiToolRegistry.ExecuteToolAsync()"]
    Dispatch --> Jail["AiPathJail Boundary Check"]
    Jail --> Tool["Tool Execution & Return Result"]
```

---

## 🛡️ `AiPathJail` Filesystem Security Rules
- Prevents file writing or deletion outside allowed project workspaces.
- Strictly blocks operations targeting `%WINDIR%`, `C:\Windows`, or system partition root files.
