---
title: "💻 Developer, AI & Media Command Handlers Complete Reference"
tags: ['handlers', 'dev', 'ai', 'media', 'git', 'build', 'ffmpeg', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 💻 Developer, AI & Media Command Handlers Complete Reference

## 💻 Complete Reference: Dev, AI & Media Command Handlers

Located in `Modules/Layer3/Handlers/Dev/`, `AI/`, and `Media/`:

### 1. Developer Handlers
- **`BuildCommandHandler`**: Background MSBuild and `dotnet build` compiler runner.
- **`GitCommandHandler`**: Quick git status, atomic commits, branch switching, and push/pull.
- **`CliRunnerCommandHandler`**: Non-blocking asynchronous command-line shell dispatcher.
- **`DisassemblerSuiteCommandHandler`**: Launches the master reverse engineering HUD.

### 2. AI Handlers
- **`AiCommandHandler`**: Instantly launches `ChatOverlay` for prompt execution.
- **`TeacherCommandHandler`**: Initiates structured coding tutoring lessons.
- **`DatasetCommandHandler`**: Ingests codebases and exports fine-tuning JSONL pairs.

### 3. Media Handlers
- **`ScreenAnalysisCommandHandler`**: Takes high-resolution screenshots and performs OCR/Vision reasoning.
- **`TtsCommandHandler`**: Vocalizes text or clipboard contents via SAPI speech synthesis.
- **`FFMpegCommandHandler`**: Video/audio format transcoding and MP3 extraction.
