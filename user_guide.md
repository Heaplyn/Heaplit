# 🤖 HEAPLIT MASTER USER GUIDE

Welcome to the official manual for the Heaplit Windows HUD. Heaplit is a high-performance, AI-driven operating assistant designed for power users, developers, and those seeking an automated digital environment.

## 🚀 Getting Started

### The Main HUD
- **Trigger**: Press the **` (Backtick)** key to toggle the main command bar.
- **Search**: Start typing any command, application name, or math equation.
- **Autocomplete**: Press **Tab** to fill in the ghosted suggestion.
- **Execution**: Press **Enter** to run the selected action.

### AI Chat Assistant
- **Open**: Type `ai` or `chat` in the HUD, or press **Ctrl + Shift + A**.
- **Context**: Heaplit automatically knows what window you are looking at and what is on your clipboard.
- **Attachments**: Drag and drop files or folders into the chat to analyze them.
- **Thinking Trace**: Click **"⌄ Details"** on any response to see the raw AI logic.

---

## 🛠️ Key Feature Domains

### 1. Developer Power Tools
Heaplit is built for creators.
- **Git Control**: Type `push` to auto-commit and push your active project. Use `push ai` for AI-generated commit messages.
- **Code Assistance**: Use the **"Fix Code"** button in chat or type `code assist` to analyze your current project for bugs.
- **PowerShell HUD**: Type any PS command directly into the HUD (e.g., `ps Get-Process`).
- **IPA Compiler**: Type `ipa` to build iOS packages from C# projects.

### 2. Media & Automation
- **Media Download**: Type `download <url>` to grab high-quality audio or video from YouTube, SoundCloud, Spotify, and more.
- **Converter**: Use `convert` to open the media transformation studio.
- **Voice Studio**: Type `voice` to train your personal voiceprint for secure biometrics.
- **Phone Control**: Type `phone` or `remote` to manage your mobile companion. Use `phone vibrate` or `phone alert` for direct control.

### 3. ADHD & Productivity Suite
- **Pomodoro**: Type `timer 25` to start a focus sprint.
- **Sticky Notes**: Type `note` to create a persistent on-screen reminder.
- **File Organizer**: Type `organize` to automatically clean up your cluttered folders.
- **System Monitoring**: Type `process` to view active tasks or `network` for connectivity diagnostics.
- **Search Sync**: Type `reindex` to refresh your Windows application database instantly.

---

## 📶 Advanced Configuration

### LLM Engines
Heaplit supports multiple backends. Switch them in **Settings -> LLM**:
- **Gemini**: High-context, supports image/audio analysis.
- **Groq**: Ultra-fast inference for snappier voice replies.
- **Ollama**: 100% offline, local execution for total privacy.

### Shorthand Protocol
For faster communication, the AI uses a concise "@" protocol:
- `@rf{path}`: Read File
- `@wf{path}{content}`: Write File
- `@ps{cmd}`: Execute PowerShell
- `@snap`: Take Screenshot
- `@app{name}`: Launch Application

---

## 🏗️ Architecture: File-Dependent & Modular
Heaplit is designed to be **transparent and modular**. The `HeaplitLauncher.exe` you see in the folder is a dependent binary that relies on the surrounding files and directories to function.

- **Modules/**: Contains the core logic layers.
- **Themes/**: Stores visual glassmorphic styles.
- **Data/**: The persistent brain of Heaplit (Settings, Memory, Skills).
- **Macros/**: User-defined command chains.

**Note**: Do not move the `HeaplitLauncher.exe` out of its root folder, or it will lose access to its "Limbs and Memories."

---

## ⌨️ Global Hotkeys
- **`**: Toggle HUD
- **Ctrl+Shift+A**: AI Chat
- **Ctrl+Alt+M**: Mobile Hub
- **Ctrl+Shift+R**: Sync & Full Rebuild
- **Ctrl+Shift+C**: Emergency Exit
- **Esc**: Close Window
