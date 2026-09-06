# Heaplit PC Architecture

Heaplit is built on a strictly layered architecture designed for modularity, high performance, and rapid extensibility. The project is divided into **Layers (0-4)**, each with a specific responsibility.

## The Layered Model

### [Layer 0] - Core Runtime & Engines
This is the "brain" and "nervous system" of Heaplit.
- **AI API (`AiAPI.cs`)**: Orchestrates communication with LLM providers. Handles prompt sanitization, tool-call execution, and multi-turn loops.
- **LLM Router (`LlmRouter.cs`)**: A dispatcher that routes queries to Gemini, OpenAI, Anthropic, or local Ollama instances based on internet availability and user preference.
- **Voice Systems (`VoskEngine.cs`, `VoiceActivationManager.cs`)**: Handles 100% local speech-to-text (Vosk) and neural phonetic wake-word detection.
- **Memory (`UserMemoryManager.cs`)**: Stores long-term facts about the user to personalize AI interactions.
- **Identity (`OAuth2Manager.cs`)**: Manages secure authentication flows for Google, Discord, Spotify, etc.
- **Settings (`SettingsManager.cs`)**: Handles persistence of system configuration.

### [Layer 1] - Background Services
Persistent processes that run independently of the UI.
- **Mobile Bridge (`MobileBridgeServer.cs`)**: A high-performance TCP/HTTP server that links your phone to your PC.
- **Environmental Analysis**: Monitors audio and system metrics in the background.

### [Layer 2] - UI Overlays & UX
Reusable visual components that appear as glassmorphic "windows" on the HUD.
- **BaseOverlay**: The foundation class for all HUD windows. Handles transparency, dragging, and animations.
- **ChatOverlay**: The primary interface for AI interaction.
- **DebugConsole**: Real-time diagnostic monitor with tiered verbosity levels.

### [Layer 3] - Command Dispatching
The logic layer that translates user intent (speech or text) into system actions.
- **CommandParser**: The central router that uses fuzzy matching and synonyms to find the right handler for a query.
- **Handlers**: Specialized modules (e.g., `GitPushCommandHandler`, `VolumeCommandHandler`) that execute specific system tasks.

### [Layer 4] - Main Environment
The top-level shell of the application.
- **MainWindow**: The search bar and HUD entry point triggered by global hotkeys.
- **LoadingWindow**: Manages the boot sequence and background initialization progress.

---

## File-Dependent Modular Execution
Heaplit is intentionally built as a **File-Dependent (Modular)** system. Unlike monolithic applications, the `HeaplitLauncher.exe` is a lightweight entry point that dynamically loads logic from the surrounding directory.

### Structural Requirements
- **DLL Dependencies**: All core libraries (Layer 0-3) are stored as external DLLs. 
- **Resource Coupling**: Themes, Scripts, and AI Instructions must reside in their respective folders (`Themes/`, `Scripts/`, `Data/Instructions/`) for the HUD to function correctly.
- **Development Flexibility**: This structure allows for real-time hot-swapping of modules and scripts without requiring a full application re-compile.

### Data Isolation
All user state and long-term memories are stored in the `Data/` folder. Deleting this folder will "reset" Heaplit's personality and understanding of the user.
