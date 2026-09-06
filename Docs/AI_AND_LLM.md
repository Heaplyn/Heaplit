# AI & LLM Implementation Guide

Heaplit is designed to be "Model Agnostic," meaning it can switch between different AI backends seamlessly without changing the core application logic.

## The AI Pipeline
The flow of a message through Heaplit follows this cycle:
1. **Input**: User speaks or types a query.
2. **Context Injection**: `AiAPI` injects the **Project Map**, **User Memory**, and **Active Window** into the prompt.
3. **Routing**: `LlmRouter` chooses the best backend (e.g., Gemini if online, Ollama if offline).
4. **Execution Loop**:
    - AI returns text + potential **Action Tags** (e.g., `[READ_FILE: path]`).
    - `AiAPI` intercepts these tags and executes them locally.
    - The output of the tool is fed back to the AI for a final summary.
5. **Output**: The final clean response is shown in the `ChatOverlay` and spoken via `TtsManager`.

## Actions & Capabilities
Heaplit provides the AI with several "Hands" in the system. To save tokens and reduce latency, the AI uses a **Concise Shorthand Protocol** (@):

- **Read**: `@rf{path}` or `[READ_FILE]`
- **Write**: `@wf{path}{content}` or `[WRITE_FILE]`
- **System**: `@ps{cmd}` (PowerShell), `@run{cmd}` (Heaplit Command)
- **Vision**: `@snap` (Screenshot)
- **Identity**: `@ingest{url}` (Learn Documentation), `@reg{type, query}` (Search NuGet/npm)

### 🛡️ PC Control Safety Toggle
For privacy and safety, you can disable the AI's ability to control your computer in **Settings -> General**.
- **When Enabled**: Heaplit can run PowerShell scripts, modify files, and execute system commands.
- **When Disabled**: Heaplit is restricted to "Observation & Speech" mode. He can still talk to you and analyze your screen, but he cannot write files or execute scripts.
- **How it Works**: The `AiAPI` and `AgentExecutor` modules perform a "Relevance Gate" check against this setting before processing any [ACTION] tags.

### 🔑 Key Rotation & Reliability
Heaplit supports multiple Gemini API keys. In **Settings -> LLM**, you can provide a list of keys separated by semicolons (`;`). 
- If a key hits a rate limit (429) or is invalid, Heaplit will automatically rotate to the next key in the pool to ensure your task is finished without interruption.

## Adding a New Backend
To add a new provider (e.g., xAI or DeepSeek API):
1. Add the API Key/Model settings in `SettingsManager.cs`.
2. Implement the `AskProviderAsync` method in `LlmRouter.cs`.
3. Add a configuration panel in `SettingsOverlay.cs` and `LlmSettingsOverlay.cs`.

## Sanitization
Heaplit uses a strictly enforced **Zero-Reasoning Policy**. The AI is instructed to skip drafting and persona checks in its output. `AiAPI.SanitizeText` further strips any leaked reasoning or system tags before the response reaches the UI. Detailed "inner monologue" can be viewed by expanding the **Debug Trace** in the chat bubble.

