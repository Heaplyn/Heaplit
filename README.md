# 🤖 Heaplit Launcher & HUD Environment

Welcome to **Heaplit** — a high-fidelity, frosted-glass desktop AI companion, developer automation deck, and offline workspace orchestration suite designed to run locally on Windows. 

Heaplit is designed to act as an extension of the developer's thought process — providing a low-latency keyboard-driven HUD, dynamic media downloading, multi-engine AI chat routing, automated web scraping, and integrated reverse engineering decompilers.

---

## 🚀 Quick Start (Fast Launch)

The build and launch pipeline has been optimized to leverage .NET incremental compilation, reducing startup checks from **5+ minutes down to under 2 seconds**.

1. **Launch the HUD**:
   Double-click the **`HeaplitLauncher.bat`** (or **`run.bat`**) in the root directory. 
   - *This automatically checks for file modifications, compiles incrementally (typically taking less than 1 second), and launches the environment.*
2. **Development Compile**:
   To manually build the project check from your terminal:
   ```powershell
   dotnet build
   ```
3. **Hotkeys**:
   - Press **`Ctrl + Space`** (or your configured trigger) to show the Heaplit HUD launcher.
   - Press **`Escape`** to dismiss any active glassmorphic overlay window instantly.

---

## 🏗️ Architecture & Layer Hierarchy

Heaplit is structured as a strictly decoupled **4-Layer Dependency Hierarchy**. This prevents lateral coupling and eliminates circular dependency loops in C#.

```
[Layer 3: Routing/Handlers] 
          ↓ (Downward reference only)
[Layer 2: WPF Glassmorphic Overlays] 
          ↓ 
[Layer 1: Messaging Contracts & Bridges] 
          ↓ 
[Layer 0: Core Utilities & Engines]
```

### Layer Dependency Rules
- **Rule**: A module in **Layer K** can reference modules in **Layer J** if and only if **J < K**.
- **Isolation Constraint**: A class in Layer K **MUST NOT** reference or depend on another class in the same layer (J = K) or higher. This isolates modules cleanly.

### Folder Mapping
- 📂 **[`Modules/Layer0/`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer0/) (Core Utilities & Engines)**: Contains settings managers, audio processing pipelines, web scrapers, security engines, and local data files.
- 📂 **[`Modules/Layer1/`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer1/) (Bridges & Interfaces)**: Contains system interfaces, data transfer objects, and local SSE/TCP server bridges.
- 📂 **[`Modules/Layer2/`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer2/) (WPF UI Overlays)**: Glassmorphic user interface overlay windows constructed programmatically in C# (no XAML files).
- 📂 **[`Modules/Layer3/`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer3/) (Command Parsing & Handlers)**: Keyboard routing commands and suggestion providers.

---

## ⚡ Core Systems & Features

### 1. Multi-Engine AI Prober & Router (`LlmRouter.cs`)
Heaplit maintains a parallel diagnostic prober that benchmark-tests **17 concurrent AI inference endpoints** (Ollama, LM Studio, Gemini, Groq, Mistral, OpenRouter, Perplexity, DeepSeek, SambaNova, Cerebras, etc.) using `Task.WhenAll`. 
- **Auto-Failover**: If your preferred model fails or goes offline, the router instantly switches to the next lowest-latency active cloud or local model in the chain without interrupting your workflow.
- **Token Saver**: Automatically trims conversational history to fit prompt context windows efficiently.

### 2. Reflection-Based Dynamic Command Registry
- **Decoupled Commands**: All command handlers implement the `ICommandHandler` interface.
- **Zero-Config Extensions**: Instead of mapping commands to hardcoded enums, the `CommandParser` dynamically scans assembly types using reflection at boot time. Developers can drop in a new handler file under `Layer3/Handlers/` and it will be registered automatically without modifying a single core file.

### 3. Integrated Reverse Engineering Suite (`DisassemblerSuiteOverlay.cs`)
An overlay dedicated to reversing and decompilation. On first use, it automatically downloads and builds workspace tools under the `Data/ReversedTools` folder:
- **Ghidra**: Java-based headless PE/ELF analysis.
- **pycdc & pork**: Locally compiled C++ decompiler for Python `.pyc` bytecode.
- **Krakatau & javabytes**: Bytecode interpreters and decompilers for Java `.class` / `.jar` files.
- **jadx & APK Decompiler**: Android APK decompilers.
- **Features**: Includes hex viewing with patch diffing, symbol variable grouping, and assembly editing mode.

### 4. Advanced Web Scraping & Crawling (`WebScraperManager.cs`)
An automated web crawler and parser that extracts data from online resources:
- **Recursive Scraper**: Follows page structures recursively to extract relevant developer documentation.
- **Readability Parser**: Uses heuristics to extract main text from articles, stripping navigation headers, sidebars, and footer clutter.
- **API and Table Extractor**: Translates raw HTML table rows into clean, parseable JSON arrays.

### 5. Media Downloader (`DownloadMediaRunner.cs`)
Handles streaming downloads of audio and videos:
- **Emoji-safe pathing**: Process streams are configured with UTF-8 encoding to prevent Windows ANSI conversion from garbling folders containing emojis (e.g. `🎵 All Songs`).
- **Direct Save**: Supports saving files directly to custom destination directories without forced nested folder restructuring.

---

## ⚙️ Performance & Optimization Settings

WPF's frosted glass `BlurEffect` and background processes can be heavy on some systems. Heaplit includes optimization settings to run efficiently:

- **Low-VFX Performance Mode**: Can be enabled in **Settings -> Visual Options**. This disables GPU-heavy window blurs and replaces them with solid, semi-transparent overlays for ultra-low GPU consumption.
- **Parallel Boot Sequences**: The bootloader inside `App.xaml.cs` runs heavy engine initializations concurrently in separate threads. The HUD becomes active in milliseconds while larger indexes load gracefully in the background.
- **Vosk STT Deferral**: The heavy 40MB offline Vosk speech model is loaded **lazily** only when a speech recognition file command is run, freeing up ~100MB of startup memory.

---

## 🧑‍💻 Developer Guide & Coding Conventions

Everything below reflects how the code is actually wired today (root namespace `HeaplitLauncher`, WPF on .NET). Follow these when extending Heaplit.

### Adding a Command Handler (the extension point)

Handlers implement `ICommandHandler` ([`Modules/Layer1/Interfaces/ICommandHandler.cs`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer1/Interfaces/ICommandHandler.cs)):

```csharp
public interface ICommandHandler
{
    bool CanHandle(string Query);                    // fast prefix/keyword test — keep it allocation-free
    List<CommandResult> GetSuggestions(string Query); // build the HUD result rows
    void OnStart() { }                                // optional: warm caches at boot
    List<CommandDesc> GetCommandDescriptions() => new(); // optional: self-document for help/search
}
```

The `CommandParser` reflection-scans the assembly at boot and auto-registers every non-abstract type implementing this interface. To ship a new command:

1. Drop a class under [`Modules/Layer3/Handlers/`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer3/Handlers/) (grouped by domain: `AI`, `Dev`, `Media`, `Productivity`, `System`, `Utilities`).
2. Implement `CanHandle`/`GetSuggestions`. **Do not** register it anywhere — reflection handles that.
3. Keep `CanHandle` cheap: it runs on **every keystroke** across **all** handlers. Do heavy work lazily inside `GetSuggestions`, or defer it to `OnStart`.

> ⚠️ **Reflection registry caveat:** because handlers are discovered by type, a handler with a throwing constructor or a slow `OnStart` stalls the whole boot scan. Wrap risky init in try/catch and log rather than throw.

### Enforcing the Layer Hierarchy

The 4-layer rule (Layer K may reference only J < K) is a *convention*, not compiler-enforced — the whole app is one assembly. To keep it honest:

- **Never** add a `using` that points "upward" (e.g. a `Layer0` engine referencing a `Layer2` overlay). If a lower layer needs to notify an upper one, raise an **event** or push through a **Layer1 messaging contract/DTO** — don't call up directly.
- Cross-cutting data travels as DTOs in [`Modules/Layer1/DTOs/`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer1/DTOs/) (`ChatTurn`, `CommandResult`, `CommandDesc`). Add new shared shapes here, not inside a handler.

### WPF Threading & Glass Overlays (Layer2)

Overlays are built programmatically in C# (no XAML). Common footguns:

- **UI objects are thread-affine.** Any mutation of a `Window`/`Control` from a background task must marshal back via `Dispatcher.Invoke` / `await Dispatcher.InvokeAsync`. Touching UI off-thread throws `InvalidOperationException` intermittently.
- **Never block the UI thread.** No `.Result` / `.Wait()` on async calls in event handlers — it deadlocks the HUD. Use `async void` only for top-level event handlers; everything else returns `Task`.
- **`BlurEffect` is GPU-bound.** Respect the Low-VFX flag (Settings → Visual Options) in any new overlay so it degrades to a solid brush when blur is disabled. Dispose bitmaps/`RenderTargetBitmap` you allocate — they are unmanaged-backed and a frequent leak source.
- Prefer `Freeze()` on any `Brush`/`Geometry`/`ImageSource` shared across threads or reused across frames; frozen resources skip locking and reduce GC churn (the recent *"Fixed gc pressure with fuzzy searching"* commit is this class of fix).

### The Mobile Bridge (Layer1)

[`Modules/Layer1/Bridges/MobileBridgeServer.cs`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/Modules/Layer1/Bridges/MobileBridgeServer.cs) is the PC-side counterpart to the **Heaplit Mobile** app. It exposes a local JSON-over-HTTP surface (screen stream, remote mouse/keyboard, file explorer, EXEC/DEL) and is tunnelled out via **ngrok** for off-LAN access. When changing the wire format here, update `HeaplitBridgeClient.cs` in the mobile repo in lockstep — they are a contract pair. Server activity is traced to [`mobile_server_log.txt`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/mobile_server_log.txt).

### Async & Networking Patterns

- Every `HttpClient` should be **reused** (one long-lived instance), never `new`-per-call — per-call sockets exhaust the ephemeral port range under the LLM prober's parallel load.
- The 17-endpoint prober fans out with `Task.WhenAll` + per-task `CancellationToken` timeouts. Add new providers as isolated tasks; one slow endpoint must never stall the group — always give it a `CancellationTokenSource(timeout)`.
- Wrap external process launches (yt-dlp, Ghidra, decompilers) with **UTF-8 stream encoding** on `ProcessStartInfo` (`StandardOutputEncoding = Encoding.UTF8`) to preserve emoji/Unicode paths — see the media downloader notes above.

### Build & Debug

```powershell
dotnet build                 # incremental compile (sub-second once warm)
```

- Launch via `HeaplitLauncher.bat` / `run.bat` (incremental compile + launch). `run_silent.vbs` starts with no console window.
- Runtime diagnostics land in [`heaplit_debug.log`](file:///c:/Users/Kyle/Downloads/Projects/Heaplit/heaplit_debug.log); the mobile server has its own log.
- The repo ships the .NET runtime/WPF DLLs alongside the launcher (self-contained-style layout), so a machine without the matching SDK can still run the built output.
