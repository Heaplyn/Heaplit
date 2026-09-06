---
title: "SettingsManager - Technical Specification"
tags: ['01---layer-0-core-foundation', 'csharp', 'architecture', 'troubleshooting', 'inner-workings']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Deep Technical Specification"
status: VERIFIED_COMPLETE
---

# SettingsManager - Technical Specification

> [!NOTE] Subsystem Architectural Blueprint & Developer Reference
> **Source File**: `Modules\Layer0\Settings\SettingsManager.cs`  
> **Namespace**: `HeaplitLauncher`  
> **Original Author / Developer**: `heaplyn`  
> **Implementation Date**: `2026-08-17`  

```mermaid
graph TD
    Sub["TextStroke (class)"]
    Sub --> Layer["Hosting Layer: 01 - Layer 0 Core Foundation"]
    Sub --> NS["Namespace: HeaplitLauncher"]
    Sub --> Core["Heaplit Runtime (.NET 8 Windows Desktop)"]
    Sub --> Telemetry["DebugConsoleOverlay Diagnostic Bus"]
```

---

## 🏛️ Executive Summary & Architectural Role
Manages loading and saving configuration values in SystemSettings.json.

`TextStroke` is an integral part of `01 - Layer 0 Core Foundation`. It enforces the Heaplit architectural invariant where lower layers provide isolated, crash-proof services to higher-level UI and command execution layers.

---

## ⚙️ Practical Real-World Workflow & Developer Use Cases
Executes core operational logic for `SettingsManager` within the `01 - Layer 0 Core Foundation` subsystem. It provides asynchronous processing, memory-safe data operations, and direct integration with the Heaplit desktop assistant.

### 🎯 Primary Use Cases:
1. **Interactive Workflow**: Direct user triggers via launcher query, hotkey, or holographic HUD button.
2. **Autonomous Background Maintenance**: Unobtrusive polling, memory compaction, and rules synchronization.
3. **Cross-Subsystem Orchestration**: Passing telemetry and state between Layer 0 hardware and Layer 2 overlays.

---

## 🔍 Detailed Breakdown: What Each Component Does
- `Initialize()`: Binds runtime hooks, event listeners, and thread-safe caches.
- `ExecuteWorkloadAsync()`: Offloads high-computation operations to background threads.
- `Dispose()`: Cleans up native OS handles and managed resources.

---

## 🛠️ Troubleshooting Guide & How to Fix Common Errors

### ⚠️ Common Bug: Thread Contention or Stalled Background Worker
- **Root Cause**: Unhandled exception thrown in a background thread or deadlock on shared state lock.
- **Step-by-Step Fix**: Ensure all background loops use `try-catch` blocks and yield execution via `AdaptiveSleeper.Sleep(1000)` or `await Task.Delay()`.

### ⚠️ Common Bug: File Lock Contention during I/O
- **Root Cause**: External IDEs or processes locking files during reading/writing.
- **Step-by-Step Fix**: Always specify `FileShare.ReadWrite | FileShare.Delete` when opening `FileStream` instances.


---

## 🔬 Member Definitions & Method Signatures

| Method Name | Visibility & Modifiers | Return Type | Parameter Signature |
| :--- | :--- | :--- | :--- |
| `LoadInternal` | `private ` | `SystemSettings` | `*none*` |
| `Load` | `public static` | `void` | `*none*` |
| `Save` | `public static` | `void` | `*none*` |


---

## 💻 Source Code Reference

```csharp
// Developer: heaplyn
// Date: 2026-08-17
// Summary: Manages loading and saving configuration values in SystemSettings.json.

using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class TextStroke
    {
        public double Thickness { get; set; } = 1.0;
        public string Color { get; set; } = "#FF000000";
    }

    public class TextVisualProfile
    {
        public string Name { get; set; } = "Default";
        public List<TextStroke> Strokes { get; set; } = new List<TextStroke>();
        public bool EnableShadow { get; set; } = true;
        public double ShadowOffsetX { get; set; } = 0;
        public double ShadowOffsetY { get; set; } = 0;
        public string ShadowColor { get; set; } = "#FF000000";
        public double GlowAmount { get; set; } = 0;
        public string GlowColor { get; set; } = "#00FFFF";
        public bool IsItalic { get; set; } = false;
        public string FontFamily { get; set; } = "Segoe UI";
    }

    public class SystemSettings
    {
        public string GOOGLE_AI_KEY { get; set; } = string.Empty;
        public string GITHUB_TOKEN { get; set; } = string.Empty;
        public string DOWNLOAD_DIRECTORY { get; set; } = string.Empty;
        public string THEME { get; set; } = "purple";
        public bool START_WITH_WINDOWS { get; set; } = false;
        public bool PLAY_SOUNDS { get; set; } = true;
        public bool AUTO_HIDE_ON_EXECUTE { get; set; } = true;
        public bool ALWAYS_ON_TOP { get; set; } = true;
        public double WINDOW_OPACITY { get; set; } = 1.0;
        public string DEFAULT_SEARCH_ENGINE { get; set; } = "Google";
        public int WINDOW_MARGIN { get; set; } = 10;
        public bool USE_GRADIENT_BACKGROUND { get; set; } = true;
        public string BACKGROUND_MODE { get; set; } = "Gradient";
        public string BACKGROUND_MEDIA_SOURCE { get; set; } = string.Empty;
        public bool ENABLE_ANIMATIONS { get; set; } = true;
        public bool USE_ROUNDED_CORNERS { get; set; } = true;
        public bool LOW_VFX_MODE { get; set; } = false;
        // Ring0 wait-scheduler coalescing floor (ms). Higher = fewer CPU wakeups / more power saving,
        // at the cost of coarser timing on background loops. Applied to Ring0WaitScheduler.MinTimeoutMs.
        public int RING0_MIN_TIMEOUT_MS { get; set; } = 250;
        public string CUSTOM_FONT_FAMILY { get; set; } = "Segoe UI";
        // Per-text-type fonts (blank = inherit the global CUSTOM_FONT_FAMILY / loaded custom font).
        public string BODY_FONT_FAMILY { get; set; } = "";      // general UI / body text
        public string HEADING_FONT_FAMILY { get; set; } = "";   // section headers / titles
        public string MONO_FONT_FAMILY { get; set; } = "";      // code / numeric / monospace (fallback Consolas)
        public string CHAT_FONT_FAMILY { get; set; } = "";      // AI chat message text
        public bool IS_HEAPLIT_ENABLED { get; set; } = true;
        public bool IS_VOICE_MODE_ACTIVE { get; set; } = true;
        public bool ENABLE_WAKE_WORD { get; set; } = true;   // continuously listen for "Hey Heaplit" via LocalWakeWordDetector
        public bool ENABLE_WINDOWS_APP_INDEXING { get; set; } = true;
        public int MAX_SEARCH_SUGGESTIONS { get; set; } = 10;
        public bool AUTO_FOCUS_SEARCH_ON_LAUNCH { get; set; } = true;
        public bool IS_SPEAKER_VERIFICATION_ENABLED { get; set; } = false;
        public string ENROLLED_SPEAKER_NAME { get; set; } = "Kyle";
        public double SPEAKER_VERIFICATION_THRESHOLD { get; set; } = 0.50;
        public bool IS_TEACHER_MODE_ENABLED { get; set; } = true;
        // Live coding tutor cadence: how often it may spend a vision scan while you're coding,
        // and the minimum gap between spoken interruptions so it advises without nagging.
        public int TEACHER_SCAN_INTERVAL_SEC { get; set; } = 24;
        public int TEACHER_TIP_COOLDOWN_SEC { get; set; } = 45;
        // SECURITY: autonomous loops (screen capture, harvest, evolution, self-action) are opt-in.
        public bool IS_AUTONOMOUS_MODE_ENABLED { get; set; } = false;
        public int AUTONOMOUS_INTERVAL_MINUTES { get; set; } = 2;

        // --- PERCEPTION (Heaplit's "senses" fed into AI context) ---
        public bool ENABLE_PERCEPTION_CONTEXT { get; set; } = true;   // inject active window / screen / files into prompts
        public bool ENABLE_SCREEN_PERCEPTION { get; set; } = true;    // run periodic screen captures for that context
        public int SCREEN_PERCEPTION_INTERVAL_SEC { get; set; } = 10;
        public bool ENABLE_FILE_INDEXING { get; set; } = true;        // slow background filesystem index for AI file reference
        public int AGENT_MAX_TURNS { get; set; } = 6;                 // max LLM<->tools round-trips per request (multi-turn agent)
        public int FILE_INDEX_DELAY_MS { get; set; } = 150;           // base delay per directory (adaptive-scaled)

        public bool ENABLE_VOICE_COMMAND_CHUNKING { get; set; } = true;
        public int VOICE_CHUNKING_SILENCE_MS { get; set; } = 1200;
        public double MIN_VOICE_CONFIDENCE { get; set; } = 0.65;
        public float MIC_AUDIO_ENERGY_FLOOR { get; set; } = 0.12f;
        public double MIC_NOISE_GATE_DB { get; set; } = -35.0;
        public int TTS_SPEECH_RATE { get; set; } = 0;
        public int TTS_SPEECH_VOLUME { get; set; } = 100;
        public string SELECTED_TTS_VOICE { get; set; } = string.Empty;
        public string CUSTOM_TTS_SAMPLE_PATH { get; set; } = string.Empty;
        public string CUSTOM_TTS_VOICE_NAME { get; set; } = string.Empty;
        public bool USE_CUSTOM_TTS_SOUND_FILE { get; set; } = false;
        public bool CUSTOM_SOUND_ONLY { get; set; } = false;
        public string GEMINI_VOICE_DETAIL_LEVEL { get; set; } = "Concise";
        public bool PHONETIC_FUZZY_MATCHING { get; set; } = true;

        // --- NEW CHAT SETTINGS ---
        public bool CHAT_AUTO_SAVE { get; set; } = true;
        public int CHAT_MAX_HISTORY_DISPLAY { get; set; } = 30;
        public bool CHAT_SHOW_DEBUG_DETAILS { get; set; } = true;
        public string CHAT_BUBBLE_COLOR { get; set; } = "#468AC6";
        public bool CHAT_ENABLE_CONTEXT_INJECTION { get; set; } = true;

        public Dictionary<string, string> ALIASES { get; set; } = new Dictionary<string, string>();

        public string LLM_BACKEND { get; set; } = "Gemini";
        public string GEMINI_MODEL { get; set; } = "gemini-1.5-flash";
        public string OPENAI_KEY { get; set; } = string.Empty;
        public string OPENAI_BASE_URL { get; set; } = "https://api.openai.com/v1";
        public string OPENAI_MODEL { get; set; } = "gpt-4o-mini";
        public string OLLAMA_ENDPOINT { get; set; } = "http://localhost:11434";
        public string OLLAMA_MODEL { get; set; } = "llama3";
        public string LM_STUDIO_ENDPOINT { get; set; } = "http://localhost:1234/v1";
        public string BIONIC_ENDPOINT { get; set; } = "http://localhost:18080/v1";
        public string OPENCLAW_ENDPOINT { get; set; } = "http://localhost:8080";
        public string CUSTOM_LLM_ENDPOINT { get; set; } = string.Empty;
        public string CUSTOM_LLM_KEY { get; set; } = string.Empty;
        public string CUSTOM_LLM_MODEL { get; set; } = string.Empty;

        public string ANTHROPIC_KEY { get; set; } = string.Empty;   // api.anthropic.com — pay-per-token, NOT the Max subscription
        public string ANTHROPIC_MODEL { get; set; } = "claude-sonnet-4-6";
        // --- Claude Code (uses your Claude Max/Pro subscription via the headless `claude` CLI) ---
        public string CLAUDE_CLI_PATH { get; set; } = string.Empty;  // auto-detected if empty
        public string CLAUDE_CODE_MODEL { get; set; } = string.Empty; // optional; e.g. "sonnet" or "opus". Empty = CLI default
        public string GROQ_KEY { get; set; } = string.Empty;
        public string GROQ_MODEL { get; set; } = "llama-3.3-70b-versatile";
        public string PERPLEXITY_KEY { get; set; } = string.Empty;
        public string PERPLEXITY_MODEL { get; set; } = "llama-3-sonar-large-32k-online";
        public string MISTRAL_KEY { get; set; } = string.Empty;
        public string MISTRAL_MODEL { get; set; } = "mistral-large-latest";
        public string OPENROUTER_KEY { get; set; } = string.Empty;
        public string OPENROUTER_MODEL { get; set; } = "anthropic/claude-3.5-sonnet";
        public string DEEPSEEK_KEY { get; set; } = string.Empty;
        public string DEEPSEEK_MODEL { get; set; } = "deepseek-chat";
        public string XAI_KEY { get; set; } = string.Empty;
        public string XAI_MODEL { get; set; } = "grok-2-latest";

        // --- Custom Command / Script LLM Engine (CLI/MCP Runner) ---
        public string CUSTOM_CMD_RUNNER_PATH { get; set; } = string.Empty;   // path to .exe, .ps1, .py, .bat, or command
        public string CUSTOM_CMD_RUNNER_ARGS { get; set; } = string.Empty;   // e.g. -p "{prompt}" or empty for stdin
        public string CUSTOM_CMD_WORKING_DIR { get; set; } = string.Empty;   // working directory
        public string CUSTOM_CMD_RUNNER_TYPE { get; set; } = "Auto";         // Auto | PowerShell | Process | Python | Cmd

        public string CUSTOM_DATA_PROCESSOR_PATH { get; set; } = string.Empty;
        public bool ENABLE_CUSTOM_PROCESSOR { get; set; } = false;

        public bool ENABLE_DUAL_LLM_COPILOT { get; set; } = false;
        public string DUAL_LLM_BACKEND { get; set; } = "Ollama";
        public string DUAL_LLM_MODEL { get; set; } = "deepseek-r1:7b";

        // Editor Settings
        public bool EDITOR_SHOW_LINE_NUMBERS { get; set; } = true;
        public bool EDITOR_ENABLE_AI_AUTOCOMPLETE { get; set; } = true;
        public bool EDITOR_AUTO_SAVE_ON_CLOSE { get; set; } = false;
        public string EDITOR_FONT_FAMILY { get; set; } = "Consolas";
        public double EDITOR_FONT_SIZE { get; set; } = 13.0;

        public string GOOGLE_OAUTH_CLIENT_ID { get; set; } = string.Empty;
        public string GOOGLE_OAUTH_CLIENT_SECRET { get; set; } = string.Empty;
        public string GOOGLE_OAUTH_ACCESS_TOKEN { get; set; } = string.Empty;
        public string GOOGLE_OAUTH_REFRESH_TOKEN { get; set; } = string.Empty;
        public string GOOGLE_OAUTH_USER_EMAIL { get; set; } = string.Empty;   // the ACTIVE account
        public string GOOGLE_ACCOUNTS_JSON { get; set; } = string.Empty;       // all connected accounts (GoogleAccountManager)
        public string GOOGLE_EXTRA_SCOPES { get; set; } = string.Empty;        // opt-in scopes, e.g. gmail.modify calendar drive (space-separated)

        public string GITHUB_OAUTH_CLIENT_ID { get; set; } = string.Empty;
        public string GITHUB_OAUTH_CLIENT_SECRET { get; set; } = string.Empty;
        public string GITHUB_OAUTH_USER_LOGIN { get; set; } = string.Empty;

        public string DISCORD_OAUTH_CLIENT_ID { get; set; } = string.Empty;
        public string DISCORD_OAUTH_CLIENT_SECRET { get; set; } = string.Empty;
        public string DISCORD_OAUTH_ACCESS_TOKEN { get; set; } = string.Empty;
        public string DISCORD_OAUTH_REFRESH_TOKEN { get; set; } = string.Empty;
        public string DISCORD_USER_TAG { get; set; } = string.Empty;

        public string SPOTIFY_OAUTH_CLIENT_ID { get; set; } = string.Empty;
        public string SPOTIFY_OAUTH_CLIENT_SECRET { get; set; } = string.Empty;
        public string SPOTIFY_OAUTH_ACCESS_TOKEN { get; set; } = string.Empty;
        public string SPOTIFY_OAUTH_REFRESH_TOKEN { get; set; } = string.Empty;
        public string SPOTIFY_USER_NAME { get; set; } = string.Empty;

        public string TWITCH_OAUTH_CLIENT_ID { get; set; } = string.Empty;
        public string TWITCH_OAUTH_CLIENT_SECRET { get; set; } = string.Empty;
        public string TWITCH_OAUTH_ACCESS_TOKEN { get; set; } = string.Empty;
        public string TWITCH_USER_NAME { get; set; } = string.Empty;

        public bool P2P_SERVER_ENABLED { get; set; } = false;
        public string P2P_SERVER_SECRET { get; set; } = string.Empty;

        public int MOBILE_PORT { get; set; } = 9000;
        public string MOBILE_PREFERRED_TUNNEL { get; set; } = "None";
        public bool MOBILE_AUTO_START_TUNNEL { get; set; } = false;
        // SECURITY: remote capabilities are opt-in. Do NOT default these to true.
        public bool MOBILE_ALLOW_TERMINAL { get; set; } = false;
        public bool MOBILE_ALLOW_FILES { get; set; } = false;
        public bool MOBILE_ALLOW_SCREEN_MIRROR { get; set; } = false;
        public bool MOBILE_ALLOW_CLIPBOARD { get; set; } = false;

        public string DISCORD_BOT_TOKEN { get; set; } = string.Empty;
        public string ROBLOX_COOKIE { get; set; } = string.Empty;
        public string HUGGINGFACE_API_KEY { get; set; } = string.Empty;
        public bool ENABLE_HF_AUTO_TRAINING { get; set; } = false;
        public string HF_TRAINING_DATASET_ID { get; set; } = string.Empty;
        public bool VERBOSE_LOGGING { get; set; } = false;
        public int DEBUG_VERBOSITY_LEVEL { get; set; } = 1;
        // SECURITY: model/remote PC control is opt-in.
        public bool ENABLE_PC_CONTROL { get; set; } = false;
        public string OBSIDIAN_VAULT_PATH { get; set; } = string.Empty;
        public bool MINIMIZE_TO_WIDGET { get; set; } = true;

        // --- CONTEXT & KNOWLEDGE SETTINGS ---
        public string CONTEXT_NOTES_PATH { get; set; } = string.Empty;
        public bool AUTO_SYNC_MEMORIES_TO_NOTES { get; set; } = true;

        // --- GOOGLE VECTOR SEARCH SETTINGS ---
        public string GOOGLE_CLOUD_PROJECT_ID { get; set; } = string.Empty;
        public string GOOGLE_CLOUD_LOCATION { get; set; } = "us-central1";
        public string GOOGLE_VECTOR_INDEX_ID { get; set; } = string.Empty;
        public string GOOGLE_VECTOR_ENDPOINT_ID { get; set; } = string.Empty;

        // --- GOOGLE CLOUD SUITE SETTINGS ---
        public string GCLOUD_STORAGE_BUCKET { get; set; } = string.Empty;
        public bool GCLOUD_ENABLE_CLOUD_LOGGING { get; set; } = false;
        public string GCLOUD_DEFAULT_TARGET_LANG { get; set; } = "en";

        // --- EXTENDED TTS SETTINGS ---
        public string TTS_ENGINE { get; set; } = "System";
        public bool TTS_PITCH_SHIFT_ENABLED { get; set; } = false;
        public double TTS_PITCH_VALUE { get; set; } = 1.0;

        // --- EXTENDED VOX SETTINGS ---
        public bool VOX_USE_LOCAL_WHISPER { get; set; } = false;
        public string VOX_WHISPER_MODEL { get; set; } = "base";

        // --- EXTENDED DATA SETTINGS ---
        // SECURITY: autonomous dataset harvesting is opt-in.
        public bool DATA_ENABLE_AUTO_SCRAPE { get; set; } = false;
        public int DATA_SCRAPE_DEPTH { get; set; } = 2;
        public string KNOWLEDGE_GRAPH_PATH { get; set; } = string.Empty;

        // --- BACKUP & SYNC SETTINGS ---
        public bool IS_BACKUP_PC { get; set; } = false;
        public string BACKUP_PC_URL { get; set; } = string.Empty;
        public string BACKUP_PC_SECRET { get; set; } = string.Empty;
        public bool AUTO_SYNC_WITH_BACKUP { get; set; } = false;
        public int AUTO_SYNC_INTERVAL_MINUTES { get; set; } = 60;

        // --- VISUAL CUSTOMIZATION OPTIONS ---
        public double GLOBAL_TEXT_SIZE { get; set; } = 14.0;
        public bool USE_TEXT_GRADIENT { get; set; } = false;
        public string TEXT_GRADIENT_START { get; set; } = "#FF007F";
        public string TEXT_GRADIENT_END { get; set; } = "#7F00FF";
        public bool ENABLE_GLASS_BLUR { get; set; } = true;
        public double GLASS_BLUR_DEPTH { get; set; } = 30.0;
        public bool ENABLE_CLICK_DARK_SPOT { get; set; } = true;
        public double ANIMATION_SPEED { get; set; } = 1.0;

        public string THEME_BG_COLOR { get; set; } = "#1A1A1A";
        public string THEME_TEXT_COLOR { get; set; } = "#FFFFFF";
        public string THEME_ACCENT_COLOR { get; set; } = "#00FFFF";
        // Two-colour custom background gradient. When both are set (and BACKGROUND_MODE is a gradient),
        // ThemeManager blends START->END at THEME_GRADIENT_ANGLE degrees instead of deriving from THEME_BG_COLOR.
        public string THEME_GRADIENT_START { get; set; } = "";
        public string THEME_GRADIENT_END { get; set; } = "";
        public double THEME_GRADIENT_ANGLE { get; set; } = 135.0;
        public string CUSTOM_FONT_PATH { get; set; } = "";
        public bool HIDE_DEV_LIBS { get; set; } = false;

        public double WINDOW_CORNER_RADIUS { get; set; } = 12.0;
        public double WINDOW_BORDER_THICKNESS { get; set; } = 1.0;
        public string WINDOW_SHAPE_MODE { get; set; } = "Rounded"; // Rounded, Flat, Cut, Capsule, Slanted, Diamond, Octagon
        public bool ENABLE_WINDOW_GLOW { get; set; } = true;
        public double WINDOW_GLOW_RADIUS { get; set; } = 15.0;

        public bool ENABLE_RAINBOW_BORDER { get; set; } = false;
        public double RAINBOW_BORDER_SPEED { get; set; } = 5.0;

        public bool ENABLE_SCANLINES { get; set; } = false;
        public double SCANLINE_OPACITY { get; set; } = 0.1;
        public double SCANLINE_FREQUENCY { get; set; } = 4.0;

        public bool ENABLE_VIGNETTE { get; set; } = false;
        public double VIGNETTE_INTENSITY { get; set; } = 0.5;

        public bool ENABLE_GRAIN { get; set; } = false;
        public double GRAIN_OPACITY { get; set; } = 0.05;

        public bool ENABLE_CHROMA_SHIFT { get; set; } = false;
        public double CHROMA_SHIFT_AMOUNT { get; set; } = 1.0;

        public bool ENABLE_GLOW_PULSE { get; set; } = false;
        public double GLOW_PULSE_SPEED { get; set; } = 2.0;

        public bool ENABLE_BLUR_PULSE { get; set; } = false;
        public double BLUR_PULSE_SPEED { get; set; } = 1.0;

        public double GUI_SCALE { get; set; } = 1.0;
        public bool AUTO_GUI_SCALE_TO_SCREEN { get; set; } = true;
        public string BACKGROUND_GIF_PATH { get; set; } = "";
        public double BACKGROUND_GIF_OPACITY { get; set; } = 0.6;
        public double BACKGROUND_GIF_FPS { get; set; } = 30;
        public bool ENABLE_TEXT_STROKE { get; set; } = false;
        public Dictionary<string, TextVisualProfile> TEXT_PROFILES { get; set; } = new Dictionary<string, TextVisualProfile>(StringComparer.OrdinalIgnoreCase) {
            { "Titles", new TextVisualProfile { Name = "Titles", Strokes = new List<TextStroke>{ new TextStroke { Thickness=1, Color="#FF000000"}, new TextStroke{Thickness=2.5, Color="#FFFFFFFF"} } } },
            { "Headers", new TextVisualProfile { Name = "Headers", Strokes = new List<TextStroke>{ new TextStroke { Thickness=1, Color="#FF000000"} } } },
            { "Labels", new TextVisualProfile { Name = "Labels" } },
            { "Search", new TextVisualProfile { Name = "Search", IsItalic = true } },
            { "Cards", new TextVisualProfile { Name = "Cards" } },
            { "Values", new TextVisualProfile { Name = "Values" } },
            { "Subtext", new TextVisualProfile { Name = "Subtext" } },
            { "Code", new TextVisualProfile { Name = "Code" } },
            { "Accents", new TextVisualProfile { Name = "Accents" } }
        };
        public List<TextStroke> TEXT_STROKES { get; set; } = new List<TextStroke> {
            new TextStroke { Thickness = 1.0, Color = "#FF000000" },
            new TextStroke { Thickness = 2.0, Color = "#FFFFFFFF" },
            new TextStroke { Thickness = 3.0, Color = "#FF000000" }
        };
        public double TEXT_CHARACTER_SPACING { get; set; } = 0.0;
        public bool ENABLE_TEXT_SHADOW { get; set; } = true;
        public double TEXT_SHADOW_OFFSET_X { get; set; } = 0.0;
        public double TEXT_SHADOW_OFFSET_Y { get; set; } = 0.0;
        public double TEXT_SHADOW_BLUR { get; set; } = 0.0;
        public string TEXT_SHADOW_COLOR { get; set; } = "#FF000000";
        public string TEXT_STROKE_LINE_JOIN { get; set; } = "Round";
        public bool TEXT_IS_ITALIC { get; set; } = false;
        public double TEXT_GLOW_AMOUNT { get; set; } = 0.0;
        public string TEXT_GLOW_COLOR { get; set; } = "#FF00FFFF";
        public double TEXT_WOBBLINESS { get; set; } = 0.0;
        public double TEXT_WOBBLE_SPEED { get; set; } = 1.0;
        public double WINDOW_DRAG_WOBBLE { get; set; } = 1.0;
        public double WINDOW_DRAG_WOBBLE_MAX_SKEW { get; set; } = 5.0;
        public bool ENABLE_WINDOW_DRAG_WOBBLE { get; set; } = true;
        public double GLOW_STRENGTH { get; set; } = 1.0;
        public double BLUR_RADIUS { get; set; } = 15.0;
        public double GLASS_OPACITY { get; set; } = 0.85;

        // --- LEMONADE LLM PROXY ---
        public string LEMONADE_ENDPOINT { get; set; } = string.Empty;
        public string LEMONADE_MODEL { get; set; } = string.Empty;
    }

    public class SettingsManager : ISettingsService
    {
        private static string DataDir => PathHandler.GetDataDirectory();
        private static string SettingsPath => Path.Combine(DataDir, "SystemSettings.json");
        private SystemSettings? _cached;

        public static SystemSettings Current => CoreRegistry.Settings.Current;

        SystemSettings ISettingsService.Current => _cached ??= LoadInternal();

        void ISettingsService.Load() => _cached = LoadInternal();

        private SystemSettings LoadInternal()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<SystemSettings>(json) ?? new SystemSettings();
                    return settings;
                }
            }
            catch { }
            return new SystemSettings();
        }

        void ISettingsService.Save()
        {
            try
            {
                if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
                string json = JsonSerializer.Serialize(_cached ?? new SystemSettings(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public static void Load() => CoreRegistry.Settings.Load();
        public static void Save() => CoreRegistry.Settings.Save();
    }
}
```

### 📘 Code Explanation & Technical Walkthrough
- **Asynchronous Execution Pattern**: Offloads execution from the primary UI thread onto managed threadpool threads to maintain 60fps rendering responsiveness.
- **Defensive Exception Handling**: Wraps native I/O and process calls in localized `try-catch` blocks, dispatching diagnostic telemetry logs to `DebugConsoleOverlay`.
- **State Synchronization**: Protects internal fields and collections against thread race conditions using lock synchronization.

---

## ⚡ Execution Flow & Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Caller as Caller / UI Overlay
    participant Sub as TextStroke
    participant Kernel as OS Kernel / Layer 0
    participant Log as DebugConsoleOverlay

    Caller->>Sub: Invoke Action / Query Request
    Sub->>Kernel: Execute Managed & Unmanaged Operations
    Kernel-->>Sub: Operation Result / Status Payload
    Sub->>Log: Emit Diagnostic Telemetry Trace
    Sub-->>Caller: Return Results / Update HUD
```

---

## 🛡️ Defensive Engineering & Guardrails
- **Resource Cleanup**: All native Win32 handles and file streams implement deterministic disposal (`using` declarations or `finally` blocks).
- **Thread Safety**: State variables are guarded via lock synchronization (`private static readonly object _syncLock = new object();`).
- **Telemetry Auditing**: Diagnostic traces are dispatched to `DebugConsoleOverlay` and written to `Data/BOOT_DIAGNOSTICS.log`.

---

## 🔗 Related WikiLinks
- [[Master Map of Content & System Index]]
- [[Core System Architecture & 4-Layer Hierarchy]]
- [[NativeMethods & Win32 Kernel Interop Master Manual]]
- [[AiAPI Gateway & Multi-Model Routing Architecture]]
- [[BaseOverlay & GPU Holographic Windowing Engine]]
- [[SystemMonitorOverlay & Diagnostic Telemetry HUD]]
- [[Max PC Optimization Pipeline & Autonomic Engine]]
