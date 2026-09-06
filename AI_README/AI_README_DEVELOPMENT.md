# HEAPLIT DEVELOPMENT & TROUBLESHOOTING GUIDE

## HOW TO ADD NEW THINGS

### 1. Adding a Backend Manager (Layer 0)
- **Location**: `Modules/Layer0/`
- **Pattern**: Implement operations as self-contained state-free or thread-safe singleton managers.
- **Rules**: Do not reference WPF controls or windows (Layer 2) from Layer 0. Use events or async task completion values to bubble results up.

### 2. Adding an Overlay Dashboard (Layer 2)
- **Location**: `Modules/Layer2/`
- **Pattern**: Inherit from `BaseOverlay` (`Modules/Layer2/BaseOverlay.cs`).
- **Initialization**: Programmatically build grid structures, toolbars, and scroll views. Bind the parent element to `this.UserContent`.
- **Display Hook**: Implement thread-safe WPF dispatcher singleton wrapper:
  ```csharp
  public static void ShowOverlay() {
      Application.Current.Dispatcher.Invoke(() => {
          // Singleton window instantiation and display
      });
  }
  ```

### 3. Adding a Launcher Command (Layer 3)
- **Location**: `Modules/Layer3/Handlers/`
- **Pattern**: Implement `ICommandHandler`.
- **Registration**: Register the handler under static constructor `CommandParser.cs` and map to Category.

---

## DECISION MATRIX: WHAT TO USE FOR WHAT
- **State/Logic vs. UI**: Keep heavy calculations, parsing, and data harvesting in Layer 0. Keep Wpf window structure, list display logic, and saving loops in Layer 2.
- **Fuzzy Search**: Use `SearchUtil.IsClose(query, trigger)` for matching, and `SearchUtil.GetSimilarity(query, trigger)` for ranking. Boost high-priority commands to `5.0` to override default system handlers.
- **Assembly Decoding**: Use `Assembly.Load(byte[])` to load binaries in memory. Never load files from path directly (causes file locks during compile or run loops).
- **Process Executions**: Use background Task threads `Task.Run()` for shell commands (`objdump`, `dumpbin`) to avoid freezing the main UI thread.

---

## COMMON BUG FIXES & TROUBLESHOOTING

### 1. WPF Threading Violations
- **Symptom**: `InvalidOperationException: The calling thread cannot access this object because a different thread owns it.`
- **Fix**: Wrap UI updates or window creation in the Dispatcher:
  ```csharp
  Application.Current.Dispatcher.Invoke(() => {
      _myTextBox.Text = resultText;
  });
  ```

### 2. File Lock Failures
- **Symptom**: `UnauthorizedAccessException / IOException: The process cannot access the file ... because it is being used by another process.`
- **Fix**: Load bytes into memory first using `File.ReadAllBytes()`, then parse the byte array.

### 3. String Interpolation Syntax Errors
- **Symptom**: `CS8076: Missing close delimiter` or `CS8361: A conditional expression cannot be used directly`
- **Fix**: Wrap ternaries inside interpolated strings in explicit parentheses inside the braces:
  `$"Value: {((condition) ? "A" : "B")}"`
- **Fix**: Escape braces (`{{` and `}}`) carefully when generating code strings to prevent parsing clashes:
  `$"{{ {value} }}"`

### 4. Unresolved Metadata Tokens
- **Symptom**: Decompiler crashes on resolving external types/methods.
- **Fix**: Wrap metadata resolutions (`ResolveMethod`, `ResolveType`, `ResolveString`) in `try-catch` blocks and fall back to displaying the raw hex token value (e.g. `[MethodToken: 0x060000A1]`).

---

## HEAPLIT DEVELOPER & AI WISDOM MANUAL (CRITICAL DIRECTIONS FOR FUTURE DEVELOPERS/AGENTS)

### 1. Initial Diagnostics & Scan Checklist (Run EVERY Turn)
1. **Verify Compile State**: Run `dotnet build` immediately before making changes to ensure the workspace starts in a clean, working state.
2. **Find Existing Managers**: Before writing new features, search `Modules/Layer0/` to see if a similar manager exists. Reuse existing APIs (like `PathHandler.GetDataDirectory()`) rather than inventing custom directory strings.
3. **Command Hook Discovery**: Look at `Modules/Layer3/CommandParser.cs` to understand command string patterns and map your new handlers to appropriate categories.

### 2. Strict Architectural Integrity
- **Downward Dependency Flow**: Layer 3 (Command Routing) $\rightarrow$ Layer 2 (UI Overlays) $\rightarrow$ Layer 1 (Core Interfaces) $\rightarrow$ Layer 0 (Core Managers).
- **Zero Lateral Coupling**: Sibling modules in the same layer **MUST NOT** reference each other directly.
- **UI Isolation**: Never import `System.Windows` or use UI classes (Windows, controls, brushes) inside Layer 0 or Layer 1 files. Keep them purely logical.

### 3. WPF Asynchronous Best Practices
- **UI Thread Safety**: Never block the UI thread. Use `Task.Run()` for complex computations, compilation pipelines, or Web Pull requests.
- **Dispatcher Invocation**: Always use `Application.Current.Dispatcher.Invoke()` or `InvokeAsync()` when updating progress texts, logs, or displaying dialog windows from background tasks.

### 4. Resolving Lock File Collisions
- If building the project fails because `HeaplitLauncher.exe` is locked, it means the app is running in the background. Kill it using the CLI:
  `Stop-Process -Name HeaplitLauncher -Force`

---

## NUGET & DEPENDENCY MANAGEMENT

### How NuGet Packages Work in Heaplit
All third-party libraries are declared in `HeaplitLauncher.csproj` as `<PackageReference>` entries. They are **automatically restored and bundled** into the output EXE on every `dotnet build` — no manual installation step is needed.

To add a new package:
```powershell
dotnet add package <PackageName>
```

This updates `HeaplitLauncher.csproj` immediately. The package will be available in all subsequent builds.

### Current NuGet Dependencies of Note
| Package | Purpose |
|---------|---------|
| `SharpCompress` | Multi-format archive extraction (RAR, 7z, tar, gz, bz2) |
| `WpfAnimatedGif` | Animated GIF playback in WPF `Image` elements |
| `Microsoft.CognitiveServices.Speech` | Azure Speech SDK for voice activation |
| `NAudio` | Audio capture and playback |

> [!IMPORTANT]
> If you add a new NuGet package, always document it in this table so future agents know what's available without having to grep the `.csproj`.

---

## VISUAL SYSTEM — CRITICAL ORDERING RULE

When modifying `ThemeManager.ApplyVisualOverrides()`, always follow this order:

1. **Resolve font family** (from path or family name setting).
2. **Write gradient/color brushes to `Application.Current.Resources`** (`TextPrimaryBrush`, `TextSecondaryBrush`, etc.).
3. **Call `UpdateImplicitStyles(fontFamily)`** — this reads the resource brushes to build implicit `Foreground` setters. If you call it before step 2, it snapshots the old brush.

Violating this order silently causes gradients or color resets to have no visible effect.

---

## WPF EXTERNAL FONT LOADING

When a `.ttf` or `.otf` file path is provided via `CUSTOM_FONT_PATH`:
1. Use `Fonts.GetFontFamilies(folderUri)` to discover family names in the font file.
2. Build: `new FontFamily(new Uri("file:///C:/FolderPath/"), "./FileName.otf#FamilyName")`
3. Register to **both** `"GlobalFontFamily"` and `"ActiveFontFamily"` resources.
4. Call `UpdateImplicitStyles(fontFamily)` so implicit styles on `Window`, `Control`, and `TextBlock` pick up the new font system-wide.

> [!WARNING]
> `new FontFamily(absoluteFilePath)` does **not** work for external files. You must split the path into folder URI + `"./filename#FamilyName"` format. This is a WPF quirk with no workaround.

---

## SESSION LOG REMINDER (FOR ALL AI AGENTS)

> [!IMPORTANT]
> After completing a work session or making significant changes, **write a session log to `walkthrough.md`** in the artifacts directory. This allows the next AI agent (or future session) to understand what was changed and why, without reading the entire codebase.
>
> The log should include:
> - Files modified and what changed
> - Any new NuGet packages added
> - Any architectural decisions made
> - Known issues or next steps
>
> Log path: `C:\Users\Kyle\.gemini\antigravity\brain\<conversation-id>\walkthrough.md`

