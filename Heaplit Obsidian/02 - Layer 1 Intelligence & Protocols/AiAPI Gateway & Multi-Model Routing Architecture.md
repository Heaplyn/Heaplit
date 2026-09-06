---
title: "🌐 AiAPI Gateway & Multi-Model Routing Architecture"
tags: ['aiapi', 'llm', 'gemini', 'claude', 'gpt', 'ollama', 'streaming', 'sse', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 🌐 AiAPI Gateway & Multi-Model Routing Architecture

## 🌐 Multi-Model Intelligence Gateway

`AiAPI` (`Modules/Layer1/AiAPI.cs`) and `LlmRouter` (`Modules/Layer0/AI_ML/LlmRouter.cs`) form the unified cognitive engine of Heaplit.

```mermaid
sequenceDiagram
    autonumber
    participant User as User / ChatOverlay
    participant Gateway as AiAPI Gateway
    participant Router as LlmRouter
    participant Primary as Gemini 2.5 Pro (Primary)
    participant Secondary as Claude 3.5 Sonnet (Failover)
    participant Offline as Local Ollama (Offline Failover)

    User->>Gateway: SendMessageAsync(Prompt, Options)
    Gateway->>Router: SelectProvider(PromptTokens, Complexity)
    Router-->>Gateway: Route to Gemini 2.5 Pro
    Gateway->>Primary: POST /v1/chat/completions (Stream: true, Tools: [AiToolRegistry])
    
    alt Primary Rate Limited (HTTP 429) or Network Drop
        Primary-->>Gateway: HTTP 429 / 503 Service Unavailable
        Gateway->>Gateway: Log Warning & Switch Provider
        Gateway->>Secondary: POST /v1/chat/completions (Stream: true)
    end

    loop SSE Token Streaming
        Secondary-->>Gateway: chunk: {"delta": {"content": "..."}}
        Gateway-->>User: OnTokenReceived(chunk)
    end

    alt Zero Internet Connectivity
        Gateway->>Offline: POST http://localhost:11434/api/generate
        Offline-->>Gateway: Local Model Tokens
        Gateway-->>User: Render Local Response
    end
```

---

## 🔀 Multi-Model Routing Heuristics Matrix

| Scenario / Prompt Type | Selected Provider | Technical Rationale |
| :--- | :--- | :--- |
| **Massive Context (>100k Tokens)** | **Google Gemini 2.5 Pro** | 1M+ token context window; optimal for multi-file codebase analysis. |
| **Complex Logic & Refactoring** | **Anthropic Claude 3.5 Sonnet** | Industry-leading algorithmic code synthesis and tool calling accuracy. |
| **Fast Conversational Lookups** | **Google Gemini 2.5 Flash** | Sub-200ms time-to-first-token; optimal for quick search queries. |
| **Zero Internet / Privacy Mode** | **Local Ollama (`llama3:8b`)** | 100% offline local inference with zero data transmission. |

---

## 🛠️ Troubleshooting AI Gateway Errors

### 1. `HTTP 429: Rate Limit Exceeded`
- **Root Cause**: API quota exhausted or burst requests.
- **Fix**: `AiAPI` automatically catches HTTP 429 and retries against the configured secondary provider (e.g. failover from Gemini to Claude or GPT-4o) without presenting an error modal to the user.

### 2. `Invalid API Key / Unauthorized (401)`
- **Root Cause**: Expired or missing API key in `Data/SystemSettings.json`.
- **Fix**: Open `LlmSettingsOverlay` via the launcher search command `settings` and update the active provider API keys.
