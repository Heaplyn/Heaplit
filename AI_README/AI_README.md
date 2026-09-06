# HEAPLIT SYSTEM CONFIG & LLM INTERFACE

> [!IMPORTANT]
> **SYSTEM INSTRUCTION FOR AI AGENTS**: Any AI, LLM, or Agent reading this workspace MUST read this file ENTIRELY from start to finish. Do NOT summarize or skip. Retain all formulas, rules, and architecture specs.

## SYSTEM ARCHITECTURE
```
[Layer 3: Routing/Handlers] -> [Layer 2: WPF Glassmorphic Overlays] -> [Layer 1: Messaging] -> [Layer 0: Core/Engines]
```

## GLOBAL INSTRUCTIONS
- **UI Architecture**: No XAML files. Programmatic WPF construct in C# via `BaseOverlay` subclassing.
- **Build Constraints**: `dotnet build` succeeds in compile check, but copies fail if launcher active (process locks target `.exe`). Avoid false failures.
- **Symbol Maps**: Use standard prefix/suffix matching via `SearchUtil`.
- **Reasoning Density (LLM Internal Logic)**: Future reasoning cycles must prioritize **extreme semantic density over conversational syntax**. Condense logic blocks using conceptual abbreviation mappings, mathematical notation, and keyword concatenation to maximize logic payload per token window. Avoid long conversational transitions in inner thoughts.

## DEPENDENCY HIERARCHY RULES

### Heaplit Architecture Layer Rules
A class in **Layer K** can reference components in **Layer J** if and only if **J < K** (strict downward-only hierarchy). Under no circumstances can a C# module reference, import, or index another module in its own layer (**J = K**), eliminating lateral coupling and circular references.
- **Layer 0** (Core Utilities & Engines): Base layer. **MUST NOT** reference or index other Layer 0 modules, nor higher layers (Layer 1, Layer 2, Layer 3).
- **Layer 1** (Interfaces & Contracts): Can reference Layer 0. **MUST NOT** reference other Layer 1 modules, nor Layer 2 or Layer 3.
- **Layer 2** (UI Overlays): Can reference Layer 1 and Layer 0. **MUST NOT** reference other Layer 2 overlays, nor Layer 3.
- **Layer 3** (Command Routing & Handlers): Can reference Layer 2, Layer 1, and Layer 0. **MUST NOT** reference other Layer 3 handlers.

## PERFORMANCE & SYSTEM LANGUAGES DIRECTIVES
- **Engine Optimization**: Do not hesitate to write, compile, and execute submodules, helper routines, or automation scripts in **Rust, C++, or Node/TypeScript** if C# is not optimal for specific operations (e.g., custom disassembly, mathematical optimizations, high-speed scrapers, or memory-heavy calculations). Performance speed, lightweight footprints, and low-latency execution are absolute priorities.

## MODULE REGISTRY
- **Assistant Purpose & Cognition**: [`AI_README_ASSISTANT_PURPOSE.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_ASSISTANT_PURPOSE.md)
- **AI Tools & Self-Evolution**: [`AI_README_AI_TOOLS.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_AI_TOOLS.md)
- **Audio & Perception**: [`AI_README_AUDIO_PERCEPTION.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_AUDIO_PERCEPTION.md)
- **Journaling & Memory**: [`AI_README_JOURNALING_MEMORY.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_JOURNALING_MEMORY.md)
- **Web, Scraping & Devices**: [`AI_README_WEB_SCRAPING_DEVICES.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_WEB_SCRAPING_DEVICES.md)
- **Mathematical Reference**: [`AI_README_MATHEMATICS_REFERENCE.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README/AI_README_MATHEMATICS_REFERENCE.md)
- **Layer 0 Core**: [`AI_README_LAYER0.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_LAYER0.md)
- **Layer 2 UI**: [`AI_README_LAYER2.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_LAYER2.md)
- **Layer 3 Route**: [`AI_README_LAYER3.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_LAYER3.md)
- **Development & Troubleshooting**: [`AI_README_DEVELOPMENT.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_DEVELOPMENT.md)
- **Coding, Testing & Cheatsheet**: [`AI_README_CODING_TESTING.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_CODING_TESTING.md)
- **Agent Hand-off & Session Logging**: [`AI_README_AGENT_HANDOFF_LOGGING.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README_AGENT_HANDOFF_LOGGING.md)
