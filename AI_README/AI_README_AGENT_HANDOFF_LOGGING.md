# AGENT HAND-OFF & SESSION LOGGING MANUAL

## 1. THE GOLDEN HAND-OFF RULE
As an agent, you are part of a continuous cycle of AI developer sessions. To ensure seamless continuity and prevent regression:
> [!IMPORTANT]
> **Every time you modify the codebase, you MUST write or append your actions to the session logs (`walkthrough.md`) and keep these README manuals updated.**
> This allows the next agent (e.g., Android Studio AI, subagents, or peer models) to immediately comprehend what was changed, why, and how to build upon it.

---

## 2. SESSION RESUMPTION SEQUENCE
When you boot into a new session, follow this diagnostic checklist immediately:
1.  **Read the Master Index**: Read [`AI_README.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README/AI_README.md).
2.  **Read the Latest Walkthrough**: Read the `walkthrough.md` in the conversation artifacts directory (`C:\Users\Kyle\.gemini\antigravity\brain\<conversation_id>\walkthrough.md`) to see the latest achievements.
3.  **Audit Workspace**: Run `git status` or scan modified files recursively to see the current codebase state.
4.  **Confirm Build Integrity**: Run `dotnet build` before writing any new code to verify that the project is in a compiling state.

---

## 3. HOW TO DOCUMENT YOUR CHANGES (LOGGING POLICY)
When you complete a task:
1.  **Update the Walkthrough**: Modify `walkthrough.md` in the brain/artifacts folder. Format it with:
    *   **Goal Description**: What was the user request?
    *   **Files Modified**: Absolute file URIs pointing to modified classes, methods, or resources.
    *   **Technical Details**: Design decisions, WPF apartment state resolutions, or layer adjustments.
    *   **Verification Results**: Evidence of compilation success (`dotnet build`).
2.  **Enrich the Layer Guides**:
    *   If you added a core engine, document it in [`AI_README_LAYER0.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README/AI_README_LAYER0.md).
    *   If you added a UI dashboard, document it in [`AI_README_LAYER2.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README/AI_README_LAYER2.md).
    *   If you added a new command handler, document it in [`AI_README_LAYER3.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README/AI_README_LAYER3.md).

---

## 4. CROSS-IDE SYNC (ANDROID STUDIO AI INTERFACE)
Heaplit communicates with Android clients (such as Heaplit Mobile app developed in Android Studio) via `MobileBridgeServer.cs` in Layer 1.
If you are passing off work to an Android Studio AI:
1.  **Document API Contracts**: Describe the exact JSON structures sent over the WebSocket bridge or network commands.
2.  **Verify Sibling Repositories**: Update [`AI_README_WEB_SCRAPING_DEVICES.md`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/AI_README/AI_README_WEB_SCRAPING_DEVICES.md) to log any new device bridges or scraping endpoints.
3.  **Explain C# Backends**: Provide clear entry point classes (e.g. `UrlPullerManager`) so the Android AI can implement corresponding Java/Kotlin interfaces.
