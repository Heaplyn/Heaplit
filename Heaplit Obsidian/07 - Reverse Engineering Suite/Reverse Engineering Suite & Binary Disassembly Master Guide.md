---
title: "🔬 Reverse Engineering Suite & Binary Disassembly Master Guide"
tags: ['reverse-engineering', 'disassembler', 'ghidra', 'unassemblize', 'lief', 'pe', 'elf', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 🔬 Reverse Engineering Suite & Binary Disassembly Master Guide

## 🔬 Complete Reverse Engineering Suite Reference

Located in `Data/ReversedTools/` and `Modules/Layer2/Dev/DisassemblerSuite/`:

```mermaid
graph TD
    Binary["Target Executable (.exe, .dll, .so, .elf)"]
    Binary --> PE["PeStatics: Header, Section & IAT Parser"]
    Binary --> Unas["Unassemblize: C++ LIEF Binary Engine"]
    Binary --> Ghidra["Ghidra Headless Decompiler Bridge"]

    PE --> UI["DisassemblerSuiteOverlay (Hex, Sections, Imports)"]
    Unas --> UI
    Ghidra --> Pseudo["Decompiled C Pseudo-Code Project"]
```

### Integrated Reverse Engineering Engines:
1. **Unassemblize**: C++ LIEF binary parser extracting COFF/ELF sections, symbols, and entropy.
2. **Ghidra Headless Bridge**: Bundled **Ghidra 11.0.3** decompilation into clean C pseudo-code.
3. **Android Disassembler**: Dalvik/ART bytecode and DEX class disassembler.
