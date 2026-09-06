# Heaplit Scripting & Automation Guide

Heaplit is designed to be a programmable environment. This guide explains how to use action chains, macros, and the AI shorthand protocol to automate your workflow.

## 1. Command Chaining
You can run multiple Heaplit commands in a single line using the pipe `|` or sequence `&&` operators.

- **Pipeline (`|`)**: Runs commands simultaneously or in very rapid succession.
  - *Example*: `sysinfo | screenshot` (Shows specs and takes a capture).
- **Sequence (`&&`)**: Runs commands one after the other.
  - *Example*: `lock && timer 5` (Locks PC then sets a 5min alarm for your return).

## 2. Macros
Macros allow you to save long chains of commands into a single shortcut word.

### Creating a Macro
Use the command: `macro add <name> -> <command1> | <command2>`
- *Example*: `macro add startup -> open chrome | open vscode | volume 20`

### Running a Macro
Simply type the name of the macro in the HUD.
- *Example*: `startup`

Macros are stored as `.txt` files in the `Macros/` folder. You can edit them manually to add complex logic.

## 3. AI Shorthand Protocol (@)
When interacting with the AI, it uses a high-speed "Concise Protocol" to act. You can also use these in your own prompts to Heaplit to be specific about what you want him to do.

- **@rf{path}**: Read file contents into the AI context.
- **@wf{path}{content}**: Write or create a file.
- **@ps{cmd}**: Run a silent PowerShell script.
- **@app{name}**: Find and launch a Windows application.
- **@run{cmd}**: Execute a standard Heaplit HUD command.
- **@snap**: Capture the current screen for AI vision analysis.

## 4. Custom Data Processors (@proc)
For advanced users, you can link your own Python or C++ programs to Heaplit.
- **Tag**: `@proc{input}`
- **Setup**: Define your binary path in **Settings -> LLM**.
- Heaplit will pass the input as a CLI argument and read your program's output.

---

## 💡 Practical Examples
- **Research**: *"Ingest the docs at [URL] and save a summary to my Obsidian vault at @wf{Notes/Summary.md}"*
- **Dev Cycle**: *"Build my C# project and if it succeeds, @run{push ai}"*
- **Security**: *"Check my active processes and @run{kill} anything suspicious."*
