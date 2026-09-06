# HEAPLIT CODING FUNDAMENTALS, TESTING & TOKEN CHEATSHEET

## CODING FUNDAMENTALS (C# & WPF)

### 1. SOLID Principles in Heaplit
- **Single Responsibility (SRP)**: Sibling modules in Layer 0 must perform exactly one function (e.g. only audio extraction or only scraping). Never mix UI with backend operations.
- **Open/Closed (OCP)**: Command Parser relies on registering command handlers. To add features, write new handlers—never modify the core parser routing loop.
- **Interface Segregation (ISP)**: Implement minimal interfaces (like `IAiTool` or `ICommandHandler`).

### 2. Threading Rules
- **UI Thread (WPF)**: Controls (TextBox, TreeView) can only be accessed or modified on the Main UI thread.
- **Background Thread**: I/O, web requests, and parsing must run on background threads (`Task.Run`) to keep the UI smooth and responsive.

---

## UNIT TESTING & BACKGROUND RUNNER (`run.bat`)

### 1. Writing Unit Tests
All unit tests are located under the test projects folder.
- Follow the Arrange-Act-Assert structure.
- Mock external resources (like network and system binaries) using local data mocks.

### 2. Running Tests via `run.bat`
`run.bat` is a background scripting utility that automates compile and test iterations:
- Execute `run.bat` in the background. It initiates compilation (`dotnet build`) followed by test runs (`dotnet test`).
- Test telemetry is output to the local log buffer, preventing active file locks during runtime testing.

---

## TOKEN-SAVING ABBREVIATION REGISTER (LLM REFERENCER)
Future agents can reference these abbreviations to reduce token consumption during action plans:

| Abbreviation | Expanded C# / Action Procedure |
| :--- | :--- |
| `UI_RUN(act)` | `Application.Current.Dispatcher.Invoke(() => { act });` |
| `BG_RUN(act)` | `Task.Run(async () => { act });` |
| `LBL(t, s, b)` | `var lbl = CreateLabel(t, s, b);` |
| `TXT()` | `var txt = CreateTextBox();` |
| `BTN(t, c, p)` | `var btn = CreateStyledButton(t, c, p, 10);` |
| `LOG()` | Monospace TextBox Console configuration block. |
| `RD_BY(p)` | `File.ReadAllBytes(p);` (memory loading to prevent locks) |
| `WR_TX(p, c)` | `File.WriteAllText(p, c);` |
| `IS_CLOSE(a, b)` | `SearchUtil.IsClose(a, b)` |
| `GET_SIM(a, b)` | `SearchUtil.GetSimilarity(a, b)` |
| `TEST_RUN()` | Spawns `run.bat` background test compilation. |
