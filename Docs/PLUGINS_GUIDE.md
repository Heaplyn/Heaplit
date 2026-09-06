# Heaplit C# Plugin & ML API Guide

Heaplit now supports native C# plugins, allowing you to extend the HUD with custom logic and high-level AI orchestration.

## 🚀 Getting Started

1. Create a new **C# Class Library (.NET 8.0)** project.
2. Reference the `HeaplitLauncher.dll` (found in the Heaplit root folder).
3. Implement the `IHeaplitPlugin` interface.
4. Drop your compiled `.dll` into the `/Plugins` folder in your Heaplit directory.

## 🧠 The Heaplit ML API

The `HeaplitMLApi` static class provides high-level methods for AI processing.

### Text & LLM
```csharp
// Ask a generic question to the active LLM
string result = await HeaplitMLApi.AskAiAsync("Explain quantum physics.");

// Summarize long text
string summary = await HeaplitMLApi.AskAiAsync(hugeContent, maxSentences: 2);
```

### Vision (Image Processing)
```csharp
// Analyze a local image
string description = await HeaplitMLApi.AnalyzeImageFileAsync("C:\\temp\\data.png", "What's in this image?");

// Analyze what the user is looking at right now
string screenInfo = await HeaplitMLApi.AnalyzeCurrentScreenAsync("Summarize this workspace.");
```

### Audio Processing
```csharp
// Multi-modal audio analysis via Gemini
string audioIntent = await HeaplitMLApi.AnalyzeAudioClipAsync("recording.wav", "Extract the emotional tone.");
```

## 🛠️ Example Plugin Implementation

```csharp
using System;
using System.Collections.Generic;
using HeaplitLauncher;

namespace MyCustomPlugin
{
    public class WorkspaceAnalyzerPlugin : IHeaplitPlugin
    {
        public string PluginName => "Workspace Analyzer";
        public string Description => "Uses AI to suggest workspace optimizations based on screen captures.";
        public string Author => "Dev";
        public Version Version => new Version(1, 0, 0);

        public void OnInitialize() 
        {
            Console.WriteLine("Workspace Analyzer Initialized.");
        }

        public void OnShutdown() { }

        public IEnumerable<CommandDesc> GetPluginCommands()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("analyze workspace", "Run AI vision audit on your current screen", "analyze workspace")
            };
        }
        
        // You can then hook this command into a custom handler or logic
    }
}
```

## 📂 Folder Structure
```text
Heaplit/
├── HeaplitLauncher.exe
├── HeaplitLauncher.dll (Core Library)
├── Plugins/
│   └── MyCustomPlugin.dll (Your Plugin)
└── Data/
    └── ...
```
