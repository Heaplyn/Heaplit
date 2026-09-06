---
title: "🧬 Personality Evolution & Cognitive Context Ingestion"
tags: ['personality', 'evolver', 'perception', 'context', 'vision', 'ocr', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 🧬 Personality Evolution & Cognitive Context Ingestion

## 🧬 Personality Evolution & Desktop Perception

### 1. The Hourly Evolution Cycle (`PersonalityEvolver.cs`)
Every hour, `PersonalityEvolver` inspects the last 2 conversation session logs in `Data/Conversations/`, extracts the user's conversational vibe (sarcastic, serious, friendly, chaotic), and refines `Data/Instructions/PersonalityProfile.md` via `LlmRouter`.

```mermaid
graph TD
    Timer["Hourly Delay Loop"] --> Scan["Scan Data/Conversations/ (Last 2 Logs)"]
    Scan --> Buffer["Cap History at 8,000 Characters"]
    Buffer --> Prompt["Send Meta-Prompt to LlmRouter"]
    Prompt --> Evolve["Receive Evolved PersonalityProfile.md"]
    Evolve --> Save["Atomic Write to Data/Instructions/PersonalityProfile.md"]
```

### 2. Multi-Monitor Screen Perception (`PerceptionContextInjector.cs`)
Captures active foreground window titles, clipboard contents, and visual OCR bounding boxes to provide real-time context to AI conversations.
