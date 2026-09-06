# Implementation Plan: Screen Analysis, Memories, and Voice Activation

Add continuous screen analysis to remember user activity and a voice-activated chat system for Heaplit PC.

## User Review Required

> [!IMPORTANT]
> - **Privacy**: Continuous screen analysis will capture everything on your screen. Captured summaries will be stored locally in `Data/Instructions/Memories.md`.
> - **API Usage**: Vision analysis uses Gemini API credits. We will optimize frequency (e.g., every 5 minutes or on demand).
> - **Wake Word**: The default wake word will be "Heaplit".

## Proposed Changes

### Core Logic (Modules/Layer0)

#### [NEW] [ScreenCaptureUtil.cs](file:///C:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer0/ScreenCaptureUtil.cs)
A utility to take screenshots of the primary monitor using `System.Drawing`.

#### [NEW] [ScreenVisionManager.cs](file:///C:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer0/ScreenVisionManager.cs)
Background service that manages the continuous analysis loop.
- Captures screen.
- Sends to Gemini for "memory" generation.
- Appends summaries to `Data/Instructions/Memories.md`.

#### [MODIFY] [AiAPI.cs](file:///C:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer0/AiAPI.cs)
Add support for vision requests (multi-modal) to Gemini.

#### [NEW] [VoiceActivationManager.cs](file:///C:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer0/VoiceActivationManager.cs)
Uses `NAudio` to monitor the microphone for a wake word ("Heaplit").
- Integrates with a lightweight local speech-to-text or uses a frequency-based detection for the wake word.

### UI / Integration (App.xaml.cs)

#### [MODIFY] [App.xaml.cs](file:///C:/Users/Kyle/Downloads/Projects/Heaplit/App.xaml.cs)
Initialize the new managers on startup.

---

## Verification Plan

### Automated Tests
- N/A (Manual verification on PC).

### Manual Verification
- **Screen Analysis**: Verify `Memories.md` is updated with correct descriptions of what was on screen.
- **Voice Activation**: Say "Heaplit" and verify the HUD appears and starts processing.
- **Memory Retrieval**: Ask Heaplit "What was I doing 10 minutes ago?" and verify it uses the `Memories.md` context.
