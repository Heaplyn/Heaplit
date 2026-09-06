# Heaplit PC Feature Catalog

This document provides an exhaustive list of all major functional modules in Heaplit PC, grouped by domain.

## 1. AI & Automation
- **AI Chat (`AiCommandHandler`, `ChatOverlay`)**: The primary multi-turn conversational interface.
- **Agent Execution (`AgentExecutor`, `AutonomousAgentEngine`)**: Background agents that can execute shell scripts, read/write files, and perform system tasks.
- **LLM Routing (`LlmRouter`)**: Supports Gemini (with Key Rotation), Anthropic, Groq, Mistral, Perplexity, OpenRouter, and local Ollama/P2P backends.
- **Concise Shorthand Protocol**: Optimized `@rf`, `@wf`, `@ps` tags for faster AI-to-System communication.
- **Custom Data Processor (@proc)**: A bridge for executing external logic in Python, Node.js, or C# ([API Guide](CUSTOM_PROCESSOR_API.md)).
- **Autonomous Reflection**: Periodic background cycles where Heaplit reviews history and memory to suggest or perform proactive tasks.
- **Context Management (`ContextOptimizer`, `EmotionalContextManager`)**: Dynamically prunes the system prompt based on user mood and project state.

## 2. System & Power
- **Power Control (`PowerCommandHandler`)**: System Shutdown, Restart, Sleep, and Hibernate.
- **Process Management (`ProcessKillerCommandHandler`, `ProcessManagerOverlay`)**: Monitor CPU/RAM usage and force-terminate hanging processes.
- **Restart Engine (`NativeMethods.Restart`)**: Orchestrates Git pulls, .NET builds, and cold-starts for auto-updating.
- **Lock & Security (`LockCommandHandler`)**: Instantly secures the workstation.

## 3. Developer Utilities
- **Git Integration (`GitPushCommandHandler`, `GitSetupCommandHandler`)**: AI-generated commit messages, staging, and pushing to GitHub.
- **Code Assistance (`CodeAssistCommandHandler`, `CodeAssistOverlay`)**: Real-time screen and workspace analysis for debugging and refactoring.
- **CLI Runner (`CliRunnerCommandHandler`, `PowerShellRunnerCommandHandler`)**: Execute CMD and PowerShell scripts directly from the HUD.
- **IPA Compiler (`IpaCompilerService`, `IpaCompilerOverlay`)**: Compiles C# projects for iOS and deploys to mobile.

## 4. Media & Audio
- **Volume & Mute (`VolumeCommandHandler`, `MuteCommandHandler`)**: Fine-grained control over system audio devices.
- **TTS Engine (`TtsManager`, `TtsVoiceLibraryOverlay`)**: High-quality speech synthesis with custom voice cloning support.
- **Media Conversion (`FFMpegCommandHandler`, `MediaConverterOverlay`)**: Advanced FFmpeg wrapper for processing video, audio, and GIFs.
- **Spotify & Music (`MusicPlaylistCommandHandler`)**: Control playback and manage playlists.

## 5. Files & Organization
- **File Organizer (`FileOrganizerCommandHandler`, `FileOrganizerOverlay`)**: Automatically categorizes folders by extension, date, or duplicates.
- **Grid Dashboard (`GridCommandHandler`, `FileGridOverlay`)**: A visual launchpad for pinned files and folders.
- **Text Editor (`EditCommandHandler`, `TextEditorOverlay`)**: Built-in glassmorphic code/text editor.

## 6. Productivity & ADHD Tools
- **ADHD Focus Suite (`AdhdFocusSuiteHandler`)**: Pomodoro timers, task micro-chunking, and dopamine check-ins.
- **Sticky Notes (`StickyNotesCommandHandler`, `StickyNotesOverlay`)**: Persistent on-screen widgets for quick thoughts.
- **Calendar & Reminders (`CalendarCommandHandler`, `ReminderCommandHandler`)**: Visual event planning and notification scheduling.

## 7. Connectivity & Diagnostics
- **Mobile Hub (`MobileBridgeServer`, `PhoneControlCommandHandler`)**: Orchestrates the link between the PC HUD and the Mobile app.
- **Debug Console (`DebugConsoleOverlay`)**: Tiered verbosity system (None, Minimal, Full) for real-time monitoring.
- **System Specs (`SysInfoCommandHandler`, `SystemSpecsOverlay`)**: Detailed hardware and network diagnostics.
- **Web Research (`WebOperationManager`)**: Registry searching (NuGet/npm/pypi) and documentation ingestion into semantic memory.
- **Search & Launch**: High-speed indexing of Windows applications and Start Menu shortcuts. Supports automatic periodic refreshes.
- **App Management**: Ability to force re-index system apps via `reindex` command.
- **Universal Installer**: Scrapes sites and auto-deploys Windows installers via winget.
