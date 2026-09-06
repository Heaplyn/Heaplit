---
title: "🛠️ Developer HUDs Master Guide (BuildStudio, DevSuite, McpStudio)"
tags: ['devhuds', 'buildstudio', 'devsuite', 'mcpstudio', 'chatoverlay', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 🛠️ Developer HUDs Master Guide (BuildStudio, DevSuite, McpStudio)

## 🛠️ Specialized Engineering & Developer Overlays

Heaplit houses specialized HUDs for software engineering, decompilation, and model management:

### 1. `BuildStudioOverlay` (`Modules/Layer2/Dev/BuildStudioOverlay.cs`)
- Visual real-time MSBuild output stream with syntax-highlighted error logs and clickable source jump links.

### 2. `DisassemblerSuiteOverlay` (`Modules/Layer2/Dev/DisassemblerSuite/`)
- Native binary disassembly explorer displaying PE/COFF sections, hex dumps, export tables, and Ghidra decompiled C code.

### 3. `McpStudioOverlay` (`Modules/Layer2/Dev/McpStudioOverlay.cs`)
- Live inspector and testing harness for active Model Context Protocol (MCP) server connections (e.g. Roblox Studio MCP).

### 4. `ChatOverlay` (`Modules/Layer2/AI/ChatOverlay.cs`)
- Floating conversational AI workspace with SSE streaming tokens, syntax-highlighted code blocks, and audio speech toggles.
