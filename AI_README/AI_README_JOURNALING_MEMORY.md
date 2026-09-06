# JOURNALING, MEMORY & CONTEXT SUBSYSTEM (`Modules/Layer0/`)

## OVERVIEW
Heaplit retains long-term developer activity logs, clipboard snippets, context notes, and active session histories to provide continuous awareness across coding sessions.

---

## CORE MODULE DETAILS

### 1. `ActionJournalManager.cs` & `ChronoLogManager.cs`
- **`ActionJournalManager`**: Append-only log tracking discrete operations (file creations, builds, test runs, command triggers, git commits). Used by AI agents to trace past session actions.
- **`ChronoLogManager`**: Time-series log manager that groups developer events into chronological timeline blocks for historical review and post-mortem analysis.

### 2. `BackgroundContextManager.cs` & `ContextOptimizer.cs`
- **`BackgroundContextManager`**: Polls active foreground application titles, open editor tabs, and recent terminal runs.
- **`ContextOptimizer`**: Applies token-reduction algorithms (removing redundant whitespace, summarizing lengthy stack traces, truncating duplicate log lines) before sending context to LLM models.

### 3. `ClipboardHistoryManager.cs`
- Monitors Windows Clipboard for text and image copies.
- Stores historical clipboard items in an indexed cache, making past code snippets instantly searchable via command handlers.

### 4. `ContextNotesManager.cs` & `StickyNotesOverlay.cs`
- Manages persistent developer scratchpad notes.
- Syncs notes directly with `StickyNotesOverlay` for floating glassmorphic desktop reminders.
